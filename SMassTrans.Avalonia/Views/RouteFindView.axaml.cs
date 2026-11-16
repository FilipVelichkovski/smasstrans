

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using SMassTrans.Shared;
using SMassTrans.Avalonia.ViewModels;
using SMassTrans.Avalonia.Lang;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using SMassTrans.Shared.ModeShift;

namespace SMassTrans.Avalonia.Views;

public partial class RouteFindView : UserControl
{
    private double? fromLat, fromLon, toLat, toLon;
    private string? fromStop, toStop;
    private List<Stop>? allStops;
    private MainViewModel? model;
    private List<string> routeResults = new();

    public RouteFindView()
    {
        InitializeComponent();
        Loaded += RouteFindView_Loaded;
    }

    private async void RouteFindView_Loaded(object? sender, EventArgs e)
    {
        model = (this.DataContext as MainViewModel);
        if(string.IsNullOrEmpty(model?.RouteFrom) == false)
        {
              FromTextBox.Text= model.RouteFrom;
        }
        if(string.IsNullOrEmpty(model?.RouteTo) == false)
        {
              ToTextBox.Text= model.RouteTo;
        }
        if (model != null && model.client != null)
        {
            allStops = await model.client.GetStops();
        }
    }

    private async void FromGpsBtn_Click(object? sender, RoutedEventArgs e)
    {
        var (lat, lon) = await LocationGatherer.GetLoc();
        fromLat = lat;
        fromLon = lon;
        FromTextBox.Text = $"{lat:0.000000},{lon:0.000000}";
    }

    private async void ToGpsBtn_Click(object? sender, RoutedEventArgs e)
    {
        var (lat, lon) = await LocationGatherer.GetLoc();
        toLat = lat;
        toLon = lon;
        ToTextBox.Text = $"{lat:0.000000},{lon:0.000000}";
    }

    private void SwapBtn_Click(object? sender, RoutedEventArgs e)
    {
        var tmpText = FromTextBox.Text;
        FromTextBox.Text = ToTextBox.Text;
        ToTextBox.Text = tmpText;
        (fromLat, toLat) = (toLat, fromLat);
        (fromLon, toLon) = (toLon, fromLon);
        (fromStop, toStop) = (toStop, fromStop);
    }

    
    private async void OnFindRouteClick(object? sender, RoutedEventArgs e)
    {
        if (model == null || model.client == null || allStops == null)
            return;

        var (start, end) = ParseInputs();
        if (start is null || end is null)
        {
            ShowResult(Lang.Resources.InvalidInput);
            return;
        }

        var routeFinder = new RouteFinder();
        var lines = (await model.client.GetAllLineInfo()).ToList();
        var paths = await routeFinder.FindPathAsync(start, end, model.client, lines);
        if (paths.Count == 0)
        {
            ShowResult(Lang.Resources.NoRouteFound);
        }
        else
        {
            // Calculate total time for each path
            double WalkingSpeedMPerMin = 50.0; // 3km/h
            double BusSpeedMPerMin = 300.0;
            StringWriter sw = new StringWriter();
            var pathTimes = paths.Select(path =>
            {
                double totalTime = 0;
                if (path.Count > 0)
                {
                    var (firstStop, firstLine) = path[0];
                    var dist = CoordinateHelper.GetDistanceTo(start.lat, start.lon, firstStop.Lat, firstStop.Lon);
                    totalTime += dist / WalkingSpeedMPerMin;
                }
                for (int i = 1; i < path.Count; i++)
                {
                    var (prevStop, prevLine) = path[i - 1];
                    var (currStop, currLine) = path[i];
                    var dist = CoordinateHelper.GetDistanceTo(prevStop.Lat, prevStop.Lon, currStop.Lat, currStop.Lon);
                    if (EqualityComparer<LineRouteId>.Default.Equals(prevLine, currLine) && !EqualityComparer<LineRouteId>.Default.Equals(prevLine, default(LineRouteId)))
                    {
                        // Same bus line
                        totalTime += dist / BusSpeedMPerMin;
                    }
                    else if (EqualityComparer<LineRouteId>.Default.Equals(currLine, default(LineRouteId)))
                    {
                        // Walking
                        totalTime += dist / WalkingSpeedMPerMin;
                    }
                    else
                    {
                        // Transfer wait time + bus time
                        totalTime += 5.0; // wait time
                        totalTime += dist / BusSpeedMPerMin;
                    }
                }
                if (path.Count > 0)
                {
                    var (lastStop, lastLine) = path[path.Count - 1];
                    var dist = CoordinateHelper.GetDistanceTo(end.lat, end.lon, lastStop.Lat, lastStop.Lon);
                    totalTime += dist / WalkingSpeedMPerMin;
                }
                return (path, totalTime);
            }).OrderBy(x => x.totalTime).ToList();

            for (int p = 0; p < pathTimes.Count; p++)
            {
                var (path, totalTime) = pathTimes[p];
                sw.WriteLine($"Route {p + 1} (est. {totalTime:F1} min):");
                for (int i = 0; i < path.Count; i++)
                {
                    var (s, l) = path[i];
                    var lineInfo = lines.FirstOrDefault(x => x.LineRouteIds.Contains(l));
                    string distanceInfo = "";
                    if (i == 0)
                    {
                        // Distance from start point to first stop
                        var dist = CoordinateHelper.GetDistanceTo(start.lat, start.lon, s.Lat, s.Lon);
                        distanceInfo = $"[from start: {dist:F1}m] ";
                    }
                    else if (i == path.Count - 1)
                    {
                        // Distance from last stop to end point
                        var dist = CoordinateHelper.GetDistanceTo(end.lat, end.lon, s.Lat, s.Lon);
                        distanceInfo = $"[to end: {dist:F1}m] ";
                    }
                    // If this hop is a walk, print walked distance from previous stop
                    string walkInfo = "";
                    if (i > 0 && EqualityComparer<LineRouteId>.Default.Equals(l, default(LineRouteId)))
                    {
                        var prev = path[i - 1].Item1;
                        var walkDist = CoordinateHelper.GetDistanceTo(prev.Lat, prev.Lon, s.Lat, s.Lon);
                        walkInfo = $"[walked: {walkDist:F1}m] ";
                    }
                    sw.WriteLine($"  {s.StopName} {distanceInfo}{walkInfo}{(lineInfo != null ? $"via {lineInfo.Name}" : (EqualityComparer<LineRouteId>.Default.Equals(l, default(LineRouteId)) ? "(walk)" : $"via {l.LineId}"))}");
                }
                sw.WriteLine();
            }
            ShowResult(sw.ToString());
    }

    }

