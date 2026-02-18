using System;
using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;
using Avalonia.Maui;
using Avalonia.ReactiveUI;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;

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
        LocationGatherer.GetLoc = async () =>
        {
            GeolocationRequest request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5));
            var res = await MainThread.InvokeOnMainThreadAsync(async () =>
                await Geolocation.Default.GetLocationAsync(request));
            if (res == null) return new(0, 0);
            return new(res.Latitude, res.Longitude);
        };
        LocationGatherer.HasGps = true;
    }
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
                .UseMaui<MauiApplication>(this)
            .WithInterFont()
            .UseReactiveUI();
    }
}

public class MauiApplication : Microsoft.Maui.Controls.Application
{
    
}