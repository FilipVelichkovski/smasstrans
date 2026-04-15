using System;
using System.Threading.Tasks;
using SMassTrans.Shared;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using Debug = System.Diagnostics.Debug;

namespace SMassTrans.Avalonia.Android;

/// <summary>
/// Native implementation of user location tracking using MAUI Geolocation API
/// </summary>
public class AndroidUserLocationTracker : IUserLocationTracker, IDisposable
{
    private UserLocation _currentLocation;
    private bool _isTracking;
    private readonly object _locationLock = new();

    public event EventHandler<UserLocation> LocationUpdated;

    public bool IsTracking => _isTracking;

    public AndroidUserLocationTracker()
    {
        _currentLocation = null;
        _isTracking = false;
    }

    public Task StartTrackingAsync()
    {
        if (_isTracking)
        {
            Debug.WriteLine("Location tracking already active");
            return Task.CompletedTask;
        }

        return OnStartListeningAsync();
    }

    private async Task OnStartListeningAsync()
    {
        try
        {
            Debug.WriteLine("StartListeningAsync: Starting foreground location listening...");
            _isTracking = true;

            Geolocation.LocationChanged += Geolocation_LocationChanged;

            var request = new GeolocationListeningRequest(GeolocationAccuracy.Best);
            request.MinimumTime = TimeSpan.FromSeconds(1);
            
            var success = await Geolocation.StartListeningForegroundAsync(request);

            if (success)
            {
                Debug.WriteLine("Started listening for foreground location updates");
            }
            else
            {
                Debug.WriteLine("Couldn't start listening for location updates");
                _isTracking = false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in StartListeningAsync: {ex}");
            _isTracking = false;
        }
    }

    private void Geolocation_LocationChanged(object sender, GeolocationLocationChangedEventArgs e)
    {
        if (e?.Location == null)
            return;

        var location = e.Location;
        var newLocation = new UserLocation
        {
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            Timestamp = DateTime.UtcNow,
            Accuracy = (float)location.Accuracy
        };

        bool shouldUpdate = false;

        lock (_locationLock)
        {
            if (_currentLocation == null)
            {
                shouldUpdate = true;
            }
            else
            {
               /* double distance = CoordinateHelper.GetDistanceTo(
                    _currentLocation.Latitude,
                    _currentLocation.Longitude,
                    newLocation.Latitude,
                    newLocation.Longitude);*/

                //if (distance >= 2) // DISTANCE_THRESHOLD_METERS
                {
                    shouldUpdate = true;
                }
            }

            if (shouldUpdate)
            {
                _currentLocation = newLocation;
            }
        }

        if (!shouldUpdate) return;
        LocationUpdated?.Invoke(this, newLocation);
        Debug.WriteLine($"Location updated: {newLocation.Latitude}, {newLocation.Longitude}, Accuracy: {location.Accuracy}m");
    }

    public Task StopTrackingAsync()
    {
        if (!_isTracking)
            return Task.CompletedTask;

        try
        {
            _isTracking = false;
            Geolocation.LocationChanged -= Geolocation_LocationChanged;
            Geolocation.StopListeningForeground();
            Debug.WriteLine("Location tracking stopped");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error stopping location tracking: {ex}");
        }

        return Task.CompletedTask;
    }

    public UserLocation GetCurrentLocation()
    {
        lock (_locationLock)
        {
            return _currentLocation;
        }
    }

    public void Dispose()
    {
        StopTrackingAsync().GetAwaiter().GetResult();
    }
}