    private (RouteFinder.Point? start, RouteFinder.Point? end) ParseInputs()
    {
        RouteFinder.Point? start = null, end = null;
        if (TryParsePoint(FromTextBox.Text, out var s))
            start = s;
        else if (allStops != null && !string.IsNullOrWhiteSpace(FromTextBox.Text))
        {
            var stop = allStops.FirstOrDefault(x => x.StopName.Contains(FromTextBox.Text, StringComparison.OrdinalIgnoreCase));
            if (stop != null)
                start = new RouteFinder.Point(stop.Lon, stop.Lat);
        }
        if (TryParsePoint(ToTextBox.Text, out var e))
            end = e;
        else if (allStops != null && !string.IsNullOrWhiteSpace(ToTextBox.Text))
        {
            var stop = allStops.FirstOrDefault(x => x.StopName.Contains(ToTextBox.Text, StringComparison.OrdinalIgnoreCase));
            if (stop != null)
                end = new RouteFinder.Point(stop.Lon, stop.Lat);
        }
        return (start, end);
    }

    private bool TryParsePoint(string? text, out RouteFinder.Point? pt)
    {
        pt = null;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var parts = text.Split(',');
        if (parts.Length == 2)
        {
            // Always use dot as decimal separator
            var latStr = parts[0].Replace(',', '.').Replace(" ", "");
            var lonStr = parts[1].Replace(',', '.').Replace(" ", "");
            if (double.TryParse(latStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lat)
                && double.TryParse(lonStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lon))
            {
                pt = new RouteFinder.Point(lon, lat);
                return true;
            }
        }
        return false;
    }

    private void ShowResult(string text)
    {
        Dispatcher.UIThread.Post(() =>
        {
            routeResults.Clear();
            routeResults.Add(text);
            RouteResults.DataContext = null;
            RouteResults.ItemsSource = null;
            RouteResults.ItemsSource = routeResults;
        });
    }
}
