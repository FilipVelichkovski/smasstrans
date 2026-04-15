namespace SMassTrans.Shared;

/// <summary>
/// Represents a user's location update
/// </summary>
public class UserLocation
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime Timestamp { get; set; }
    public double? Accuracy { get; set; }
}

/// <summary>
/// API for tracking the user's location on mobile devices
/// Updates triggered by either 20 meters distance or 10 seconds elapsed, whichever comes first
/// </summary>
public interface IUserLocationTracker : IDisposable
{
    /// <summary>
    /// Event fired when user location is updated
    /// </summary>
    event EventHandler<UserLocation> LocationUpdated;

    /// <summary>
    /// Start tracking user location
    /// </summary>
    Task StartTrackingAsync();

    /// <summary>
    /// Stop tracking user location
    /// </summary>
    Task StopTrackingAsync();

    /// <summary>
    /// Get the most recent location
    /// </summary>
    UserLocation GetCurrentLocation();

    /// <summary>
    /// Check if tracking is currently active
    /// </summary>
    bool IsTracking { get; }
}
