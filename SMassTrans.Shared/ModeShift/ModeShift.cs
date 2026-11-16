using System.Diagnostics;
using System.Net.Http.Json;
using SilverConfig;

namespace SMassTrans.Shared.ModeShift;

public class ModeShiftClient : IClient<Stop, LineInfo>
{
    private readonly HttpClient _client;
    public string BaseUrl { get; set; } = "https://www.modeshift.app/api/v1/9814b106-2afe-47c8-919b-bdec6a5e521e";
    public string SettingsUrl { get; set; } = "https://transport.skopjebus.mk/wp-content/plugins/planner/data/settings.json";

    public GtfsFeed Gtfs { get; }

    public ModeShiftClient(HttpClient? client = null)
    {
        _client = client ?? new HttpClient();
        if (!_client.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _client.DefaultRequestHeaders.Add("User-Agent", "SMassTrans (+https://gitlab.com/silvercraft, silverdiamond@duck.com)");
        }
        Gtfs = new GtfsFeed(_client);
    }

    public bool IsInitialised { get; private set; }

    public async Task InitAsync()
    {
        try
        {
            await GetStops();
            await GetAllLineInfo();
            IsInitialised = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ModeShiftClient InitAsync error: {ex.Message}");
            // Try loading from GTFS feed as fallback
            try
            {
                await LoadFromGtfsFeedAsync();
                IsInitialised = true;
            }
            catch (Exception gtfsEx)
            {
                Debug.WriteLine($"GTFS fallback load failed: {gtfsEx.Message}");
            }
        }
    }

    public async Task LoadFromGtfsFeedAsync(bool forceDownload = false)
    {
        await Gtfs.LoadFeedAsync(forceDownload);
        var stops = Gtfs.ToStops();
        _stopsCache.Value = stops;

        var routeInfoDict = new SerializableDictionary<LineRouteId, RouteInfo>();
        var lineInfoList = new List<LineInfo>();

        var tripStopTimes = Gtfs.StopTimes
            .GroupBy(st => st.TripId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.StopSequence).Select(x => x.StopId.ToString()).ToList());

        var routeTrips = Gtfs.Trips.GroupBy(t => t.RouteId);

        foreach (var rGroup in routeTrips)
        {
            int routeId = rGroup.Key;
            var gtfsRoute = Gtfs.Routes.FirstOrDefault(r => r.RouteId == routeId);
            string shortName = gtfsRoute?.ShortName ?? routeId.ToString();
            string longName = gtfsRoute?.LongName ?? $"Линија {shortName}";

            var lineRouteIds = new List<LineRouteId>();

            var dirGroups = rGroup.GroupBy(t => t.DirectionId);
            int idx = 1;
            foreach (var dirGroup in dirGroups)
            {
                var repTrip = dirGroup.First();
                var key = new LineRouteId(routeId.ToString(), idx.ToString());
                lineRouteIds.Add(key);

                tripStopTimes.TryGetValue(repTrip.TripId, out var stopList);
                stopList ??= [];

                string dir = !string.IsNullOrWhiteSpace(repTrip.Headsign) ? repTrip.Headsign : $"Direction {dirGroup.Key}";
                routeInfoDict[key] = new RouteInfo(key, longName, dir, stopList);
                idx++;
            }

            bool nightly = shortName.Contains('Н') || shortName.Contains('N') || longName.ToLowerInvariant().Contains("ноќ");
            lineInfoList.Add(new LineInfo(lineRouteIds.ToArray(), longName, shortName, "ЈСП", nightly));
        }

