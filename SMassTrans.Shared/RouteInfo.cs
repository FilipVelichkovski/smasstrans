namespace SMassTrans.Shared;

public record RouteInfo(LineRouteId LineRouteId, string Name, string Direction) : IRouteInfo;