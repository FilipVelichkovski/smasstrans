using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;

namespace SMassTrans.Shared;

public class Skopska : IClient<SkopskaStop, LineInfo>
{
    public Skopska(HttpClient _client)
    {
        client = _client;
        client.DefaultRequestHeaders.Add("User-Agent", "SMassTrans (+https://gitlab.com/silvercraft, silverdiamond@duck.com)");
    }
    const string baseUrl = "http://info.skopska.mk:8080";
    const string authUrl = baseUrl + "/rest-auth/guests?aid=3136";
    const string stopsUrl = baseUrl + "/rest-its/scheme/stops?filter=true";
    const string routesUrl = baseUrl + "/rest-its/scheme/routes";
    const string linesUrl = baseUrl + "/rest-its/scheme/lines";
    const string stoplinesUrl = baseUrl + "/rest-its/scheme/stop-lines";
    const string stopTimes = baseUrl + "/rest-its/scheme/stop-times?rid={0}&sid={1}";


    HttpClient client;
    string SessionId = null;
    public async Task PerformAuth()
    {
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var resp = await client.PostAsync(authUrl, content);
        SessionId = (await resp.Content.ReadAsStringAsync()).Trim('"');
        Debug.WriteLine(SessionId);
    }

    public bool IsInitialised { get; set; }
    public async Task InitAsync()
    {
        await PerformAuth();
        IsInitialised = true;
    }
    FileBackedCache<List<SkopskaStop>> _stopsCache=new(TimeSpan.FromDays(20));
    public async Task<List<SkopskaStop>> GetStops(bool ignoreCache = false)
    {
        if (_stopsCache.IsValid && !ignoreCache)
        {
            return _stopsCache.Value;
        }
        using var request = new HttpRequestMessage(HttpMethod.Get, stopsUrl);
        request.Headers.Add("eurogps.eu.sid", SessionId);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var enu = response.Content.ReadFromJsonAsAsyncEnumerable<StopRoot>();
        List<SkopskaStop> stops = [];
        await foreach (var s in enu)
        {
            stops.Add(new SkopskaStop(s.id.ToString(), s.number.ToString(), s.name, s.lat, s.lon));
        }

        _stopsCache.Value = stops;
        return _stopsCache.Value;

    }
    Cache<Dictionary<LineRouteId, RouteInfo>>? RouteInfoCache;
    TimeSpan AllowedCacheForRouteInfoCache = TimeSpan.FromHours(10);
    public async Task<IEnumerable<RouteInfo>> GetAllRouteInfo()
    {
        if (RouteInfoCache != null && DateTime.UtcNow < RouteInfoCache.Value.LastModified + AllowedCacheForRouteInfoCache)
        {
            return RouteInfoCache.Value.Value.Values;
        }
        await GetRoutes();
        return RouteInfoCache.Value.Value.Values;
    }
    public async Task<RouteInfo> GetRouteInfo(LineRouteId lineRouteId)
    {
        if (RouteInfoCache != null && DateTime.UtcNow < RouteInfoCache.Value.LastModified + AllowedCacheForRouteInfoCache)
        {
            return (RouteInfoCache.Value.Value[lineRouteId]);
        }
        await GetRoutes();
        return (RouteInfoCache.Value.Value[lineRouteId]);
    }
    public async Task GetRoutes()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, routesUrl);
        request.Headers.Add("eurogps.eu.sid", SessionId);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var enu = response.Content.ReadFromJsonAsAsyncEnumerable<RouteRoot>();
        Dictionary<LineRouteId, RouteInfo> routeinfostuff = [];
        await foreach (var s in enu)
        {
            var key = new LineRouteId(s.lineId.ToString(), s.id.ToString());
            routeinfostuff.Add(key, new RouteInfo(key, s.name, s.direction));
        }
        RouteInfoCache = new(routeinfostuff,AllowedCacheForRouteInfoCache);
    }
    Cache<List<LineInfo>>? LineInfoCache;
    TimeSpan AllowedCacheForLineInfoCache = TimeSpan.FromHours(10);
    public async Task<LineInfo> GetLineInfo(string lineId)
    {
        if (LineInfoCache != null && DateTime.UtcNow < LineInfoCache.Value.LastModified + AllowedCacheForLineInfoCache)
        {
            return LineInfoCache.Value.Value.First(x => x.lineRouteIds.Any(x => x.LineId == lineId));
        }
        await GetLines();
        return LineInfoCache.Value.Value.First(x => x.lineRouteIds.Any(x => x.LineId == lineId));
    }
      public async Task<LineInfo> GetLineInfo(LineRouteId lineId)
    {
        if (LineInfoCache != null && DateTime.UtcNow < LineInfoCache.Value.LastModified + AllowedCacheForLineInfoCache)
        {
            return LineInfoCache.Value.Value.First(x => x.lineRouteIds.Contains(lineId));
        }
        await GetLines();
        return LineInfoCache.Value.Value.First(x => x.lineRouteIds.Contains(lineId));
    }
    public async Task GetLines()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, linesUrl);
        request.Headers.Add("eurogps.eu.sid", SessionId);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var enu = response.Content.ReadFromJsonAsAsyncEnumerable<LinesRoot>();
        List<LineInfo> la = new();
        await foreach (var s in enu)
        {
            LineInfo l = new(s.RouteIds.Select(x => new LineRouteId(s.Id.ToString(), x.ToString())).ToArray(), s.Name, s.Name, s.Carrier, s.Nightly);
            la.Add(l);
        }
        LineInfoCache = new(la,AllowedCacheForLineInfoCache);
    }
    public async Task GetAllStopLines()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, stoplinesUrl);
        request.Headers.Add("eurogps.eu.sid", SessionId);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var enu = response.Content.ReadFromJsonAsAsyncEnumerable<StopLines>();
        using var fs = new FileStream("StopLines.txt", FileMode.Create);
        using var tw = new StreamWriter(fs);
        await foreach (var s in enu)
        {
            await tw.WriteLineAsync($"{s.StopId} {s.RouteId} {string.Join(',', s.RemainingTime)}");
        }
    }

    public Task<IReadOnlyDictionary<LineRouteId, IReadOnlyList<int>>> GetStopLinesForStop(SkopskaStop stop)
    {
       return GetStopLinesForStopId(stop.StopId.ToString("0000"));
    }
    public async Task<IReadOnlyDictionary<LineRouteId, IReadOnlyList<int>>> GetStopLinesForStopId(string stopId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, stoplinesUrl + "/" + stopId);
        request.Headers.Add("eurogps.eu.sid", SessionId);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var enu = response.Content.ReadFromJsonAsAsyncEnumerable<StopLines>();
        Dictionary<LineRouteId, IReadOnlyList<int>> lines = [];
        await foreach (var s in enu)
        {
            lines.Add(new(s.LineId.ToString(), s.RouteId.ToString()), s.RemainingTime);
        }
        return lines;
    }
    public async Task GetStopTimes(int stopId, int routeId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, string.Format(stopTimes, routeId, stopId));
        request.Headers.Add("eurogps.eu.sid", SessionId);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var enu = await response.Content.ReadFromJsonAsync<List<int>>();
        foreach (var s in enu)
        {
            Console.WriteLine($"{s / 3600:00}:{s % 3600 / 60:00}:{s % 60:00}");
        }
    }


}
public class StopLines
{
    [JsonPropertyName("stopId")]
    public int StopId { get; set; }

