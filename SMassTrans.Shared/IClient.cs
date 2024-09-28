namespace SMassTrans.Shared;

public interface IClient<TStop, TLineInfo> where TStop : IStop where TLineInfo : ILineInfo
{
    public bool IsInitialised { get; }
    public Task InitAsync();
    public Task<List<TStop>> GetStops(bool ignoreCache = false);
    public Task<IEnumerable<RouteInfo>> GetAllRouteInfo();
    public Task<RouteInfo> GetRouteInfo(LineRouteId lineRouteId);
    public Task<TLineInfo> GetLineInfo(LineRouteId lineRouteId);
    public Task<IReadOnlyDictionary<LineRouteId, IReadOnlyList<int>>> GetStopLinesForStopId(string stopId);
    public Task<TLineInfo> GetLineInfo(string lineId);

}