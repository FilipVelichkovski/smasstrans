using System;
using SMassTrans.Shared;

namespace SMassTrans.Avalonia.Views;

public class UpcomingBusOnStop
{
    public UpcomingBusOnStop(LineRouteId lineAndRoute, double time)
    {
        LineAndRoute = lineAndRoute;
        Time = DateTime.Now.AddSeconds(time);
    }

    public LineRouteId LineAndRoute { get; set; }
    public DateTime Time { get; set; }
}