    [JsonPropertyName("lineId")]
    public int LineId { get; set; }

    [JsonPropertyName("routeId")]
    public int RouteId { get; set; }

    [JsonPropertyName("remainingTime")]
    public List<int> RemainingTime { get; set; }
}

public class StopRoot
{
    public int id { get; set; }
    public int areaId { get; set; }
    public string number { get; set; }
    public string name { get; set; }
    public StopRootTranslations translations { get; set; }
    public double lat { get; set; }
    public double lon { get; set; }
    public string note { get; set; }
}

public class StopRootTranslations
{
    public string en { get; set; }
    public string al { get; set; }
}

public class RouteDirectionTranslations
{
    [JsonPropertyName("al")]
    public string al { get; set; }
}

public class RouteNameTranslations
{
    [JsonPropertyName("al")]
    public string al { get; set; }

    [JsonPropertyName("en")]
    public string en { get; set; }

    [JsonPropertyName("mk")]
    public string mk { get; set; }
}

public class RouteRoot
{
    [JsonPropertyName("id")]
    public int id { get; set; }

    [JsonPropertyName("lineId")]
    public int lineId { get; set; }

    [JsonPropertyName("direction")]
    public string direction { get; set; }

    [JsonPropertyName("directionTranslations")]
    public RouteDirectionTranslations directionTranslations { get; set; }

    [JsonPropertyName("name")]
    public string name { get; set; }

    [JsonPropertyName("nameTranslations")]
    public RouteNameTranslations nameTranslations { get; set; }

    [JsonPropertyName("begin")]
    public int begin { get; set; }

    [JsonPropertyName("end")]
    public int end { get; set; }

    [JsonPropertyName("length")]
    public int length { get; set; }

    [JsonPropertyName("stopIds")]
    public List<int> stopIds { get; set; }

    [JsonPropertyName("stopOffsets")]
    public List<int> stopOffsets { get; set; }
}


public class LinesRoot
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("kind")]
    public string Kind { get; set; }

    [JsonPropertyName("number")]
    public string Number { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("nightly")]
    public bool Nightly { get; set; }

    [JsonPropertyName("routeIds")]
    public List<int> RouteIds { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("carrier")]
    public string Carrier { get; set; }
}

