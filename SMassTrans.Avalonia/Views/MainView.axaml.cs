using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers.AnimatedLayers;
using Mapsui.Projections;
using Mapsui.Styles;
using SMassTrans.Avalonia.ViewModels;
using SMassTrans.Shared;
using Brush = Mapsui.Styles.Brush;
using Color = Mapsui.Styles.Color;

namespace SMassTrans.Avalonia.Views;

public partial class MainView : UserControl
{
    private MainViewModel model = new();
    public static Skopska client;
    bool Inited = false;
    Mapsui.UI.Avalonia.MapControl mapControl;
    private BusPointProvider BusPointProvider;
    private TextBlock mapdata;
    private Grid mapTabGrid;
    public MainView()
    {
        DataContext = model;
        InitializeComponent();
        TabCtl = this.FindControl<TabControl>("TabCtl");
        mapTabGrid = new Grid();
        mapTabGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(9, GridUnitType.Star) });
        mapTabGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
        client = new Skopska(new());
        model.client = client;
        mapdata = new();
        mapdata.TextWrapping = TextWrapping.Wrap;
        MapTabItem.Content = mapTabGrid;
        mapTabGrid.Children.Add(mapdata);
        Grid.SetRow(mapdata, 1);

      
        MainViewModel.SetGpsLine += SetGpsLine;
        Refresh();
        if (Application.Current.TryGetFeature<IActivatableLifetime>() is { } activatableLifetime)
        {
            activatableLifetime.Activated += (sender, args) =>
            {
                if (args.Kind == ActivationKind.Background)
                {
                    Debug.WriteLine($"App exited background");
                    if (Equals(TabCtl.SelectedContent, mapTabGrid))
                    {
                        StartMap();
                    }
                }
            };
            activatableLifetime.Deactivated += (sender, args) =>
            {
                if (args.Kind == ActivationKind.Background)
                {
                    Debug.WriteLine($"App entered background");
                    StopMap();
                }
            };
        }
    }
    

    private MPoint Bitpazar = SphericalMercator.FromLonLat(21.439978, 42.000939).ToMPoint();

    private void OnMapInfo(object? sender, MapInfoEventArgs e)
    {
       mapdata.Text= FeatureToText(e.MapInfo?.Feature);
    }
    public Func<IFeature?, string> FeatureToText { get; set; } = (f) =>
    {
        if (f is null) return string.Empty;
        var result = new StringBuilder();
        foreach (var field in f.Fields)
        {
            result.Append(field);
            result.Append(':');
            result.Append(' ');

            if (field == "StopId" && f[field] is int x)
            {
                result.Append($" {x}.{client.GetStop(x.ToString("0000")).GetAwaiter().GetResult().StopName} - ");
            }
            else
            {
                result.Append($" {f[field]} - ");
            }
        }
        result.Remove(result.Length - 2, 2);
        return result.ToString();
    };

    private async void SetGpsLine(object? sender, LineRouteId e)
    {
        var info = await client.GetRouteInfo(e);
        SetGpsLine(info);
    }

    private void SetGpsLine(IRouteInfo info)
    {
        GpsLine = info.LineRouteId.RouteId;
        model.MapName=$"Map ({info.Name.Trim()})";
    }
    private void LineGPS(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: IRouteInfo context })
        {
            SetGpsLine(context);
        }
    }
    private void MapControl_Navigator_ViewportChanged(object? sender, PropertyChangedEventArgs e)
    {
        Debug.WriteLine($"{mapControl.Map.Navigator.Viewport.CenterX}  {mapControl.Map.Navigator.Viewport.CenterY} {mapControl.Map.Navigator.Viewport.Resolution}");
    }

    private void LineDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control { DataContext: IRouteInfo context } a)
        {
            a.ContextMenu.Open();
           
        }
    }


    void Refresh()
    {
        var task = Task.Run(async () =>
        {
            try
            {
                if (!Inited)
                {
                    await client.InitAsync();
                    Inited = true;
                }
                var stops = await client.GetStops();
                var lines = await client.GetAllRouteInfo();
               
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    model.Stops = new AvaloniaList<IStop>(stops);
                    model.Lines = new( lines.OrderBy(x=>x.LineRouteId.LineId));
                });
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }
        });
    }

    private void Bitpazar_OnClick(object? sender, RoutedEventArgs e)
    {
        mapControl.Map.Navigator.CenterOnAndZoomTo(Bitpazar, mapControl.Map.Navigator.Resolutions[18]);
    }

    private AnimatedPointLayer layer;
    public static string GpsLine = "1126";
    private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.Source is not TabControl) return;
        if (TabCtl is null)
        {
            Debug.WriteLine("tabctl null");
            return;
        }

        if (Equals(TabCtl.SelectedContent, mapTabGrid))
        {
            StartMap();
        }
        else
        {
            StopMap();
        }
        e.Handled = true;
    }

    void StartMap()
    {
        mapControl = new();
        mapTabGrid.Children.Add(mapControl);
        mapControl.Map?.Layers.Add(OpenStreetMap.CreateTileLayer());
        mapControl.Map?.Navigator.CenterOnAndZoomTo(Bitpazar, mapControl.Map.Navigator.Resolutions[18]);
        mapControl.Map.Info += OnMapInfo;
        BusPointProvider = new BusPointProvider(GpsLine);
        layer = new AnimatedPointLayer(BusPointProvider)
        {
            Name = "Buses",
            Style = new LabelStyle
            {
                BackColor = new Brush(Color.Red),
                ForeColor = Color.White,
                Text = "Bus",
            },
            IsMapInfoLayer = true
        };
        mapControl.Map?.Layers.Add(layer); 
    }
    void StopMap()
    {
        if (layer != null)
        {
            mapControl.Map?.Layers.Remove(layer);
        }
        layer?.Dispose();
        layer = null;
        BusPointProvider?.Dispose();
        BusPointProvider = null;
        mapTabGrid.Children.Remove(mapControl);
        mapControl.Dispose();
        mapControl = null;
    }

   

    private void CopyStops(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control { DataContext: IRouteInfo context }) return;
        var topLevel = TopLevel.GetTopLevel(this);
        StringBuilder s = new();
        s.AppendLine($"{context.Name}: ");
        if (context is RouteInfo routeInfo)
        {
            foreach (var stop in routeInfo.StopIds)
            {
                s.AppendLine($"{model.client.GetStop(stop).GetAwaiter().GetResult().StopName}");
            }
        }
        topLevel.Clipboard.SetTextAsync(s.ToString());
    }


    private async void GPSButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var (lat,lon)= await LocationGatherer.GetLoc();
        mapControl.Map.Navigator.CenterOnAndZoomTo(SphericalMercator.FromLonLat(lon,lat).ToMPoint(), mapControl.Map.Navigator.Resolutions[18]);
    }
}