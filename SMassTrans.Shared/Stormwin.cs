using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
namespace SMassTrans.Shared;

public class Stormwin : IGps
{
    public event EventHandler<GpsData> DataRecieved;
    CancellationTokenSource s = new();
    Task com;
    ClientWebSocket ws = new();

    async Task Com(CancellationToken cancellationToken)
    {
        Uri uri = new("wss://jspotg.stormwin.mk");
         ws.Options.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => 
    {
        return true; 
    };
        await ws.ConnectAsync(uri, cancellationToken);
        while (!cancellationToken.IsCancellationRequested)
        {
            var bytes = new byte[1024 * 256];
            var result = await ws.ReceiveAsync(bytes, default);
            string res = Encoding.UTF8.GetString(bytes, 0, result.Count);
            Debug.WriteLine(res);
            if (res == "ping")
            {
                await ws.SendAsync("pong"u8.ToArray(), WebSocketMessageType.Text, true, cancellationToken);
                continue;
            }
            try
            {
                var gps = JsonSerializer.Deserialize<GPSRoot>(res);
                var igps = new GpsData
                {
                    Id = gps.RouteId,
                    Infos = []
                };
                igps.Infos.AddRange(gps.Positions);
                DataRecieved?.Invoke(this, igps);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                throw;
            }
        }
        Debug.WriteLine("normal cancellation");
        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, default);
    }
    public async Task SubscribeToLine(string Line)
    {
        async Task sub()
        {
            var payload = new Dictionary<string, string>
                    {
                        { "action", "subscribe" },
                        { "routeId", Line }
                    };
            await ws.SendAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)), WebSocketMessageType.Text, true, s.Token);
        }
        if (ws.State is not (WebSocketState.Connecting or WebSocketState.Open))
        {
            com = Task.Run(async () => { s = new(); await Com(s.Token); });
            while (ws.State != WebSocketState.Open)
            {
                await Task.Delay(50);
            }
        }

        await sub();
    }

    public Task UnSubscribeToLine(string Line)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        ws.Dispose();
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

