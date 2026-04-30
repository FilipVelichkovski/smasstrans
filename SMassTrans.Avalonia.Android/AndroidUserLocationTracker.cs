using System;
using System.Threading.Tasks;
using SMassTrans.Shared;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using Android.Content;
using Android.OS;
using Debug = System.Diagnostics.Debug;

namespace SMassTrans.Avalonia.Android;

/// <summary>
/// Native implementation of user location tracking using a foreground service
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
            Debug.WriteLine("StartListeningAsync: Starting foreground location service...");
            _isTracking = true;

            LocationService.LocationChanged += Service_LocationChanged;

            var intent = new Intent(Platform.AppContext, typeof(LocationService));
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                Platform.AppContext.StartForegroundService(intent);
            }
            else
            {
                Platform.AppContext.StartService(intent);
            }

            Debug.WriteLine("Requested LocationService to start");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in StartListeningAsync: {ex}");
            _isTracking = false;
        }
    }

    private void Service_LocationChanged(object sender, UserLocation newLocation)
    {
        bool shouldUpdate = false;

        lock (_locationLock)
        {
            // We could add distance filtering here if needed
            shouldUpdate = true;

            if (shouldUpdate)
            {
                _currentLocation = newLocation;
            }
        }

        if (!shouldUpdate) return;
        LocationUpdated?.Invoke(this, newLocation);
        Debug.WriteLine($"Location updated from service: {newLocation.Latitude}, {newLocation.Longitude}");
    }

    public Task StopTrackingAsync()
    {
        if (!_isTracking)
            return Task.CompletedTask;

        try
        {
            _isTracking = false;
            LocationService.LocationChanged -= Service_LocationChanged;
            
            var intent = new Intent(Platform.AppContext, typeof(LocationService));
            Platform.AppContext.StopService(intent);
            
            Debug.WriteLine("Location tracking service stopped");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error stopping location tracking service: {ex}");
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
