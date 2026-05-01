using BruTile;
using BruTile.Cache;
using BruTile.Predefined;
using BruTile.Web;
using Mapsui.Tiling.Layers;
using System.IO;
using System;
namespace SMassTrans.Avalonia;

public static class OpenStreetMap
{
    private static FileCache? _cache;

    private static readonly Attribution OpenStreetMapAttribution = new("© OpenStreetMap contributors", "https://www.openstreetmap.org/copyright");

    public static TileLayer CreateTileLayer()
    {
        return new TileLayer(CreateTileSource("SMassTrans (https://gitlab.com/silvercraft, silverdiamond@duck.com)"))
        {
            Name = "OpenStreetMap"
        };
    }

    private static HttpTileSource CreateTileSource(string userAgent)
    {
        if (_cache == null)
        {  
            var loc=Path.Combine(Path.GetTempPath(),"smasstransmap");
            Directory.CreateDirectory(loc);
            _cache = new(loc, "png", TimeSpan.FromDays(31));
        }
        GlobalSphericalMercator tileSchema = new();
        Attribution openStreetMapAttribution = OpenStreetMapAttribution;
        return new HttpTileSource(tileSchema, App.Config.OSMTileServer, null, null, "OpenStreetMap", _cache, openStreetMapAttribution,
            x =>
            {
                x.Headers.UserAgent.ParseAdd(userAgent);
            });
    }
}