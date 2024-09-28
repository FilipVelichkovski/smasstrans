namespace SMassTrans.Shared;

public interface IStop
{
    public int StopId { get; }
    public int StopNum {get;}

    public string StopName { get; }
    public double Lat{get;}
    
    public double Lon{get;}
}