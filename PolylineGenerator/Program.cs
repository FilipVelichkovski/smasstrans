using System.Text;
using System.Text.Json;
using SMassTrans.Shared.ModeShift;

var _httpClient = new HttpClient();
string valhallaUrl = args.FirstOrDefault()
    ?? Environment.GetEnvironmentVariable("VALHALLA_URL")
    ?? "http://localhost:8002/route";

ModeShiftClient client = new(_httpClient);
await client.InitAsync();

var stops = await client.GetStops(true);
var stopById = stops.ToDictionary(s => s.StopId.ToString(), s => s);
var routes = (await client.GetAllRouteInfo()).ToList();

Console.WriteLine($"Loaded {stops.Count} stops and {routes.Count} routes from ModeShift. Using Valhalla at {valhallaUrl}");

// Key: LineId_RouteId, Value: Full Encoded Polyline segments for the entire journey
Dictionary<string, List<string>> routeGeometries = new();

async Task<List<string>?> GetFullRouteShape(List<string> stopIds, string name)
{
    var locations = new List<object>();
    foreach (var id in stopIds)
    {
        if (stopById.TryGetValue(id, out var stop))
        {
            locations.Add(new { lat = stop.Lat, lon = stop.Lon });
        }
    }

    if (locations.Count < 2) return null;

    const int maxBatch = 45;
    var allShapes = new List<string>();

    for (int start = 0; start < locations.Count - 1; start += maxBatch - 1)
    {
        int count = Math.Min(maxBatch, locations.Count - start);
        var batch = locations.GetRange(start, count);

        var requestBody = new
        {
            locations = batch,
            costing = "bus",
            units = "kilometers",
        };

        try
        {
            var response = await _httpClient.PostAsync(
                valhallaUrl,
                new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            );

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var trip = doc.RootElement.GetProperty("trip");
                var legs = trip.GetProperty("legs");

                foreach (var leg in legs.EnumerateArray())
                {
                    var shape = leg.GetProperty("shape").GetString();
                    if (!string.IsNullOrEmpty(shape))
                    {
                        allShapes.Add(shape);
                    }
                }
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Route {name} Valhalla failed ({response.StatusCode}): {error}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching full route {name}: {ex.Message}");
            return null;
        }
    }

    return allShapes;
}

int count = 0;
foreach (var route in routes)
{
    var key = $"{route.LineRouteId.LineId}_{route.LineRouteId.RouteId}";
    var fullShape = await GetFullRouteShape(route.StopIds, $"{key} ({route.Name})");
    if (fullShape != null && fullShape.Count > 0)
    {
        routeGeometries[key] = fullShape;
    }
    count++;
    if (count % 20 == 0 || count == routes.Count)
    {
        Console.WriteLine($"Processed {count}/{routes.Count} routes ({routeGeometries.Count} succeeded)");
    }
}

File.WriteAllText("fullRouteGeometries.json", JsonSerializer.Serialize(routeGeometries));
Console.WriteLine($"Finished! Saved {routeGeometries.Count} complete route shapes.");