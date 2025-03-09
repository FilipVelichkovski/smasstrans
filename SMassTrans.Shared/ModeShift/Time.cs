namespace SMassTrans.Shared.ModeShift;

public class Time
{
    public TimeRoute route { get; set; }
    public Times[] times { get; set; }
}

public class TimeRoute
{
    public int routeId { get; set; }
    public int index { get; set; }
}

public class Times
{
    public int stopId { get; set; }
    public string scheduledArrival { get; set; }
    public string scheduledDeparture { get; set; }
    public int arrivalDelay { get; set; }
    public int departureDelay { get; set; }
    public bool realtime { get; set; }
    public string tripId { get; set; }
    public string headsign { get; set; }
}


