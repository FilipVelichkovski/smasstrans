using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using TransitRealtime;

namespace SMassTrans.Shared.ModeShift;

public class ModeShiftGps : IGps
{
    public const string GtfsRtTripUpdatesUrl = "https://www.modeshift.app/api/v1/9814b106-2afe-47c8-919b-bdec6a5e521e/transport/gtfsrt/tripupdates.pb";
    public const string LiveVehiclesUrl = "https://www.modeshift.app/api/v1/9814b106-2afe-47c8-919b-bdec6a5e521e/transport/public/vehicles/live";

    public event EventHandler<GpsData>? DataRecieved;

    private readonly HttpClient _httpClient;
    private readonly SkopskaPositionsClient _fallbackClient;
    private CancellationTokenSource? _cts;
    private Task? _pollTask;
    private string? _currentLine;
    private readonly TimeSpan _pollInterval;

    public ModeShiftGps(HttpClient? httpClient = null, TimeSpan? pollInterval = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "SMassTrans (+https://gitlab.com/silvercraft, silverdiamond@duck.com)");
        }
        _fallbackClient = new SkopskaPositionsClient(_httpClient);
        _pollInterval = pollInterval ?? TimeSpan.FromSeconds(5);
    }

    public async Task SubscribeToLine(string line)
    {
        _currentLine = line;

        if (_pollTask == null || _cts == null || _cts.IsCancellationRequested)
        {
            _cts = new CancellationTokenSource();
            _pollTask = Task.Run(() => PollLoopAsync(_cts.Token));
        }

        await Task.CompletedTask;
    }

    public Task UnSubscribeToLine(string line)
    {
        if (_currentLine == line)
        {
            _currentLine = null;
        }
        return Task.CompletedTask;
    }

    private async Task PollLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            bool modeshiftSucceeded = false;
            try
            {
                var positions = await FetchGtfsRtPositionsAsync(ct).ConfigureAwait(false);

                // If GTFS-RT had no positions for the route, try ModeShift REST live endpoint
                if (positions.Count == 0 && !string.IsNullOrEmpty(_currentLine))
                {
                    var restPositions = await FetchLiveVehiclesAsync(_currentLine, ct).ConfigureAwait(false);
                    if (restPositions.Count > 0)
                    {
                        positions.AddRange(restPositions);
                    }
                }

                modeshiftSucceeded = true;
                int lineId = int.TryParse(_currentLine, out var parsed) ? parsed : 0;
                var gpsData = new GpsData
                {
                    Id = lineId,
                    Infos = positions.Cast<IGpsInfo>().ToList()
                };
                DataRecieved?.Invoke(this, gpsData);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ModeShift GTFS-RT/REST poll error: {ex.Message}");
            }

            // If ModeShift failed with exception, try Skopska fallback
            if (!modeshiftSucceeded)
            {
                try
                {
                    var fallbackPositions = await _fallbackClient.GetCombinedPositionsAsync(_currentLine, ct).ConfigureAwait(false);
                    if (fallbackPositions.Count > 0)
                    {
                        int lineId = int.TryParse(_currentLine, out var parsed) ? parsed : 0;
                        var gpsData = new GpsData
                        {
                            Id = lineId,
                            Infos = fallbackPositions.Cast<IGpsInfo>().ToList()
                        };
                        DataRecieved?.Invoke(this, gpsData);
                    }
                }
                catch (Exception fbEx)
                {
                    Debug.WriteLine($"Skopska fallback positions error: {fbEx.Message}");
                }
            }

            try
            {
                await Task.Delay(_pollInterval, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task<List<Position>> FetchLiveVehiclesAsync(string routeId, CancellationToken ct)
    {
        var result = new List<Position>();
        try
        {
            string url = $"{LiveVehiclesUrl}?routeId={Uri.EscapeDataString(routeId)}";
            using var resp = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                return result;
            }

            var list = await resp.Content.ReadFromJsonAsync<List<ModeShiftLiveVehicle>>(cancellationToken: ct).ConfigureAwait(false) ?? [];
            int.TryParse(routeId, out var parsedRouteId);

            foreach (var v in list)
            {
                var pos = new Position
                {
                    VehicleId = v.Id,
                    RouteId = parsedRouteId,
                    Lat = v.PositionLatitude,
                    Lon = v.PositionLongitude,
                    Speed = (int)v.PositionSpeed,
                    Ignition = true,
                    Status = v.Status switch
                    {
                        1 => "WAIT_STOP",
                        2 => "COURSE_EXECUTE",
                        _ => "COURSE_EXECUTE"
                    },
                    Vehicle = new Vehicle
                    {
                        Name = !string.IsNullOrEmpty(v.DisplayName) ? v.DisplayName : v.Id.ToString(),
                        Plate = !string.IsNullOrEmpty(v.DisplayName) ? v.DisplayName : v.Id.ToString()
                    }
                };
                result.Add(pos);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error fetching ModeShift live vehicles: {ex.Message}");
        }

        return result;
    }

    private async Task<List<Position>> FetchGtfsRtPositionsAsync(CancellationToken ct)
    {
        var result = new List<Position>();
        using var resp = await _httpClient.GetAsync(GtfsRtTripUpdatesUrl, ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            return result;
        }

        await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        var feed = FeedMessage.Parser.ParseFrom(stream);

        // Build a lookup of delay info from trip updates if present
        var delayByTripId = new Dictionary<string, int>();
        foreach (var entity in feed.Entity)
        {
            if (entity.TripUpdate != null && !string.IsNullOrEmpty(entity.TripUpdate.Trip?.TripId))
            {
                int delay = 0;
                if (entity.TripUpdate.Delay != 0)
                {
                    delay = entity.TripUpdate.Delay;
                }
                else if (entity.TripUpdate.StopTimeUpdate.Count > 0)
                {
                    var lastStu = entity.TripUpdate.StopTimeUpdate[^1];
                    delay = lastStu.Departure?.Delay != 0 ? lastStu.Departure?.Delay ?? 0 : lastStu.Arrival?.Delay ?? 0;
                }
                delayByTripId[entity.TripUpdate.Trip.TripId] = delay;
            }
        }

        foreach (var entity in feed.Entity)
        {
            if (entity.Vehicle != null)
            {
                var v = entity.Vehicle;
                string? routeIdStr = v.Trip?.RouteId;

                // Filter by route/line if specified
                if (!string.IsNullOrEmpty(_currentLine))
                {
                    if (routeIdStr != null && !routeIdStr.Equals(_currentLine, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                }

                int.TryParse(routeIdStr, out var routeId);
                int.TryParse(v.Vehicle?.Id, out var vehicleId);
                int.TryParse(v.StopId, out var stopId);
                int delay = 0;
                if (!string.IsNullOrEmpty(v.Trip?.TripId))
                {
                    delayByTripId.TryGetValue(v.Trip.TripId, out delay);
                }

                string plate = !string.IsNullOrEmpty(v.Vehicle?.LicensePlate) ? v.Vehicle.LicensePlate : (v.Vehicle?.Id ?? "");
                string name = !string.IsNullOrEmpty(v.Vehicle?.Label) ? v.Vehicle.Label : plate;

                var pos = new Position
                {
                    VehicleId = vehicleId,
                    RouteId = routeId,
                    Lat = v.Position?.Latitude ?? 0,
                    Lon = v.Position?.Longitude ?? 0,
                    Speed = (int)(v.Position?.Speed ?? 0),
                    StopId = stopId,
                    StopIndex = (int)v.CurrentStopSequence,
                    Delay = delay,
                    Ignition = true,
                    Status = v.CurrentStatus.ToString(),
                    Vehicle = new Vehicle
                    {
                        Name = name,
                        Plate = plate
                    }
                };

                result.Add(pos);
            }
        }

        return result;
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _pollTask = null;
    }
}

public class ModeShiftLiveVehicle
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("positionLatitude")]
    public double PositionLatitude { get; set; }

    [JsonPropertyName("positionLongitude")]
    public double PositionLongitude { get; set; }

    [JsonPropertyName("positionBearing")]
    public double PositionBearing { get; set; }

    [JsonPropertyName("positionSpeed")]
    public double PositionSpeed { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }
}
