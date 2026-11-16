using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;

namespace SMassTrans.Shared;

public class SkopskaPositionsClient
{
    private readonly HttpClient _client;
    private const string BaseUrl = "http://info.skopska.mk:8080";
    private const string AuthUrl = BaseUrl + "/rest-auth/guests?aid=3136";
    private const string PositionsUrl = BaseUrl + "/rest-avl/rtpi/positions";
    private const string StatusesUrl = BaseUrl + "/rest-its/rtpi/vehicle-statuses";
    private const string DutiesUrl = BaseUrl + "/rest-its/scheme/duties";
    private const string CoursesUrl = BaseUrl + "/rest-its/scheme/courses";

    private string? _sessionId;

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
            var resp = await _client.PostAsync(AuthUrl, content, ct);
            if (resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
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
            await AuthenticateAsync(ct);
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, PositionsUrl);
            if (!string.IsNullOrEmpty(_sessionId))
            {
                request.Headers.Add("eurogps.eu.sid", _sessionId);
            }

            var resp = await _client.SendAsync(request, ct);
            if (resp.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
            {
                // Re-authenticate and retry once
                await AuthenticateAsync(ct);
                using var retryReq = new HttpRequestMessage(HttpMethod.Get, PositionsUrl);
                if (!string.IsNullOrEmpty(_sessionId))
                {
                    retryReq.Headers.Add("eurogps.eu.sid", _sessionId);
                }
                resp = await _client.SendAsync(retryReq, ct);
            }

            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<List<SkopskaRawPosition>>(cancellationToken: ct) ?? [];
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
            await AuthenticateAsync(ct);
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, StatusesUrl);
            if (!string.IsNullOrEmpty(_sessionId))
            {
                request.Headers.Add("eurogps.eu.sid", _sessionId);
            }

            var resp = await _client.SendAsync(request, ct);
            if (resp.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
            {
                await AuthenticateAsync(ct);
                using var retryReq = new HttpRequestMessage(HttpMethod.Get, StatusesUrl);
                if (!string.IsNullOrEmpty(_sessionId))
                {
                    retryReq.Headers.Add("eurogps.eu.sid", _sessionId);
                }
                resp = await _client.SendAsync(retryReq, ct);
            }

            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<List<SkopskaRawStatus>>(cancellationToken: ct) ?? [];
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to get Skopska statuses: {ex.Message}");
            return [];
        }
    }

    public async Task<List<Position>> GetCombinedPositionsAsync(string? filterLine = null, CancellationToken ct = default)
    {
        var positions = await GetPositionsAsync(ct);
        if (positions.Count == 0) return [];

        var statuses = await GetStatusesAsync(ct);
        var statusDict = statuses.ToDictionary(s => s.VehicleId);

        var result = new List<Position>();
        foreach (var pos in positions)
        {
            statusDict.TryGetValue(pos.VehicleId, out var st);

            int routeId = 0;
            if (int.TryParse(filterLine, out var fl))
            {
                routeId = fl;
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
                Ignition = st?.Ignition ?? true,
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
}

public class SkopskaRawStatus
{
    [JsonPropertyName("vehicleId")]
    public int VehicleId { get; set; }

    [JsonPropertyName("courseId")]
    public int CourseId { get; set; }

    [JsonPropertyName("ignition")]
    public bool Ignition { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("speed")]
    public int Speed { get; set; }

    [JsonPropertyName("delay")]
    public int Delay { get; set; }
}
