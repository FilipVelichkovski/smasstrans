using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SMassTrans.Avalonia.Views;

namespace SMassTrans.Avalonia;
public class SMassTransConfig
{
    public string OSMTileServer { get; set; }
    public string? RouteGeometry { get; set; }
}


public  class App : Application
{
    public override void Initialize()
    {
        Task.Run(InitializeAsync).GetAwaiter().GetResult();
        AvaloniaXamlLoader.Load(this);
    }

    public static SMassTransConfig Config;
    public static readonly HttpClient HttpClient = new();
    private async Task InitializeAsync()
    {
        const string configUrl = "https://thesilvercraft.github.io/smasstrans.json";
        HttpClient.DefaultRequestHeaders.Add("User-Agent", "SMassTrans (+https://gitlab.com/silvercraft, silverdiamond@duck.com)");
        try
        {
            var jsonString = await HttpClient.GetStringAsync(configUrl);
            Config = System.Text.Json.JsonSerializer.Deserialize<SMassTransConfig>(jsonString) ?? new(){OSMTileServer = "https://tile.openstreetmap.org/{z}/{x}/{y}.png", RouteGeometry = null};
        }
        catch (HttpRequestException e)
        {
            System.Diagnostics.Debug.WriteLine($"Error downloading config: {e.Message}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"General error initializing app: {ex.Message}");
        }
    }
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView();
        }

        base.OnFrameworkInitializationCompleted();
    }
}