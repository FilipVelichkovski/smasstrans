using System.Text;
using System.Text.Json;
using SMassTrans.Shared;

var _httpClient = new HttpClient();
Skopska skopska = new(_httpClient);
await skopska.InitAsync();

var routes = await skopska.GetAllRouteInfo();
await skopska.GetStops(true);

// Key: Route Name/ID, Value: Full Encoded Polyline for the entire journey
Dictionary<string, List<string>> routeGeometries = new();

async Task<List<string>> GetFullRouteShape(List<string> stopIds)
{
    // 1. Resolve all stops for this route
    var locations = new List<object>();
    foreach (var id in stopIds)
    {
        var stop = await skopska.GetStop(id);
        if (stop != null)
        {
            locations.Add(new { lat = stop.Lat, lon = stop.Lon });
        }
    }

    if (locations.Count < 2) return null;

    // 2. Build the Valhalla multi-stop request
    var requestBody = new
    {
        locations = locations,
        costing = "bus",
        units = "kilometers",
        // 'osrm' or 'polyline6' are common. Valhalla default is polyline6 (6 decimal precision)
    };

    try
    {
        var response = await _httpClient.PostAsync(
            "http://localhost:8002/route", 
            new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        );

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var trip = doc.RootElement.GetProperty("trip");
            var legs = trip.GetProperty("legs");
            List<string> allShapes = new List<string>();

            foreach (var leg in legs.EnumerateArray())
            {
                allShapes.Add(leg.GetProperty("shape").GetString());
            }
            return allShapes;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error fetching full route: {ex.Message}");
    }
    return null;
}
foreach (var route in routes)
{
    var fullShape = await GetFullRouteShape(route.StopIds);
    routeGeometries[route.LineRouteId.LineId+"_"+ route.LineRouteId.RouteId] = fullShape;
}
File.WriteAllText("fullRouteGeometries.json", JsonSerializer.Serialize(routeGeometries));
Console.WriteLine($"Finished! Saved {routeGeometries.Count} complete route shapes.");