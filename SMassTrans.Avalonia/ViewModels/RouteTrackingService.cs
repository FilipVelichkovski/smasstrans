using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SMassTrans.Shared;

namespace SMassTrans.Avalonia.ViewModels;

/// <summary>
/// Service for tracking user location on a selected route and detecting proximity to stops
/// </summary>
public class RouteTrackingService
{
    private IRouteInfo _activeRoute;
    private List<IStop> _routeStops;
    private IUserLocationTracker? _locationTracker;
    private int _currentStopIndex = -1;
    
    // Distance threshold for "approaching" a stop (meters)
    private const double APPROACHING_THRESHOLD = 300;
    private const double AT_STOP_THRESHOLD = 50;

    public event EventHandler<UpcomingStopModel> UserApproachingStop;  // When user is ~300m away
    public event EventHandler<UpcomingStopModel> UserAtStop;            // When user is ~50m away
    public event EventHandler<int> StopIndexChanged;                   // When moving to next stop

    public IRouteInfo ActiveRoute => _activeRoute;
    public int CurrentStopIndex => _currentStopIndex;
    public bool IsTracking { get; private set; }

    public RouteTrackingService(IUserLocationTracker locationTracker)
    {
        _locationTracker = locationTracker;
        IsTracking = false;
    }

    /// <summary>
    /// Start tracking user on a specific route
    /// </summary>
    public async Task StartTrackingRouteAsync(IRouteInfo routeInfo, List<IStop> routeStops)
    {
        if (IsTracking)
            await StopTrackingAsync();

        _activeRoute = routeInfo;
        _routeStops = routeStops;
        _currentStopIndex = FindNearestStopIndex();
        IsTracking = true;

        if (_locationTracker != null)
        {
            _locationTracker.LocationUpdated += OnUserLocationUpdated;
            await _locationTracker.StartTrackingAsync();
        }
        Debug.WriteLine($"Started tracking route: {routeInfo.Name} {routeInfo.Direction}");
    }

    /// <summary>
    /// Stop tracking the current route
    /// </summary>
    public async Task StopTrackingAsync()
    {
        if (!IsTracking)
            return;

        IsTracking = false;
        if (_locationTracker != null)
        {
            _locationTracker.LocationUpdated -= OnUserLocationUpdated;
            await _locationTracker.StopTrackingAsync();
        }
        _activeRoute = null;
        _routeStops = null;
        _currentStopIndex = -1;
        
        Debug.WriteLine("Stopped tracking route");
    }

    /// <summary>
    /// Get upcoming stops from current position (next 5 stops)
    /// </summary>
    public List<UpcomingStopModel> GetUpcomingStops(int count = 5)
    {
        if (_routeStops == null || _routeStops.Count == 0)
            return new List<UpcomingStopModel>();

        // If current stop index is not set, start from the beginning
        int startIndex = _currentStopIndex < 0 ? 0 : _currentStopIndex;

        var upcomingStops = new List<UpcomingStopModel>();
        
        for (int i = startIndex; i < Math.Min(startIndex + count, _routeStops.Count); i++)
        {
            var stop = _routeStops[i];
            var currentLocation = _locationTracker?.GetCurrentLocation();
            
            double distance = currentLocation != null
                ? CoordinateHelper.GetDistanceTo(
                    currentLocation.Latitude,
                    currentLocation.Longitude,
                    stop.Lat,
                    stop.Lon)
                : double.MaxValue; // Unknown distance if no location

            var status = double.IsInfinity(distance) || double.IsNaN(distance) ? StopProximityStatus.Upcoming :
                        distance < AT_STOP_THRESHOLD ? StopProximityStatus.AtStop :
                        distance < APPROACHING_THRESHOLD ? StopProximityStatus.Approaching :
                        StopProximityStatus.Upcoming;

            upcomingStops.Add(new UpcomingStopModel
            {
                Stop = stop,
                Distance = distance,
                IsNextStop = i == startIndex,
                Status = status,
                StopIndex = i
            });
        }

        return upcomingStops;
    }

