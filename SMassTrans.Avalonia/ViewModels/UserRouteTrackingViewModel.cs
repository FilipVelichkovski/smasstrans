using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Collections;
using ReactiveUI;
using SMassTrans.Shared;

namespace SMassTrans.Avalonia.ViewModels;

/// <summary>
/// ViewModel for user route tracking - displays upcoming stops and notifications
/// </summary>
public class UserRouteTrackingViewModel : ViewModelBase
{
    
    
    private readonly RouteTrackingService _trackingService;
    private IRouteInfo _selectedRoute;
    private List<SkopskaStop> _selectedRouteStops;
    
    private AvaloniaList<UpcomingStopModel> _upcomingStops = new();
    public AvaloniaList<UpcomingStopModel> UpcomingStops
    {
        get => _upcomingStops;
        set => this.RaiseAndSetIfChanged(ref _upcomingStops, value);
    }

    private UpcomingStopModel _currentStop;
    public UpcomingStopModel CurrentStop
    {
        get => _currentStop;
        set => this.RaiseAndSetIfChanged(ref _currentStop, value);
    }

    private string _routeDisplay;
    public string RouteDisplay
    {
        get => _routeDisplay;
        set => this.RaiseAndSetIfChanged(ref _routeDisplay, value);
    }

    private string _statusMessage;
    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    private bool _isTracking;
    public bool IsTracking
    {
        get => _isTracking;
        set => this.RaiseAndSetIfChanged(ref _isTracking, value);
    }

 

    public UserRouteTrackingViewModel(RouteTrackingService trackingService)
    {
        _trackingService = trackingService;
        
        // Subscribe to tracking events
        _trackingService.UserAtStop += OnUserAtStop;
        _trackingService.UserApproachingStop += OnUserApproachingStop;
        _trackingService.StopIndexChanged += OnStopIndexChanged;
    }

    /// <summary>
    /// Start tracking user on a route
    /// </summary>
    public async Task StartTrackingAsync(IRouteInfo routeInfo, List<SkopskaStop> routeStops)
    {
        _selectedRoute = routeInfo;
        _selectedRouteStops = routeStops;
        
        RouteDisplay = $"{routeInfo.Name} - {routeInfo.Direction}";
        await _trackingService.StartTrackingRouteAsync(routeInfo, routeStops);
        IsTracking = true;
        StatusMessage = "Tracking started...";
        
        UpdateUpcomingStops();
    }

    /// <summary>
    /// Stop tracking the current route
    /// </summary>
    public async Task StopTrackingAsync()
    {
        await _trackingService.StopTrackingAsync();
        IsTracking = false;
        StatusMessage = "Tracking stopped";
        UpcomingStops.Clear();
        CurrentStop = null;
    }

    private void OnStopIndexChanged(object sender, int newIndex)
    {
        UpdateUpcomingStops();
        
        if (newIndex < _selectedRouteStops.Count)
        {
            var stop = _selectedRouteStops[newIndex];
            StatusMessage = $"Now near: {stop.StopName}";
        }
    }

    private void OnUserApproachingStop(object sender, UpcomingStopModel upcomingStop)
    {
        var message = $"Approaching: {upcomingStop.Stop.StopName} ({upcomingStop.DistanceDisplay})";
        LocationGatherer.ShowNotification(message);
        StatusMessage = message;
    }

    private void OnUserAtStop(object sender, UpcomingStopModel atStop)
    {
        var message = $"🎯 You are at: {atStop.Stop.StopName}";
        LocationGatherer.ShowNotification(message);
        StatusMessage = message;
        UpdateUpcomingStops();
    }

    private void UpdateUpcomingStops()
    {
        var upcomingStops = _trackingService.GetUpcomingStops(5);
        
        UpcomingStops.Clear();
        foreach (var stop in upcomingStops)
        {
            UpcomingStops.Add(stop);
        }

        // Set current stop as the next one
        if (UpcomingStops.Count > 0)
        {
            CurrentStop = UpcomingStops[0];
        }
    }
}
