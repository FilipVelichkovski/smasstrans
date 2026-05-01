using System;
using System.Collections.Generic;
using Avalonia.Collections;
using Avalonia.Controls;
using ReactiveUI;
using SMassTrans.Avalonia.Views;
using SMassTrans.Shared;

namespace SMassTrans.Avalonia.ViewModels;

public enum Tab
{
    Stop,
    StopGPS,
    Line,
    RouteTracking
}
public class MainViewModel : ViewModelBase
{
    public Skopska client;
    public static EventHandler<LineRouteId> SetGpsLine;
    private AvaloniaList<IStop> _stops = new();
    public AvaloniaList<IStop> Stops
    {
        get => _stops;
        set => this.RaiseAndSetIfChanged(ref _stops, value);
    }
    private AvaloniaList<IRouteInfo> _lines = new();
    public AvaloniaList<IRouteInfo> Lines
    {
        get => _lines;
        set => this.RaiseAndSetIfChanged(ref _lines, value);
    }
    private Control _stopContent = new StopSelection();
    public Control StopContent
    {
        get => _stopContent;
        set => this.RaiseAndSetIfChanged(ref _stopContent, value);
    }
    public Stack<Control> PrevStopContent = new();

    private Control _stopGPSContent = new StopSelectionGPS();
    public Control StopGPSContent
    {
        get => _stopGPSContent;
        set => this.RaiseAndSetIfChanged(ref _stopGPSContent, value);
    }
    public Stack<Control> PrevStopGPSContent = new();

    private Control? _linePage = new LineView();
    public Control LinePage
    {
        get => _linePage;
        set => this.RaiseAndSetIfChanged(ref _linePage, value);
    }

    private RouteTrackingService _routeTrackingService;
    public RouteTrackingService RouteTrackingService
    {
        get => _routeTrackingService;
        set => this.RaiseAndSetIfChanged(ref _routeTrackingService, value);
    }

    private Control _routeTrackingPage;
    public Control RouteTrackingPage
    {
        get => _routeTrackingPage;
        set => this.RaiseAndSetIfChanged(ref _routeTrackingPage, value);
    }

    public void GoBack(Tab tab)
    {
        switch (tab)
        {
            case Tab.Stop:
                StopContent = PrevStopContent.Pop();
                break;
            case Tab.StopGPS:
                StopGPSContent = PrevStopGPSContent.Pop();
                break;
            case Tab.Line:
                LinePage = PrevLinePages.Pop();
                break;
            case Tab.RouteTracking:
                RouteTrackingPage = PrevRouteTrackingPages.Pop();
                break;
        }
    }
    public void GoForward(Tab tab, Control next)
    {
        switch (tab)
        {
            case Tab.Stop:
                PrevStopContent.Push(StopContent);
                StopContent = next;
                break;
            case Tab.StopGPS:
                PrevStopGPSContent.Push(StopGPSContent);
                StopGPSContent = next;
                break;
            case Tab.Line:
                PrevLinePages.Push(LinePage);
                LinePage = next;
                break;
            case Tab.RouteTracking:
                PrevRouteTrackingPages.Push(RouteTrackingPage);
                RouteTrackingPage = next;
                break;
        }
    }

    public Stack<Control> PrevLinePages = new();
    public Stack<Control> PrevRouteTrackingPages = new();

    private Control? _routeFindPage = new RouteFindView();
    public Control? RouteFindPage
    {
        get => _routeFindPage;
        set => this.RaiseAndSetIfChanged(ref _routeFindPage, value);
    }

    private string _mapName = "Map";
    public string MapName
    {
        get => _mapName;
        set => this.RaiseAndSetIfChanged(ref _mapName, value);
    }

    private string? _routeFrom;
    public string? RouteFrom
    {
        get => _routeFrom;
        set => this.RaiseAndSetIfChanged(ref _routeFrom, value);
    }

    private string? _routeTo;
    public string? RouteTo
    {
        get => _routeTo;
        set => this.RaiseAndSetIfChanged(ref _routeTo, value);
    }

    public MainViewModel()
    {
        // Initialize route tracking service (will be set properly by the view)
        _routeTrackingService = null;
    }
}