    private void OnUserLocationUpdated(object sender, UserLocation location)
    {
        if (!IsTracking || _routeStops == null)
            return;

        try
        {
            // Find closest stop on the route
            int newStopIndex = FindClosestStopIndex(location);
            
            if (newStopIndex != _currentStopIndex)
            {
                _currentStopIndex = newStopIndex;
                StopIndexChanged?.Invoke(this, newStopIndex);
                Debug.WriteLine($"User at stop index: {newStopIndex} - {_routeStops[newStopIndex].StopName}");
            }

            // Check if approaching or at next stop
            if (_currentStopIndex < _routeStops.Count)
            {
                var nextStop = _routeStops[_currentStopIndex];
                double distance = CoordinateHelper.GetDistanceTo(
                    location.Latitude,
                    location.Longitude,
                    nextStop.Lat,
                    nextStop.Lon);

                var upcomingStop = new UpcomingStopModel
                {
                    Stop = nextStop,
                    Distance = distance,
                    IsNextStop = true,
                    Status = distance < AT_STOP_THRESHOLD ? StopProximityStatus.AtStop : StopProximityStatus.Approaching,
                    StopIndex = _currentStopIndex
                };

                if (distance < AT_STOP_THRESHOLD)
                {
                    UserAtStop?.Invoke(this, upcomingStop);
                }
                else if (distance < APPROACHING_THRESHOLD)
                {
                    UserApproachingStop?.Invoke(this, upcomingStop);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in route tracking: {ex}");
        }
    }

    private int FindNearestStopIndex()
    {
        if (_locationTracker == null || _routeStops == null || _routeStops.Count == 0)
        {
            Debug.WriteLine("Location tracker not available or no route stops, starting from stop 0");
            return 0; // Start from beginning if no location tracker or stops
        }

        var currentLocation = _locationTracker.GetCurrentLocation();
        if (currentLocation == null)
        {
            Debug.WriteLine("Location not available yet, starting from stop 0");
            return 0; // Start from beginning if no location
        }

        double minDistance = double.MaxValue;
        int nearestIndex = 0;

        for (int i = 0; i < _routeStops.Count; i++)
        {
            double distance = CoordinateHelper.GetDistanceTo(
                currentLocation.Latitude,
                currentLocation.Longitude,
                _routeStops[i].Lat,
                _routeStops[i].Lon);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    private int FindClosestStopIndex(UserLocation location)
    {
        if (_routeStops == null || _routeStops.Count == 0)
            return 0;

        // Start from current index and look nearby first (efficiency)
        double minDistance = double.MaxValue;
        int closestIndex = _currentStopIndex;

        // Check a range around current stop
        int startSearch = Math.Max(0, _currentStopIndex - 2);
        int endSearch = Math.Min(_routeStops.Count, _currentStopIndex + 5);

        for (int i = startSearch; i < endSearch; i++)
        {
            double distance = CoordinateHelper.GetDistanceTo(
                location.Latitude,
                location.Longitude,
                _routeStops[i].Lat,
                _routeStops[i].Lon);

            if (distance < minDistance)
            {
                minDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }
}

/// <summary>
/// Represents status of user proximity to a stop
/// </summary>
public enum StopProximityStatus
{
    Upcoming,      // More than 300m away
    Approaching,   // Between 50m and 300m away
    AtStop         // Less than 50m away
}

/// <summary>
/// Model for displaying an upcoming stop
/// </summary>
public class UpcomingStopModel
{
    public IStop Stop { get; set; }
    public double Distance { get; set; }
    public bool IsNextStop { get; set; }
    public StopProximityStatus Status { get; set; }
    public int StopIndex { get; set; }

    public string DistanceDisplay => double.IsInfinity(Distance) || double.IsNaN(Distance)
        ? "?.?"
        : $"{Math.Round(Distance)}m";
    
    public string StatusDisplay => Status switch
    {
        StopProximityStatus.AtStop => "🎯 At Stop",
        StopProximityStatus.Approaching => "📍 Approaching",
        _ => "⭕ Upcoming"
    };
}
