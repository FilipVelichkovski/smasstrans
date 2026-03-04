using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Fetcher;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Providers;
using Mapsui.Styles;
using SMassTrans.Shared;

namespace SMassTrans.Avalonia.Views;

public class BusPointProvider : MemoryProvider, IDynamic, IDisposable
{
    //https://mapsui.com/v5/codesamples/SingleCalloutSample.html
    private Stormwin s;
    public BusPointProvider(string line = "1126")
    {
        s = new();

        s.DataRecieved += (x, y) =>
        {
            #if DEBUG
            var activeBuses = y.Infos.Where(x => x.Status is "COURSE_LEAVE" or "COURSE_EXECUTE" or "WAIT_STOP").ToList();
            Debug.WriteLine(activeBuses.Count > 0
                ? $"{y.Id} {activeBuses.Count}/{y.Infos.Count} buses going at avg {activeBuses.Select(x => x.Speed).Average()}km/h"
                : $"{y.Id} {activeBuses.Count}/{y.Infos.Count}");
            foreach (var bus in activeBuses)
            {
                Debug.WriteLine($"{bus.Id} {bus.Status} {bus.Lat} {bus.Lon} {bus.Speed}km/h");
            }
            #endif
            infos = y.Infos;
            DataHasChanged();
        };
        Task.Run(async () =>
        {
            await s.SubscribeToLine(line);
        });
    }
    
    public async Task Subscribe(string line)
    {
        _ = Task.Run(async () =>
        {
            await s.SubscribeToLine(line);
        });
    }

    public Dictionary<string, object>? GetInfo(string Id)
    {
        var x = infos.FirstOrDefault(x => x.Id == Id);
        if (x == null)
        {
            return null;
        }
        var p = new Dictionary<string, object>();
        p["ID"] = x.Id;
        if (x is Position pz)
        {
            p["#"] = pz.Vehicle.Name;
        }
        p["Status"] = x.Status;
        p["Speed"] = x.Speed + "km/h";
        p["Delay"] = x.Delay;
        p["Ignition"] = x.Ignition;
        p["StopIndex"] = x.StopIndex;
        p["StopId"] = x.StopId;
        p["RouteId"] = x.RouteId;
        p["CourseId"] = x.CourseId;
        
        return p;
    }
    private List<IGpsInfo> infos = new();

   
    public override Task<IEnumerable<IFeature>> GetFeaturesAsync(FetchInfo fetchInfo)
    {
        return Task.FromResult(infos.Select(x =>
        {
            var p = new PointFeature(SphericalMercator.FromLonLat(x.Lon, x.Lat).ToMPoint());
            p["ID"] = x.Id;
            if (x is Position pz)
            {
                p["#"] = pz.Vehicle.Name;
            }
            p["Status"] = x.Status;
            p["Speed"] = x.Speed + "km/h";
            p["Delay"] = x.Delay;
            p["Ignition"] = x.Ignition;
            p["StopIndex"] = x.StopIndex;
            p["StopId"] = x.StopId;
            p["RouteId"] = x.RouteId;
            p["CourseId"] = x.CourseId;
            return p;
        }).Cast<IFeature>());
    }
    public void DataHasChanged()
    {
        DataChanged?.Invoke(this, new DataChangedEventArgs(""));
    }

    public event EventHandler? DataChanged;

    public void Dispose()
    {
        s.Dispose();
        infos = null;
    }
}