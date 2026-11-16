using System;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SMassTrans.Avalonia.ViewModels;
using SMassTrans.Shared;

namespace SMassTrans.Avalonia.Views;

public partial class LineView : UserControl
{
    public LineView()
    {
        InitializeComponent();
    }
    private void LineDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control { DataContext: IRouteInfo context } a)
        {
            a.ContextMenu.Open();
        }
    }
    private void LineGPS(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: IRouteInfo context })
        {
            MainViewModel.SetGpsLine.Invoke(this,  context.LineRouteId);
        }
    }
    private void CopyStops(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control { DataContext: IRouteInfo context }) return;
        var topLevel = TopLevel.GetTopLevel(this);
        StringBuilder s = new();
        s.AppendLine($"{context.Name}: ");
        if (context is RouteInfo routeInfo && DataContext is MainViewModel mw)
        {
            var stops = routeInfo.StopIds
                .Select(x => mw.client.GetStop(x).GetAwaiter().GetResult())
                .Where(x => x != null)
                .ToList();
            foreach (var stop in stops)
            {
                s.AppendLine($"{stop!.StopName}");
            }
        }
        topLevel.Clipboard.SetTextAsync(s.ToString());
    }
    private void ViewStops(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control { DataContext: IRouteInfo context }) return;
        if (context is not RouteInfo routeInfo || DataContext is not MainViewModel mainViewModel) return;
        var stops = routeInfo.StopIds
            .Select(x => mainViewModel.client.GetStop(x).GetAwaiter().GetResult())
            .Where(x => x != null)
            .Cast<IStop>()
            .ToList();
        mainViewModel.GoForward(Tab.Line,new StopSelectionLine(mainViewModel, Tab.Line)
        {
            DataContext = new StopSelectionLineModel()
            {
                LinePageStops = stops,
            }
        });
    }

    private async void TrackRoute(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control { DataContext: IRouteInfo context }) return;
        if (context is not RouteInfo routeInfo || DataContext is not MainViewModel mainViewModel) return;
        if (mainViewModel.RouteTrackingService == null)
        {
            Debug.WriteLine("Route tracking service not initialized");
            return;
        }

        var stops = routeInfo.StopIds
            .Select(x => mainViewModel.client.GetStop(x).GetAwaiter().GetResult())
            .Where(x => x != null)
            .Cast<IStop>()
            .ToList();

        var trackingViewModel = new UserRouteTrackingViewModel(mainViewModel.RouteTrackingService);
        var trackingView = new UserRouteTrackingView
        {
            DataContext = trackingViewModel
        };

        mainViewModel.GoForward(Tab.RouteTracking, trackingView);
        
        // Start tracking the route
        await trackingViewModel.StartTrackingAsync(routeInfo, stops);
    }

    private void TextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
         if (DataContext is not MainViewModel mwv) return;
        if (!string.IsNullOrWhiteSpace(SearchBox.Text))
        {
            var searchTerm = SearchBox.Text.ToLowerInvariant();
            mwv.Lines = new AvaloniaList<IRouteInfo>(mwv.Lines.OrderBy(x =>  
            {
                var distance1 = Search.ModifiedLevenshteinDistance(x.Name.ToLowerInvariant(), searchTerm);
                var lineInfo = mwv.client.GetLineInfo(x.LineRouteId).GetAwaiter().GetResult();
                var distance2 = Search.ModifiedLevenshteinDistance(lineInfo.Name.ToLowerInvariant(), searchTerm);
                return Math.Min(distance1, distance2);
            })); 
        }
        else
        {
            mwv.Lines = new AvaloniaList<IRouteInfo>(mwv.Lines.OrderBy(x => x.LineRouteId.LineId));
        }
    }
}