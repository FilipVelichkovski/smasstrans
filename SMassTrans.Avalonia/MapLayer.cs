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
    public static FileCache? cache = null;

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
        if (cache == null)
        {  
            var loc=Path.Combine(Path.GetTempPath(),"smasstransmap");
            Directory.CreateDirectory(loc);
            cache = new(loc, "png", TimeSpan.FromDays(31));
        }
        GlobalSphericalMercator tileSchema = new();
        Attribution openStreetMapAttribution = OpenStreetMapAttribution;
        return new HttpTileSource(tileSchema, "https://tile.openstreetmap.org/{z}/{x}/{y}.png", null, null, "OpenStreetMap", cache, openStreetMapAttribution,
            x =>
            {
                x.Headers.UserAgent.ParseAdd(userAgent);
            });
    }
}