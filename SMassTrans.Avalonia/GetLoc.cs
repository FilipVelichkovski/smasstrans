using System;
using System.Threading.Tasks;
using SMassTrans.Shared;

namespace SMassTrans.Avalonia;

public static class LocationGatherer
{
    public static Func<Task<Tuple<double, double>>> GetLoc { get; set; } = () => Task.FromResult(new Tuple<double, double>(42.000713d, 21.440125d));

    public static Func<IUserLocationTracker> GetLocationTracker { get; set; } = ()=>null;
    public static Action<string> ShowNotification { get; set; } = x => Console.WriteLine(x);
    
    
    public static bool HasGps = false;
}
