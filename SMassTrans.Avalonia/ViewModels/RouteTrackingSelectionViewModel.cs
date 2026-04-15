using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls;
using ReactiveUI;
using SMassTrans.Avalonia.Views;
using SMassTrans.Shared;

namespace SMassTrans.Avalonia.ViewModels;

/// <summary>
/// ViewModel for selecting a route to track
/// </summary>
public class RouteTrackingSelectionViewModel : ViewModelBase
{
    private readonly Skopska _client;
    private readonly RouteTrackingService _trackingService;
    private readonly MainViewModel _mainViewModel;
    
    private AvaloniaList<RouteSelectionModel> _availableRoutes = new();
    public AvaloniaList<RouteSelectionModel> AvailableRoutes
    {
        get => _availableRoutes;
        set => this.RaiseAndSetIfChanged(ref _availableRoutes, value);
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    private string _loadingMessage = "Loading routes...";
    public string LoadingMessage
    {
        get => _loadingMessage;
        set => this.RaiseAndSetIfChanged(ref _loadingMessage, value);
    }

    public event EventHandler<RouteSelectionModel> RouteSelected;

    public RouteTrackingSelectionViewModel(Skopska client, RouteTrackingService trackingService, MainViewModel mainViewModel = null)
    {
        _client = client;
        _trackingService = trackingService;
        _mainViewModel = mainViewModel;
    }

    public async void LoadRoutes()
    {
        IsLoading = true;
        LoadingMessage = "Loading routes...";
        Debug.WriteLine("RouteTrackingSelectionViewModel.LoadRoutes: Starting...");

        try
        {
            // Get all routes
            Debug.WriteLine("RouteTrackingSelectionViewModel.LoadRoutes: Calling GetAllRouteInfo...");
            var routeInfoEnum = await _client.GetAllRouteInfo();
            var routeInfoList = routeInfoEnum.ToList();
            Debug.WriteLine($"RouteTrackingSelectionViewModel.LoadRoutes: Got {routeInfoList.Count} routes");
            
            Debug.WriteLine("RouteTrackingSelectionViewModel.LoadRoutes: Calling GetStops...");
            var allStops = await _client.GetStops();
            Debug.WriteLine($"RouteTrackingSelectionViewModel.LoadRoutes: Got {allStops.Count} stops");

            if (routeInfoList.Count == 0)
            {
                IsLoading = false;
                LoadingMessage = "No routes available";
                Debug.WriteLine("RouteTrackingSelectionViewModel.LoadRoutes: No routes returned");
                return;
            }

            AvailableRoutes.Clear();

            // Group by line and create a model for each unique route
            var routesByLine = routeInfoList
                .GroupBy(r => r.LineRouteId)
                .Select(g => new RouteSelectionModel
                {
                    RouteInfo = g.First(),
                    StopsCount = g.First().StopIds.Count,
                    Direction = g.First().Direction,
                    Stops = g.First().StopIds
                        .Where(sid => int.TryParse(sid, out var id) && allStops.Any(s => s.StopId == id))
                        .Select(sid => allStops.First(s => int.TryParse(sid, out var id) && s.StopId == id))
                        .ToList() as List<SkopskaStop> ?? new()
                })
                .OrderBy(r => r.RouteInfo.LineRouteId.LineId)
                .ToList();

            foreach (var route in routesByLine)
            {
                AvailableRoutes.Add(route);
            }

            IsLoading = false;
            LoadingMessage = $"Found {AvailableRoutes.Count} routes";
            Debug.WriteLine($"RouteTrackingSelectionViewModel.LoadRoutes: Loaded {AvailableRoutes.Count} routes successfully");
        }
        catch (Exception ex)
        {
            IsLoading = false;
            LoadingMessage = $"Error loading routes: {ex.Message}";
            Debug.WriteLine($"RouteTrackingSelectionViewModel.LoadRoutes: Exception - {ex}");
        }
    }

    public void SelectRoute(RouteSelectionModel route)
    {
        RouteSelected?.Invoke(this, route);
        
        // If MainViewModel is available, handle navigation and start tracking directly
        if (_mainViewModel != null)
        {
            HandleRouteSelectionWithNavigation(route);
        }
    }

    /// <summary>
    /// Handle route selection by navigating to tracking view and starting tracking
    /// </summary>
    private async void HandleRouteSelectionWithNavigation(RouteSelectionModel route)
    {
        try
        {
            if (_mainViewModel.RouteTrackingService == null)
            {
                Debug.WriteLine("HandleRouteSelectionWithNavigation: RouteTrackingService is null");
                return;
            }

            Debug.WriteLine($"HandleRouteSelectionWithNavigation: Selected route {route.RouteInfo.Name} {route.RouteInfo.Direction}");
            
            // Create the tracking view model
            var trackingViewModel = new UserRouteTrackingViewModel(_mainViewModel.RouteTrackingService);
            var trackingView = new UserRouteTrackingView
            {
                DataContext = trackingViewModel
            };

            // Navigate to the tracking view
            _mainViewModel.GoForward(Tab.RouteTracking, trackingView);
            
            // Start tracking the route
            Debug.WriteLine("HandleRouteSelectionWithNavigation: Starting tracking...");
            await trackingViewModel.StartTrackingAsync(route.RouteInfo, route.Stops);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"HandleRouteSelectionWithNavigation: Error - {ex}");
        }
    }
}

/// <summary>
/// Model for displaying a route in the selection list
/// </summary>
public class RouteSelectionModel
{
    public IRouteInfo RouteInfo { get; set; }
    public int StopsCount { get; set; }
    public string Direction { get; set; }
    public List<SkopskaStop> Stops { get; set; }

    public string DisplayName => $"{RouteInfo.Name} - {Direction}";
    public string StopsInfo => $"{StopsCount} stops";
}
