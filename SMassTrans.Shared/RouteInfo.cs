namespace SMassTrans.Shared;

public class RouteInfo : IRouteInfo
{
    public  RouteInfo( LineRouteId lineRouteId,  string name,  string direction, List<string> stopIds)
    {
        LineRouteId = lineRouteId;
        Name = name;
        Direction = direction;
        StopIds= stopIds;
    }

    public RouteInfo()
    {
        
    }
    public LineRouteId LineRouteId { get; set; }
    public string Name { get; set; } 
    public string Direction { get; set; } 
    public List<string> StopIds { get; set; } 

    
}