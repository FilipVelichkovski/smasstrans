using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using SilverConfig;

namespace SMassTrans.Shared;

public class SkopskaPositionsClient
{
    private readonly HttpClient _client;
    private const string BaseUrl = "http://info.skopska.mk:8080";
    private const string AuthUrl = BaseUrl + "/rest-auth/guests?aid=3136";
    private const string PositionsUrl = BaseUrl + "/rest-avl/rtpi/positions";
    private const string StatusesUrl = BaseUrl + "/rest-its/rtpi/vehicle-statuses";
    private const string CoursesUrl = BaseUrl + "/rest-its/scheme/courses";

    private string? _sessionId;
    private readonly FileBackedCache<List<SkopskaRawCourse>> _coursesCache = new(TimeSpan.FromHours(24), "skopska_courses");
    private Dictionary<int, int>? _courseRouteDict;

    public SkopskaPositionsClient(HttpClient? client = null)
    {
        _client = client ?? new HttpClient();
        if (!_client.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _client.DefaultRequestHeaders.Add("User-Agent", "SMassTrans (+https://gitlab.com/silvercraft, silverdiamond@duck.com)");
        }
    }

    public async Task<string?> AuthenticateAsync(CancellationToken ct = default)
    {
        try
        {
            var content = new StringContent("{}", Encoding.UTF8, "application/json");
            var resp = await _client.PostAsync(AuthUrl, content, ct).ConfigureAwait(false);
            if (resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _sessionId = body.Trim('"', ' ', '\r', '\n');
                return _sessionId;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Skopska auth error: {ex.Message}");
        }
        return null;
    }

    public async Task<List<SkopskaRawPosition>> GetPositionsAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_sessionId))
        {
            await AuthenticateAsync(ct).ConfigureAwait(false);
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, PositionsUrl);
            if (!string.IsNullOrEmpty(_sessionId))
            {
                request.Headers.Add("eurogps.eu.sid", _sessionId);
            }

            var resp = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            if (resp.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
            {
                await AuthenticateAsync(ct).ConfigureAwait(false);
                using var retryReq = new HttpRequestMessage(HttpMethod.Get, PositionsUrl);
                if (!string.IsNullOrEmpty(_sessionId))
                {
                    retryReq.Headers.Add("eurogps.eu.sid", _sessionId);
                }
                resp = await _client.SendAsync(retryReq, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            }

            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<List<SkopskaRawPosition>>(cancellationToken: ct).ConfigureAwait(false) ?? [];
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to get Skopska positions: {ex.Message}");
            return [];
        }
    }

    public async Task<List<SkopskaRawStatus>> GetStatusesAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_sessionId))
        {
            await AuthenticateAsync(ct).ConfigureAwait(false);
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, StatusesUrl);
            if (!string.IsNullOrEmpty(_sessionId))
            {
                request.Headers.Add("eurogps.eu.sid", _sessionId);
            }

            var resp = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            if (resp.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
            {
                await AuthenticateAsync(ct).ConfigureAwait(false);
                using var retryReq = new HttpRequestMessage(HttpMethod.Get, StatusesUrl);
                if (!string.IsNullOrEmpty(_sessionId))
                {
                    retryReq.Headers.Add("eurogps.eu.sid", _sessionId);
                }
                resp = await _client.SendAsync(retryReq, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            }

            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<List<SkopskaRawStatus>>(cancellationToken: ct).ConfigureAwait(false) ?? [];
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to get Skopska statuses: {ex.Message}");
            return [];
        }
    }

    public async Task<Dictionary<int, int>> GetCourseRoutesAsync(CancellationToken ct = default)
    {
        if (_courseRouteDict != null)
        {
            return _courseRouteDict;
        }

        if (_coursesCache.IsValid && _coursesCache.Value != null && _coursesCache.Value.Count > 0)
        {
            _courseRouteDict = _coursesCache.Value.GroupBy(c => c.Id).ToDictionary(g => g.Key, g => g.First().RouteId);
            return _courseRouteDict;
        }

        if (string.IsNullOrEmpty(_sessionId))
        {
            await AuthenticateAsync(ct).ConfigureAwait(false);
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, CoursesUrl);
            if (!string.IsNullOrEmpty(_sessionId))
            {
                request.Headers.Add("eurogps.eu.sid", _sessionId);
            }

            var resp = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            if (resp.IsSuccessStatusCode)
            {
                var courses = await resp.Content.ReadFromJsonAsync<List<SkopskaRawCourse>>(cancellationToken: ct).ConfigureAwait(false) ?? [];
                _coursesCache.Value = courses;
                _courseRouteDict = courses.GroupBy(c => c.Id).ToDictionary(g => g.Key, g => g.First().RouteId);
                return _courseRouteDict;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to get Skopska courses: {ex.Message}");
        }

        if (_coursesCache.Value != null)
        {
            _courseRouteDict = _coursesCache.Value.GroupBy(c => c.Id).ToDictionary(g => g.Key, g => g.First().RouteId);
            return _courseRouteDict;
        }

        return [];
    }

    public async Task<List<Position>> GetCombinedPositionsAsync(string? filterLine = null, CancellationToken ct = default)
    {
        return await Task.Run(async () =>
        {
            var positions = await GetPositionsAsync(ct).ConfigureAwait(false);
            if (positions.Count == 0) return [];

            var statuses = await GetStatusesAsync(ct).ConfigureAwait(false);
            var statusDict = statuses
                .GroupBy(s => s.VehicleId)
                .ToDictionary(g => g.Key, g => g.Last());

            Dictionary<int, int>? courseRoutes = null;
            try
            {
                courseRoutes = await GetCourseRoutesAsync(ct).ConfigureAwait(false);
            }
            catch {}

            int.TryParse(filterLine, out var parsedFilter);

            var result = new List<Position>();
            foreach (var pos in positions)
            {
                statusDict.TryGetValue(pos.VehicleId, out var st);

                int routeId = 0;
                if (st != null && courseRoutes != null && courseRoutes.TryGetValue(st.CourseId, out var mappedRoute))
                {
                    routeId = mappedRoute;
                }
                else if (parsedFilter != 0)
                {
                    routeId = parsedFilter;
                }

                if (!string.IsNullOrEmpty(filterLine) && parsedFilter != 0 && routeId != 0 && routeId != parsedFilter)
                {
                    // Filtered out
                    continue;
                }

                var p = new Position
                {
                    VehicleId = pos.VehicleId,
                    Lat = pos.Lat,
                    Lon = pos.Lon,
                    Time = pos.Time,
                    Speed = st?.Speed ?? 0,
                    Delay = st?.Delay ?? 0,
                    CourseId = st?.CourseId ?? 0,
                    StopId = st?.StopId ?? 0,
                    StopIndex = st?.StopIndex ?? 0,
                    Ignition = pos.Ignition || (st?.Ignition ?? true),
                    Status = st?.Status ?? "COURSE_EXECUTE",
                    RouteId = routeId,
                    Vehicle = new Vehicle
                    {
                        Name = pos.VehicleId.ToString(),
                        Plate = pos.VehicleId.ToString()
                    }
                };

                result.Add(p);
            }

            return result;
        }, ct).ConfigureAwait(false);
    }
}

public class SkopskaRawPosition
{
    [JsonPropertyName("vehicleId")]
    public int VehicleId { get; set; }

    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("lon")]
    public double Lon { get; set; }

    [JsonPropertyName("time")]
    public int Time { get; set; }

    [JsonPropertyName("ignition")]
    public bool Ignition { get; set; }
}

public class SkopskaRawStatus
{
    [JsonPropertyName("vehicleId")]
    public int VehicleId { get; set; }

    [JsonPropertyName("courseId")]
    public int CourseId { get; set; }

    [JsonPropertyName("stopId")]
    public int StopId { get; set; }

    [JsonPropertyName("stopIndex")]
    public int StopIndex { get; set; }

    [JsonPropertyName("ignition")]
    public bool Ignition { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("speed")]
    public int Speed { get; set; }

    [JsonPropertyName("delay")]
    public int Delay { get; set; }
}

public class SkopskaRawCourse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("routeId")]
    public int RouteId { get; set; }
}
