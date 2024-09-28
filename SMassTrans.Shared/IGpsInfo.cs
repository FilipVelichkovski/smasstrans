namespace SMassTrans.Shared;

/// <summary>
/// The GPS info for one bus
/// </summary>
public interface IGpsInfo
{
    public bool Ignition { get; }
    public int CourseId { get; }
    public int StopId { get; }
    public int Delay { get; }
    public int Shift { get; set; }
    public int StopIndex { get; set; }
    public int RouteId { get; set; }
    public double Lat { get; }
    public double Lon { get; }
    public string Id { get; }
    public int Number { get; }
    public int Speed { get; }
    public int StartTime { get; set; }
    public string Status { get; set; }

}