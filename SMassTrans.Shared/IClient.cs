namespace SMassTrans.Shared;

public interface IClient<TStop, TLineInfo> where TStop : IStop where TLineInfo : ILineInfo
{
    public bool IsInitialised { get; }
    public Task InitAsync();
    public Task<List<TStop>> GetStops(bool ignoreCache = false);
    public Task<IEnumerable<RouteInfo>> GetAllRouteInfo();
    public Task<IEnumerable<TLineInfo>> GetAllLineInfo();

    public Task<RouteInfo> GetRouteInfo(LineRouteId lineRouteId);
    public Task<TLineInfo> GetLineInfo(LineRouteId lineRouteId);
    public Task<IEnumerable<KeyValuePair<LineRouteId, IReadOnlyList<int>>>> GetLiveStopLinesForStopAsync(string stopId);
    public Task<TLineInfo> GetLineInfo(string lineId);

}