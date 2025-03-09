namespace SMassTrans.Shared.ModeShift;

public class NearbyTimes
{
    public int id { get; set; }
    public NearbyTimesPatterns[] patterns { get; set; }
    public double distance { get; set; }
}

public class NearbyTimesPatterns
{
    public int routeId { get; set; }
    public int index { get; set; }
    public StopTimes[] stopTimes { get; set; }
}

public class StopTimes
{
    public int scheduledArrival { get; set; }
    public int scheduledDeparture { get; set; }
    public int realtimeArrival { get; set; }
    public int realtimeDeparture { get; set; }
    public int arrivalDelay { get; set; }
    public int departureDelay { get; set; }
    public bool timepoint { get; set; }
    public bool realtime { get; set; }
    public string realtimeState { get; set; }
    public int serviceDay { get; set; }
    public string headsign { get; set; }
}



