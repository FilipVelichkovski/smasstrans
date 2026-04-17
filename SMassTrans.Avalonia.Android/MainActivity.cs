using System;
using System.Diagnostics;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using Avalonia;
using Avalonia.Android;
using Avalonia.Maui;
using Avalonia.ReactiveUI;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using SMassTrans.Shared;
using SystemDebug = System.Diagnostics.Debug;

namespace SMassTrans.Avalonia.Android;

[Activity(
    Label = "SMassTrans.Avalonia.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@mipmap/ic_launcher",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{

    public MainActivity()
    {
        SystemDebug.WriteLine("=== MainActivity constructor START ===");
        HttpClientHandler.UseNativePlatformHandler = false;
        LocationGatherer.GetLoc = async () =>
        {
            GeolocationRequest request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(2));
            var res = await MainThread.InvokeOnMainThreadAsync(async () =>
                await Geolocation.Default.GetLocationAsync(request));
            if (res == null) return new(0, 0);
            return new(res.Latitude, res.Longitude);
        };
        
        LocationGatherer.GetLocationTracker = () => 
        {
            SystemDebug.WriteLine("GetLocationTracker lambda called");
            var tracker = new AndroidUserLocationTracker();
            SystemDebug.WriteLine($"Created tracker: {(tracker == null ? "NULL" : "OK")}");
            return tracker;
        };
        LocationGatherer.ShowNotification = notificationT =>
        {
            var notificationManager = NotificationManagerCompat.From(Platform.AppContext);

            var channelId = "route_tracking";
            var channelName = "Route Tracking";
            var channelDescription = "Notifications for route tracking";

                var channel = new NotificationChannel(channelId, channelName, NotificationImportance.High)
                {
                    Description = channelDescription
                };
                notificationManager.CreateNotificationChannel(channel);

            var notification = new NotificationCompat.Builder(Platform.AppContext, channelId)
                .SetContentTitle("SMassTrans Route Tracking")
                .SetContentText(notificationT)
                .SetSmallIcon(Resource.Drawable.ic_launcher_foreground)
                .SetAutoCancel(true)
                .Build();

            notificationManager.Notify(1001, notification);
        };
        LocationGatherer.HasGps = true;
        SystemDebug.WriteLine("=== MainActivity constructor: Delegates assigned ===");
    }

    protected override void OnCreate(Bundle savedInstanceState)
    {
        SystemDebug.WriteLine("=== MainActivity OnCreate called ===");
        base.OnCreate(savedInstanceState);
        SystemDebug.WriteLine("=== MainActivity OnCreate END ===");
    }

 
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        SystemDebug.WriteLine("=== CustomizeAppBuilder called ===");
        // Delegates already set in OnCreate, just return builder
        return base.CustomizeAppBuilder(builder)
                .UseMaui<MauiApplication>(this)
            .WithInterFont()
            .UseReactiveUI();
    }

    /// <summary>
    /// Handle permission results from the system
    /// </summary>
    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        // MAUI Geolocation API handles permissions internally
    }
}
[Service(ForegroundServiceType = ForegroundService.TypeLocation)]
public class LocationService : Service
{
    public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
    {
        var channelId = "route_tracking";
        // Create the notification that keeps the service alive
        var notification = new NotificationCompat.Builder(this, channelId)
            .SetContentTitle("SMassTrans")
            .SetContentText("Route tracking active...")
            .SetSmallIcon(Resource.Drawable.ic_launcher_foreground)
            .SetOngoing(true)
            .Build();

        StartForeground(1001, notification);

        return StartCommandResult.Sticky;
    }

    public override IBinder OnBind(Intent intent) => null;
}
public class MauiApplication : Microsoft.Maui.Controls.Application
{
    
}