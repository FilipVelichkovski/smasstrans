using System;
using System.Threading.Tasks;

namespace SMassTrans.Avalonia;

public static class LocationGatherer
{
    public static Func<Task<Tuple<double,double>>> GetLoc { get; set; } = () => Task.FromResult(new Tuple<double,double> (42.000713d, 21.440125d));
}