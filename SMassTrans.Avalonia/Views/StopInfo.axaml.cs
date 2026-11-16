using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using SMassTrans.Avalonia.ViewModels;
using SMassTrans.Shared;
using SMassTrans.Shared.ModeShift;

namespace SMassTrans.Avalonia.Views;


public partial class StopInfo : UserControl
{
    private IStop _stop;
    private Tab bsource;
    private ModeShiftClient _client;
    private MainViewModel _mainViewModel;
    /// <summary>
    /// DO NOT USE THIS METHOD
    /// </summary>
    public StopInfo()
    {
        // See: https://github.com/AvaloniaUI/Avalonia/issues/11312
    }
    /// <summary>
    /// Creates new stopinfo
    /// </summary>
    public StopInfo(IStop stop, MainViewModel mainViewModel, Tab source)
    {
        InitializeComponent();
        bsource = source;
        _stop = stop;
        _mainViewModel = mainViewModel;
        _client=mainViewModel.client;
        DataContext = new StopInfoModel();
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
            var linesAndTimes = await _client.GetLiveStopLinesForStopAsync(_stop.StopId.ToString());
            var upcomingBuses = linesAndTimes.SelectMany(x => x.Value.Select(y => new UpcomingBusOnStop(x.Key, y))).ToList();
            upcomingBuses.Sort((x,y)=>DateTime.Compare( x.Time, y.Time));
            Dispatcher.UIThread.Invoke(() =>
            {
                if (DataContext is StopInfoModel a )
                {
                    a.BusesOnStop = new(upcomingBuses);
                }
            });
        });
    }
 
  
    private void BackRequested(object? sender, RoutedEventArgs e)
    {
        
        e.Handled = true;
        _mainViewModel.GoBack(bsource);

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
    private void ViewStops(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: UpcomingBusOnStop bus } a)
        {
           
            var stops = _mainViewModel.client.GetRouteInfo(bus.LineAndRoute).GetAwaiter().GetResult().StopIds
                .Select(x => _mainViewModel.client.GetStop(x).GetAwaiter().GetResult())
                .Where(x => x != null)
                .Cast<IStop>()
                .ToList();
            var newpage=new StopSelectionLine(_mainViewModel, bsource)
            {
                DataContext = new StopSelectionLineModel()
                {
                    LinePageStops = stops,
                }
            };
            _mainViewModel.GoForward(bsource, newpage);
        }
    }

    private async void TrackRoute(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control { DataContext: UpcomingBusOnStop bus }) return;
        if (_mainViewModel.RouteTrackingService == null)
        {
            Debug.WriteLine("Route tracking service not initialized");
            return;
        }

        var routeInfo = _mainViewModel.client.GetRouteInfo(bus.LineAndRoute).GetAwaiter().GetResult();
        var routeStops = routeInfo.StopIds
            .Select(x => _mainViewModel.client.GetStop(x).GetAwaiter().GetResult())
            .Where(x => x != null)
            .Cast<IStop>()
            .ToList();

        var trackingViewModel = new UserRouteTrackingViewModel(_mainViewModel.RouteTrackingService);
        var trackingView = new UserRouteTrackingView
        {
            DataContext = trackingViewModel
        };

        _mainViewModel.GoForward(Tab.RouteTracking, trackingView);
        
        // Start tracking the route
        await trackingViewModel.StartTrackingAsync(routeInfo, routeStops);
    }

}
