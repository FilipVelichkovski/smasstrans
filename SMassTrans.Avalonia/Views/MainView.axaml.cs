using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers.AnimatedLayers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Widgets;
using Mapsui.Widgets.BoxWidgets;
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
    Mapsui.UI.Avalonia.MapControl mapControl = new Mapsui.UI.Avalonia.MapControl();
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
        mapControl.Map?.Layers.Add(OpenStreetMap.CreateTileLayer());
        //21.192293167114258,42.1134033203125,21.781400680541992,41.800537109375
        //mapControl.Map?.Widgets.Add(new MapInfoWidget(mapControl.Map));
        mapdata = new();
        mapdata.TextWrapping = TextWrapping.Wrap;
        MapTabItem.Content = mapTabGrid;
        mapTabGrid.Children.Add(mapControl);
        mapTabGrid.Children.Add(mapdata);
        Grid.SetRow(mapdata, 1);

        mapControl.Map.Navigator.CenterOnAndZoomTo(SphericalMercator.FromLonLat(21.43919, 42.00392).ToMPoint(), mapControl.Map.Navigator.Resolutions[18]);
        mapControl.Map.Info += OnMapInfo;
        // mapControl.UseContinuousMouseWheelZoom = true;
        MainViewModel.SetGpsLine += SetGpsLine;
        Refresh();
    }

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
            result.Append($"{field}: {f[field]} - ");
        }
        result.Remove(result.Length - 2, 2);
        return result.ToString();
    };

    private async void SetGpsLine(object? sender, LineRouteId e)
    {
        GpsLine = e.RouteId;
        var info = await client.GetRouteInfo(e);
       model.MapName=$"Map ({info.Name})";
    }

    private void MapControl_Navigator_ViewportChanged(object? sender, PropertyChangedEventArgs e)
    {
        Debug.WriteLine($"{mapControl.Map.Navigator.Viewport.CenterX}  {mapControl.Map.Navigator.Viewport.CenterY} {mapControl.Map.Navigator.Viewport.Resolution}");
    }

    bool FirstShowOfMap = true;
    private void LineDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control { DataContext: IRouteInfo context })
        {
            GpsLine = context.LineRouteId.RouteId;
            model.MapName=$"Map ({context.Name})";
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
                    model.Lines = new(lines);
                });
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }
            /*
                        double right = double.MinValue; //long
                        double left = double.MaxValue;//long
                        double up = double.MinValue; //lat
                        double down = double.MaxValue;//lat
                        foreach (var stop in  model.Stops )
                        {
                            if (stop.Lon > right) right = stop.Lon;
                            if (stop.Lon < left) left = stop.Lon;
                            if (stop.Lat > up) up = stop.Lat;
                            if (stop.Lat < down) down = stop.Lat;
                        }
                        Debug.WriteLine($"U{up} L{left} D{down} R{right}"); //code to get extent*/
        });
    }
    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        Refresh();
    }

    private void Bitpazar_OnClick(object? sender, RoutedEventArgs e)
    {
        mapControl.Map.Navigator.CenterOnAndZoomTo(SphericalMercator.FromLonLat(21.43919, 42.00392).ToMPoint(), mapControl.Map.Navigator.Resolutions[18]);
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
            if (FirstShowOfMap)
            {
                FirstShowOfMap = false;
                mapControl.Map.Navigator.CenterOnAndZoomTo(SphericalMercator.FromLonLat(21.43919, 42.00392).ToMPoint(), mapControl.Map.Navigator.Resolutions[18]);
            }
        }
        else
        {
            //turn off client
            if (layer != null)
            {
                mapControl.Map?.Layers.Remove(layer);
                layer.Dispose();
                layer = null;
                BusPointProvider.Dispose();
            }
            if (TabCtl.SelectedContent is StopSelection s)
            {
                Debug.WriteLine("StopSelection");
            }
            else if (TabCtl.SelectedContent is StopSelectionGPS gps)
            {
                Debug.WriteLine("StopSelectionGPS");
            }
            else if (Equals(TabCtl.SelectedContent, LineBox))
            {
                Debug.WriteLine("Linebox");
            }

        }
        e.Handled = true;
    }
}