using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using SMassTrans.Avalonia.ViewModels;
using SMassTrans.Shared;

namespace SMassTrans.Avalonia.Views;

public partial class StopInfo : UserControl
{
    private IStop _stop;
    private readonly EventHandler<LineRouteId> _gpsLine;
    private bool GPS = false;
    private Skopska _client;
    public StopInfo(IStop stop, Skopska client, bool gps = false)
    {
        GPS = gps;
        InitializeComponent();
        _stop = stop;
        _client = client;
        Refresh();
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is not null)
        {
            topLevel.BackRequested += BackRequested;
        }
    }

    void Refresh()
    {
        Task.Run(async () =>
        {
            var linesAndTimes = await _client.GetStopLinesForStopId(_stop.StopId.ToString("0000"));
            var upcomingBuses = linesAndTimes.SelectMany(x => x.Value.Select(y => new UpcomingBusOnStop(x.Key, y))).ToList();
            upcomingBuses.Sort((x,y)=>DateTime.Compare( x.Time, y.Time));
            Dispatcher.UIThread.Invoke(() =>
            {
                if (DataContext is MainViewModel a )
                {
                    a.BusesOnStop = new(upcomingBuses);
                }
            });
        });
    }
 
  
    private void BackRequested(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel a) return;
        e.Handled = true;
        if (GPS)
        {
            a.StopGPSContent = a.PrevStopGPSContent;
        }
        else
        {
            a.StopContent = a.PrevStopContent;
        }
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is not null)
        {
            topLevel.BackRequested -= BackRequested;
        }
    }

    private void RefreshContainer_OnRefreshRequested(object? sender, RefreshRequestedEventArgs e)
    {
        var deferral = e.GetDeferral();
        Refresh();
        deferral.Complete();
    }

    private void RefreshRequested(object? sender, RoutedEventArgs e)
    {
        Refresh();
    }

 
    private void LinePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is Control  { DataContext: UpcomingBusOnStop bus } x)
        {
            x.ContextMenu.Open();
        }
    }


    private void LineGPS(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: UpcomingBusOnStop bus } a)
        {
            MainViewModel.SetGpsLine.Invoke(this,  bus.LineAndRoute);
        }
    }
}

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
public class LineRouteIdConverter : IValueConverter
{
    public static readonly LineRouteIdConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, 
        CultureInfo culture)
    {
        if (value is LineRouteId linerouteID  
                                       && targetType.IsAssignableTo(typeof(string)))
        {
           var res= Task.Run(async () =>
            {
                return await MainView.client.GetLineInfo(linerouteID);
            }).GetAwaiter().GetResult();
            return res.Name;
        }
        return new BindingNotification(new InvalidCastException(), 
            BindingErrorType.Error);
    }

    public object ConvertBack(object? value, Type targetType, 
        object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}