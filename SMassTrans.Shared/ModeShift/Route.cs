namespace SMassTrans.Shared.ModeShift;

public class Route 
{
    public int id { get; set; }
    public string shortName { get; set; }
    public string longName { get; set; }
    public int type { get; set; }
    public RoutePatterns[] patterns { get; set; }
    public string inbound { get; set; }
    public string outbound { get; set; }
}

public class RoutePatterns
{
    public int index { get; set; }
    public int routeId { get; set; }
    public int toStopId { get; set; }
    public int[] stops { get; set; }
    public string geometry { get; set; }
    public int direction { get; set; }
    public string patternHash { get; set; }
    public int fromStopId { get; set; }
    public int viaStopId { get; set; }
}
