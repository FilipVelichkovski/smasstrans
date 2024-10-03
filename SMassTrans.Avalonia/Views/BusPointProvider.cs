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
            var activeBuses = y.Infos.Where(x => x.Status == "COURSE_LEAVE" || x.Status == "COURSE_EXECUTE" || x.Status == "WAIT_STOP").ToList();
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
    private List<IGpsInfo> infos = new();
    /* public  MRect? GetExtent()
     {
         var a = SphericalMercator.FromLonLat(21.192293167114258, 42.15064239501953);
         var b = SphericalMercator.FromLonLat(21.781400680541992, 41.800537109375);
         return new MRect(a.x, a.y, b.x, b.y);
     }*/

    public override Task<IEnumerable<IFeature>> GetFeaturesAsync(FetchInfo fetchInfo)
    {
        /*  PointFeature busFeature;
          busFeature = new PointFeature(SphericalMercator.FromLonLat(_prevCoords.Lon, _prevCoords.Lat).ToMPoint());
          */
        return Task.FromResult(infos.Select(x =>
        {
            var p = new PointFeature(SphericalMercator.FromLonLat(x.Lon, x.Lat).ToMPoint());
            p["ID"] = x.Id;
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