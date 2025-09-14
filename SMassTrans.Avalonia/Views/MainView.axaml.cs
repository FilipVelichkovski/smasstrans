using System;
using System.Collections.Generic;
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
using ReactiveUI;
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
    private LineRouteId? selectedBusLine;
    public MainView()
    {
        DataContext = model;
        InitializeComponent();
        TabCtl = this.FindControl<TabControl>("TabCtl");
        mapTabGrid = this.FindControl<Grid>("mapTabGrid");
        client = new Skopska(new());
        model.client = client;
        mapdata = this.FindControl<TextBlock>("mapdata");
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
 
    private void MapControl_Navigator_ViewportChanged(object? sender, PropertyChangedEventArgs e)
    {
        Debug.WriteLine($"{mapControl.Map.Navigator.Viewport.CenterX}  {mapControl.Map.Navigator.Viewport.CenterY} {mapControl.Map.Navigator.Viewport.Resolution}");
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
                    model.Stops = [.. stops];
                    model.Lines = [.. lines.OrderBy(x=>x.LineRouteId.LineId)];
                });
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }
        });
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
        if (MapEn) return;

        mapControl = new();
        mapControl.Initialized += (sender, args) =>
        {
            mapControl.Map?.Layers.Add(OpenStreetMap.CreateTileLayer());
            try
            {
                if (CenterX is { } x && CenterY is { } y)
                {
                    mapControl.Map?.Navigator.CenterOnAndZoomTo(new MPoint(x, y), Resolution??18);
                }
                else
                {
                    mapControl.Map?.Navigator.CenterOnAndZoomTo(Bitpazar, mapControl.Map.Navigator.Resolutions[18]);
                }
            }
            catch (Exception e)
            {
            }
        };
        mapTabGrid.Children.Add(mapControl);
        
        mapControl.Map.Info += OnMapInfo;
        BusPointProvider = new BusPointProvider(GpsLine);
        BusPointProvider.DataChanged += UpdateFeatureText;
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
        MapEn = true;  
    }

    private string? SelectedBusFeatureID = null;
    private void OnMapInfo(object? sender, MapInfoEventArgs e)
    {
        if (e.MapInfo is not null)
        {
            if (e.MapInfo.Feature ==null)
            {
                SelectedBusFeatureID = null;
            }
            else
            {
                SelectedBusFeatureID = (string)e.MapInfo.Feature["ID"];
            }
        }
        mapdata.Text = FeatureToText(e.MapInfo?.Feature);
    }
    private void UpdateFeatureText(object? sender, EventArgs e)
    {
        if (SelectedBusFeatureID != null)
        {
            var x = BusPointProvider.GetInfo(SelectedBusFeatureID);
            var result = new StringBuilder();
            foreach (var (field,data) in x)
            {
                result.Append(field);
                result.Append(':');
                result.Append(' ');

                if (field == "StopId" &&  data is int xy)
                {
                    result.Append($" {xy}.{client.GetStop(xy.ToString("0000")).GetAwaiter().GetResult().StopName} - ");
                }
                else
                {
                    result.Append($" {data} - ");
                }
            }
            result.Remove(result.Length - 2, 2);
            var resultx = result.ToString();
            Dispatcher.UIThread.Invoke(() =>
            {
                mapdata.Text = resultx;
            });
        }
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
    
 
  

    void StopMap()
    {
        if (!MapEn) return;
        if (layer != null && mapControl?.Map?.Layers.Contains(layer) == true)
        {
            mapControl.Map.Layers.Remove(layer);
        }
        layer?.Dispose();
        layer = null;
        mapControl.Map.Info -= OnMapInfo;
        BusPointProvider.DataChanged -= UpdateFeatureText;
        BusPointProvider?.Dispose();
        BusPointProvider = null;
        
        if (mapControl != null)
        {
            mapTabGrid.Children.Remove(mapControl);
            CenterX = mapControl.Map?.Navigator.Viewport.CenterX;
            CenterY = mapControl.Map?.Navigator.Viewport.CenterY;
            Resolution = mapControl.Map?.Navigator.Viewport.Resolution;
        }
        mapControl?.Dispose();
        mapControl = null;
        MapEn = false;  
    }

    private double? CenterX=null, CenterY=null,Resolution=null;
    private bool MapEn = false;



    private async void GPSButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var (lat,lon)= await LocationGatherer.GetLoc();
        mapControl.Map.Navigator.CenterOnAndZoomTo(SphericalMercator.FromLonLat(lon,lat).ToMPoint(), mapControl.Map.Navigator.Resolutions[18]);
    }
}