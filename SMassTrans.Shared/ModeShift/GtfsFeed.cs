using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;

namespace SMassTrans.Shared.ModeShift;

public class GtfsFeed
{
    public const string PrimaryGtfsUrl = "https://www.modeshift.app/api/v1/9814b106-2afe-47c8-919b-bdec6a5e521e/transport/gtfs/gtfs.zip";
    public const string FallbackGtfsUrl = "https://storage.googleapis.com/storage/v1/b/mdb-latest/o/mk-skopje-jsp-skopje-gtfs-3045.zip?alt=media";

    public List<GtfsStop> Stops { get; set; } = [];
    public List<GtfsRoute> Routes { get; set; } = [];
    public List<GtfsTrip> Trips { get; set; } = [];
    public List<GtfsStopTime> StopTimes { get; set; } = [];

    private readonly HttpClient _client;

    public GtfsFeed(HttpClient? client = null)
    {
        _client = client ?? new HttpClient();
    }

    public static string CachePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "smasstrans",
        "gtfs.zip");

    public async Task LoadFeedAsync(bool forceDownload = false, CancellationToken ct = default)
    {
        byte[]? zipBytes = null;

        if (!forceDownload && File.Exists(CachePath))
        {
            var fileAge = DateTime.UtcNow - File.GetLastWriteTimeUtc(CachePath);
            if (fileAge < TimeSpan.FromDays(7))
            {
                try
                {
                    zipBytes = await File.ReadAllBytesAsync(CachePath, ct);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to read cached GTFS zip: {ex.Message}");
                }
            }
        }

        if (zipBytes == null)
        {
            zipBytes = await DownloadFeedBytesAsync(ct);
            if (zipBytes != null && zipBytes.Length > 0)
            {
                try
                {
                    var dir = Path.GetDirectoryName(CachePath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    await File.WriteAllBytesAsync(CachePath, zipBytes, ct);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to cache GTFS zip: {ex.Message}");
                }
            }
        }

        if (zipBytes == null || zipBytes.Length == 0)
        {
            throw new InvalidOperationException("Could not obtain GTFS zip feed from primary or fallback source.");
        }

        ParseZip(zipBytes);
    }

    private async Task<byte[]?> DownloadFeedBytesAsync(CancellationToken ct)
    {
        // Try primary URL first
        try
        {
            var res = await _client.GetAsync(PrimaryGtfsUrl, ct);
            if (res.IsSuccessStatusCode)
            {
                return await res.Content.ReadAsByteArrayAsync(ct);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Primary GTFS download failed: {ex.Message}");
        }

        // Try fallback URL
        try
        {
            var res = await _client.GetAsync(FallbackGtfsUrl, ct);
            if (res.IsSuccessStatusCode)
            {
                return await res.Content.ReadAsByteArrayAsync(ct);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Fallback GTFS download failed: {ex.Message}");
        }

        return null;
    }

    public void ParseZip(byte[] zipBytes)
    {
        using var ms = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);

        var stopsEntry = archive.GetEntry("stops.txt");
        if (stopsEntry != null)
        {
            using var reader = new StreamReader(stopsEntry.Open());
            Stops = ParseStops(reader);
        }

        var routesEntry = archive.GetEntry("routes.txt");
        if (routesEntry != null)
        {
            using var reader = new StreamReader(routesEntry.Open());
            Routes = ParseRoutes(reader);
        }

        var tripsEntry = archive.GetEntry("trips.txt");
        if (tripsEntry != null)
        {
            using var reader = new StreamReader(tripsEntry.Open());
            Trips = ParseTrips(reader);
        }

        var stopTimesEntry = archive.GetEntry("stop_times.txt");
        if (stopTimesEntry != null)
        {
            using var reader = new StreamReader(stopTimesEntry.Open());
            StopTimes = ParseStopTimes(reader);
        }
    }

    private static List<GtfsStop> ParseStops(StreamReader reader)
    {
        var list = new List<GtfsStop>();
        var header = reader.ReadLine();
        if (header == null) return list;

        var columns = ParseCsvLine(header);
        int idIdx = Array.IndexOf(columns, "stop_id");
        int codeIdx = Array.IndexOf(columns, "stop_code");
        int nameIdx = Array.IndexOf(columns, "stop_name");
        int latIdx = Array.IndexOf(columns, "stop_lat");
        int lonIdx = Array.IndexOf(columns, "stop_lon");

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var vals = ParseCsvLine(line);
            if (vals.Length <= Math.Max(idIdx, Math.Max(latIdx, lonIdx))) continue;

            if (int.TryParse(vals[idIdx], out var stopId))
            {
                double.TryParse(vals[latIdx], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat);
                double.TryParse(vals[lonIdx], NumberStyles.Float, CultureInfo.InvariantCulture, out var lon);

                list.Add(new GtfsStop
                {
                    StopId = stopId,
                    StopCode = codeIdx >= 0 && codeIdx < vals.Length ? vals[codeIdx] : "",
                    StopName = nameIdx >= 0 && nameIdx < vals.Length ? vals[nameIdx] : "",
                    Latitude = lat,
                    Longitude = lon
                });
            }
        }
        return list;
    }

    private static List<GtfsRoute> ParseRoutes(StreamReader reader)
    {
        var list = new List<GtfsRoute>();
        var header = reader.ReadLine();
        if (header == null) return list;

        var columns = ParseCsvLine(header);
        int idIdx = Array.IndexOf(columns, "route_id");
        int shortNameIdx = Array.IndexOf(columns, "route_short_name");
        int longNameIdx = Array.IndexOf(columns, "route_long_name");
        int typeIdx = Array.IndexOf(columns, "route_type");

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var vals = ParseCsvLine(line);
            if (vals.Length <= idIdx) continue;

            if (int.TryParse(vals[idIdx], out var routeId))
            {
                int.TryParse(typeIdx >= 0 && typeIdx < vals.Length ? vals[typeIdx] : "3", out var type);
                list.Add(new GtfsRoute
                {
                    RouteId = routeId,
                    ShortName = shortNameIdx >= 0 && shortNameIdx < vals.Length ? vals[shortNameIdx] : "",
                    LongName = longNameIdx >= 0 && longNameIdx < vals.Length ? vals[longNameIdx] : "",
                    RouteType = type
                });
            }
        }
        return list;
    }

    private static List<GtfsTrip> ParseTrips(StreamReader reader)
    {
        var list = new List<GtfsTrip>();
        var header = reader.ReadLine();
        if (header == null) return list;

        var columns = ParseCsvLine(header);
        int routeIdIdx = Array.IndexOf(columns, "route_id");
        int tripIdIdx = Array.IndexOf(columns, "trip_id");
        int headsignIdx = Array.IndexOf(columns, "trip_headsign");
        int directionIdIdx = Array.IndexOf(columns, "direction_id");

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var vals = ParseCsvLine(line);
            if (vals.Length <= Math.Max(routeIdIdx, tripIdIdx)) continue;

            if (int.TryParse(vals[routeIdIdx], out var routeId))
            {
                int direction = 0;
                if (directionIdIdx >= 0 && directionIdIdx < vals.Length)
                {
                    int.TryParse(vals[directionIdIdx], out direction);
                }

                list.Add(new GtfsTrip
                {
                    RouteId = routeId,
                    TripId = vals[tripIdIdx],
                    Headsign = headsignIdx >= 0 && headsignIdx < vals.Length ? vals[headsignIdx] : "",
                    DirectionId = direction
                });
            }
        }
        return list;
    }

    private static List<GtfsStopTime> ParseStopTimes(StreamReader reader)
    {
        var list = new List<GtfsStopTime>();
        var header = reader.ReadLine();
        if (header == null) return list;

        var columns = ParseCsvLine(header);
        int tripIdIdx = Array.IndexOf(columns, "trip_id");
        int arrivalIdx = Array.IndexOf(columns, "arrival_time");
        int departureIdx = Array.IndexOf(columns, "departure_time");
        int stopIdIdx = Array.IndexOf(columns, "stop_id");
        int seqIdx = Array.IndexOf(columns, "stop_sequence");

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var vals = ParseCsvLine(line);
            if (vals.Length <= Math.Max(tripIdIdx, Math.Max(stopIdIdx, seqIdx))) continue;

            if (int.TryParse(vals[stopIdIdx], out var stopId) && int.TryParse(vals[seqIdx], out var seq))
            {
                list.Add(new GtfsStopTime
                {
                    TripId = vals[tripIdIdx],
                    ArrivalTime = arrivalIdx >= 0 && arrivalIdx < vals.Length ? vals[arrivalIdx] : "",
                    DepartureTime = departureIdx >= 0 && departureIdx < vals.Length ? vals[departureIdx] : "",
                    StopId = stopId,
                    StopSequence = seq
                });
            }
        }
        return list;
    }

    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var cur = new System.Text.StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    cur.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(cur.ToString().Trim());
                cur.Clear();
            }
            else
            {
                cur.Append(c);
            }
        }
        result.Add(cur.ToString().Trim());
        return result.ToArray();
    }

    public List<Stop> ToStops()
    {
        return Stops.Select(s => new Stop
        {
            id = s.StopId,
            name = s.StopName,
            code = s.StopCode,
            latitude = s.Latitude,
            longitude = s.Longitude,
            patterns = []
        }).ToList();
    }
}

public class GtfsStop
{
    public int StopId { get; set; }
    public string StopCode { get; set; } = "";
    public string StopName { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class GtfsRoute
{
    public int RouteId { get; set; }
    public string ShortName { get; set; } = "";
    public string LongName { get; set; } = "";
    public int RouteType { get; set; }
}

public class GtfsTrip
{
    public int RouteId { get; set; }
    public string TripId { get; set; } = "";
    public string Headsign { get; set; } = "";
    public int DirectionId { get; set; }
}

public class GtfsStopTime
{
    public string TripId { get; set; } = "";
    public string ArrivalTime { get; set; } = "";
    public string DepartureTime { get; set; } = "";
    public int StopId { get; set; }
    public int StopSequence { get; set; }
}
