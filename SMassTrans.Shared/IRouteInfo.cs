namespace SMassTrans.Shared;

public interface IRouteInfo 
{
    public LineRouteId LineRouteId { get; }
    public string Name{get;}
    public string Direction{get;}
}