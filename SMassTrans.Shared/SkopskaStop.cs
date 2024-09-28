namespace SMassTrans.Shared;

public class SkopskaStop : IStop
{
    public SkopskaStop()
    {
        
    }
    public SkopskaStop(string id, string num, string name, double lat, double lon)
    {
        StopId = int.Parse(id);
        StopName = name;
        Lat = lat;
        Lon = lon;
        StopNum = int.Parse(num);
    }

    public int StopId { get; set; }
    public int StopNum { get;set; }

    public string StopName { get; set;}

    public double Lat { get;set; }

    public double Lon { get; set;}

  
}