        _routeInfoCache.Value = routeInfoDict;
        _lineInfoCache.Value = lineInfoList;
    }

    private readonly FileBackedCache<List<Stop>> _stopsCache = new(TimeSpan.FromDays(20), "ms_stops");
    private readonly FileBackedCache<SerializableDictionary<LineRouteId, RouteInfo>> _routeInfoCache = new(TimeSpan.FromHours(10), "ms_routeInfo");
    private readonly FileBackedCache<List<LineInfo>> _lineInfoCache = new(TimeSpan.FromHours(10), "ms_lineInfo");

    public async Task<List<Stop>> GetStops(bool ignoreCache = false)
    {
        if (_stopsCache.IsValid && !ignoreCache && _stopsCache.Value != null && _stopsCache.Value.Count > 0)
        {
            Debug.WriteLine("Getting stops from cache");
            return _stopsCache.Value;
        }

        try
        {
            var a = await _client.GetAsync(BaseUrl + "/transport/planner/stops");
            a.EnsureSuccessStatusCode();
            var stops = await a.Content.ReadFromJsonAsync<List<Stop>>() ?? [];
            _stopsCache.Value = stops;
            return stops;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to fetch stops from REST API: {ex.Message}");
            if (_stopsCache.Value != null && _stopsCache.Value.Count > 0)
            {
                return _stopsCache.Value;
            }
            await Gtfs.LoadFeedAsync();
            var stops = Gtfs.ToStops();
            _stopsCache.Value = stops;
            return stops;
        }
    }

    public async Task<Stop?> GetStop(string stopId)
    {
        if (int.TryParse(stopId, out var id))
        {
            var stops = await GetStops();
            return stops.FirstOrDefault(x => x.StopId == id);
        }
        return null;
    }

    public async Task<List<Route>?> GetRoutes()
    {
        var a = await _client.GetAsync(BaseUrl + "/transport/planner/routes");
        a.EnsureSuccessStatusCode();
        return await a.Content.ReadFromJsonAsync<List<Route>>();
    }

    private async Task LoadRoutesAndLinesAsync()
    {
        try
        {
            var routes = await GetRoutes() ?? [];
            var stops = await GetStops();
            var stopDict = stops.ToDictionary(s => s.id, s => s.name);

            var routeInfoDict = new SerializableDictionary<LineRouteId, RouteInfo>();
            var lineInfoList = new List<LineInfo>();

            foreach (var r in routes)
            {
                var lineRouteIds = new List<LineRouteId>();

                if (r.patterns != null)
                {
                    foreach (var p in r.patterns)
                    {
                        var key = new LineRouteId(r.id.ToString(), p.index.ToString());
                        lineRouteIds.Add(key);

                        string fromName = stopDict.TryGetValue(p.fromStopId, out var fn) ? fn : $"Stop {p.fromStopId}";
                        string toName = stopDict.TryGetValue(p.toStopId, out var tn) ? tn : $"Stop {p.toStopId}";
                        string dir = p.direction == 1
                            ? (!string.IsNullOrWhiteSpace(r.outbound) ? r.outbound : $"{fromName} -> {toName}")
                            : (!string.IsNullOrWhiteSpace(r.inbound) ? r.inbound : $"{fromName} -> {toName}");
                        string name = !string.IsNullOrWhiteSpace(r.longName) ? r.longName : (!string.IsNullOrWhiteSpace(r.shortName) ? $"Линија {r.shortName}" : $"Route {r.id}");

                        var stopIds = p.stops?.Select(s => s.ToString()).ToList() ?? [];

                        routeInfoDict[key] = new RouteInfo(key, name, dir, stopIds);
                    }
                }

                bool nightly = r.shortName != null && (r.shortName.Contains('Н') || r.shortName.Contains('N') || r.longName != null && r.longName.ToLowerInvariant().Contains("ноќ"));
                string lineName = !string.IsNullOrWhiteSpace(r.longName) ? r.longName : r.shortName;
                string lineNumber = !string.IsNullOrWhiteSpace(r.shortName) ? r.shortName : r.id.ToString();

                var line = new LineInfo(lineRouteIds.ToArray(), lineName, lineNumber, "ЈСП", nightly);
                lineInfoList.Add(line);
            }

            _routeInfoCache.Value = routeInfoDict;
            _lineInfoCache.Value = lineInfoList;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load routes from API: {ex.Message}. Falling back to GTFS feed.");
            await LoadFromGtfsFeedAsync();
        }
    }

    public async Task<IEnumerable<RouteInfo>> GetAllRouteInfo()
    {
        if (_routeInfoCache.IsValid && _routeInfoCache.Value != null && _routeInfoCache.Value.Count > 0)
        {
            return _routeInfoCache.Value.Values;
        }
        await LoadRoutesAndLinesAsync();
        return _routeInfoCache.Value?.Values ?? Enumerable.Empty<RouteInfo>();
    }

    public async Task<RouteInfo> GetRouteInfo(LineRouteId lineRouteId)
    {
        if (_routeInfoCache.IsValid && _routeInfoCache.Value != null && _routeInfoCache.Value.TryGetValue(lineRouteId, out var cachedRoute))
        {
            return cachedRoute;
        }
        await LoadRoutesAndLinesAsync();
        if (_routeInfoCache.Value != null && _routeInfoCache.Value.TryGetValue(lineRouteId, out var route))
        {
            return route;
        }
        return _routeInfoCache.Value?.Values.FirstOrDefault(r => r.LineRouteId.LineId == lineRouteId.LineId)
            ?? throw new KeyNotFoundException($"Route not found for {lineRouteId}");
    }

    public async Task<IEnumerable<LineInfo>> GetAllLineInfo()
    {
        if (_lineInfoCache.IsValid && _lineInfoCache.Value != null && _lineInfoCache.Value.Count > 0)
        {
            return _lineInfoCache.Value;
        }
        await LoadRoutesAndLinesAsync();
        return _lineInfoCache.Value ?? Enumerable.Empty<LineInfo>();
    }

    public async Task<LineInfo> GetLineInfo(LineRouteId lineRouteId)
    {
        var lines = await GetAllLineInfo();
        var match = lines.FirstOrDefault(x => x.LineRouteIds.Contains(lineRouteId))
            ?? lines.FirstOrDefault(x => x.LineRouteIds.Any(lr => lr.LineId == lineRouteId.LineId))
            ?? lines.FirstOrDefault(x => x.Number == lineRouteId.LineId);
        return match ?? throw new KeyNotFoundException($"Line not found for {lineRouteId}");
    }

    public async Task<LineInfo> GetLineInfo(string lineId)
    {
        var lines = await GetAllLineInfo();
        var match = lines.FirstOrDefault(x => x.LineRouteIds.Any(lr => lr.LineId == lineId))
            ?? lines.FirstOrDefault(x => x.Number == lineId)
            ?? lines.FirstOrDefault(x => x.Name == lineId);
        return match ?? throw new KeyNotFoundException($"Line not found for {lineId}");
    }

    public async Task<IEnumerable<KeyValuePair<LineRouteId, IReadOnlyList<int>>>> GetLiveStopLinesForStopAsync(string stopId)
    {
        if (!int.TryParse(stopId, out var stopNum))
        {
            return Enumerable.Empty<KeyValuePair<LineRouteId, IReadOnlyList<int>>>();
        }

        try
        {
            var resp = await _client.GetAsync(BaseUrl + $"/transport/planner/stops/{stopNum}/times");
            if (!resp.IsSuccessStatusCode)
            {
                return Enumerable.Empty<KeyValuePair<LineRouteId, IReadOnlyList<int>>>();
            }

            var timeGroups = await resp.Content.ReadFromJsonAsync<List<Time>>() ?? [];
            var result = new List<KeyValuePair<LineRouteId, IReadOnlyList<int>>>();
            var now = DateTimeOffset.UtcNow;

            foreach (var group in timeGroups)
            {
                if (group.route == null || group.times == null) continue;

                var key = new LineRouteId(group.route.routeId.ToString(), group.route.index.ToString());
                var secondsList = new List<int>();

                foreach (var t in group.times)
                {
                    var timeStr = !string.IsNullOrEmpty(t.scheduledDeparture) ? t.scheduledDeparture : t.scheduledArrival;
                    if (DateTimeOffset.TryParse(timeStr, out var dto))
                    {
                        var delay = t.departureDelay != 0 ? t.departureDelay : t.arrivalDelay;
                        var actualTime = dto.AddSeconds(delay);
                        var remaining = (int)(actualTime - now).TotalSeconds;
                        if (remaining >= -120)
                        {
                            secondsList.Add(Math.Max(0, remaining));
                        }
                    }
                }

                if (secondsList.Count > 0)
                {
                    secondsList.Sort();
                    result.Add(new KeyValuePair<LineRouteId, IReadOnlyList<int>>(key, secondsList));
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error fetching live stop lines: {ex.Message}");
            return Enumerable.Empty<KeyValuePair<LineRouteId, IReadOnlyList<int>>>();
        }
    }

    public async Task<Settings?> GetSettings()
    {
        var a = await _client.GetAsync(SettingsUrl);
        return await a.Content.ReadFromJsonAsync<Settings>();
    }

    public async Task<List<NearbyTimes>?> GetNearbyTimes(double latitude, double longitude, double radius)
    {
        var a = await _client.GetAsync(BaseUrl + $"/transport/planner/stops/nearbyTimes?latitude={latitude}&longitude={longitude}&radius={radius}");
        return await a.Content.ReadFromJsonAsync<List<NearbyTimes>>();
    }

    public async Task<List<Time>?> GetTimes(int stopNum, int? numberofDepartures = null, int? timerange = null)
    {
        var url = BaseUrl + $"/transport/planner/stops/{stopNum}/times";
        var query = new List<string>();
        if (numberofDepartures.HasValue) query.Add($"numberOfDepartures={numberofDepartures.Value}");
        if (timerange.HasValue) query.Add($"timeRange={timerange.Value}");
        if (query.Count > 0) url += "?" + string.Join("&", query);

        var a = await _client.GetAsync(url);
        return await a.Content.ReadFromJsonAsync<List<Time>>();
    }
}
