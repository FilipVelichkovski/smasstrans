using System.Diagnostics;
using System.Text.Json.Serialization;
using SMassTrans.Shared.ModeShift;

namespace SMassTrans.Shared;

public class Stormwin : IGps
{
    private readonly ModeShiftGps _modeShiftGps;

    public event EventHandler<GpsData>? DataRecieved;

    public Stormwin(HttpClient? client = null)
    {
        _modeShiftGps = new ModeShiftGps(client);
        _modeShiftGps.DataRecieved += (sender, data) =>
        {
            DataRecieved?.Invoke(this, data);
        };
    }

    public async Task SubscribeToLine(string line)
    {
        await _modeShiftGps.SubscribeToLine(line);
    }

    public async Task UnSubscribeToLine(string line)
    {
        await _modeShiftGps.UnSubscribeToLine(line);
    }

    public void Dispose()
    {
        _modeShiftGps.Dispose();
    }
}


public class Position : IGpsInfo
{
    [JsonPropertyName("vehicleId")]
    public int VehicleId { get; set; }

    [JsonPropertyName("time")]
    public int Time { get; set; }

    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("lon")]
    public double Lon { get; set; }

    [JsonPropertyName("ignition")]
    public bool Ignition { get; set; }

    [JsonPropertyName("courseId")]
    public int CourseId { get; set; }

    [JsonPropertyName("stopId")]
    public int StopId { get; set; }

    [JsonPropertyName("date")]
    public int Date { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("delay")]
    public int Delay { get; set; }

    [JsonPropertyName("speed")]
    public int Speed { get; set; }

    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("shift")]
    public int Shift { get; set; }

    [JsonPropertyName("stopIndex")]
    public int StopIndex { get; set; }

    [JsonPropertyName("routeId")]
    public int RouteId { get; set; }

    [JsonPropertyName("vehicle")]
    public Vehicle Vehicle { get; set; }

    [JsonPropertyName("startTime")]
    public int StartTime { get; set; }
    public string Id => Vehicle.Plate;
}

public class GPSRoot
{
    [JsonPropertyName("routeId")]
    public int RouteId { get; set; }

    [JsonPropertyName("positions")]
    public List<Position> Positions { get; set; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
}

public class Vehicle
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("plate")]
    public string Plate { get; set; }
}

