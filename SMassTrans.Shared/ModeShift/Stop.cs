namespace SMassTrans.Shared.ModeShift;

public class Stop
{
    public int id { get; set; }
    public string name { get; set; }
    public string code { get; set; }
    public double latitude { get; set; }
    public double longitude { get; set; }
    public StopPatterns[] patterns { get; set; }
}

public class StopPatterns
{
    public int index { get; set; }
    public int routeId { get; set; }
    public int toStopId { get; set; }
    public int direction { get; set; }
    public int fromStopId { get; set; }
    public int viaStopId { get; set; }
}
