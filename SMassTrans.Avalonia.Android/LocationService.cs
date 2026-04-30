using System;
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using SMassTrans.Shared;
using System.Threading.Tasks;

namespace SMassTrans.Avalonia.Android;

[Service(ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeLocation)]
public class LocationService : Service
{
    private const int NOTIFICATION_ID = 10001;
    private const string CHANNEL_ID = "route_tracking_channel";
    
    public static event EventHandler<UserLocation>? LocationChanged;
    public static bool IsRunning { get; private set; }

    public override IBinder? OnBind(Intent intent) => null;
    public const string ACTION_STOP_SERVICE = "STOP_SERVICE";

    public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
    {
        if (intent?.Action == ACTION_STOP_SERVICE)
        {
            // This stops the service and triggers OnDestroy
            StopForeground(true);
            StopSelf();
            return StartCommandResult.NotSticky;
        }

        if (IsRunning) return StartCommandResult.Sticky;

        CreateNotificationChannel();
        var notification = CreateNotification("Starting route tracking...");
    
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            StartForeground(NOTIFICATION_ID, notification, global::Android.Content.PM.ForegroundService.TypeLocation);
        else
            StartForeground(NOTIFICATION_ID, notification);

        IsRunning = true;
        _ = StartTrackingAsync();

        return StartCommandResult.Sticky;
    }

    private Notification CreateNotification(string contentText)
    {
        var intent = new Intent(this, typeof(MainActivity));
        intent.AddFlags(ActivityFlags.ClearTop);
        var pendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.Immutable);

        // Create the Intent for the Stop Button
        var stopIntent = new Intent(this, typeof(LocationService));
        stopIntent.SetAction(ACTION_STOP_SERVICE);
        var stopPendingIntent = PendingIntent.GetService(this, 0, stopIntent, PendingIntentFlags.Immutable);

        return new NotificationCompat.Builder(this, CHANNEL_ID)
            .SetContentTitle("SMassTrans")
            .SetContentText(contentText)
            .SetSmallIcon(global::Android.Resource.Drawable.IcMenuMyLocation)
            .SetOngoing(true)
            .SetContentIntent(pendingIntent)
            // Add the Action Button here
            .AddAction(global::Android.Resource.Drawable.IcDelete, "Stop Tracking", stopPendingIntent)
            .Build();
    }

    private void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(CHANNEL_ID, "Route Tracking", NotificationImportance.Low)
            {
                Description = "Maintains route tracking while the app is in the background"
            };
            var notificationManager = (NotificationManager)GetSystemService(NotificationService)!;
            notificationManager.CreateNotificationChannel(channel);
        }
    }

    private async Task StartTrackingAsync()
    {
        try
        {
            Geolocation.LocationChanged += OnLocationChanged;
            var request = new GeolocationListeningRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(1));
            await Geolocation.StartListeningForegroundAsync(request);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error starting tracking in service: {ex}");
        }
    }

    private void OnLocationChanged(object? sender, GeolocationLocationChangedEventArgs e)
    {
        var userLocation = new UserLocation
        {
            Latitude = e.Location.Latitude,
            Longitude = e.Location.Longitude,
            Timestamp = DateTime.UtcNow,
            Accuracy = e.Location.Accuracy
        };

        UpdateNotification($"Current location: {userLocation.Latitude:F4}, {userLocation.Longitude:F4}");
        LocationChanged?.Invoke(this, userLocation);
    }

    private void UpdateNotification(string contentText)
    {
        var notification = CreateNotification(contentText);
        var notificationManager = (NotificationManager)GetSystemService(NotificationService)!;
        notificationManager.Notify(NOTIFICATION_ID, notification);
    }
    public override void OnDestroy()
    {
        IsRunning = false;

        try 
        {
            Geolocation.LocationChanged -= OnLocationChanged;
            Geolocation.StopListeningForeground();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error stopping listener: {ex}");
        }

        if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
        {
            StopForeground(StopForegroundFlags.Remove);
        }
        else
        {
            StopForeground(true);
        }

        base.OnDestroy();
    }
   
}
