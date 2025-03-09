using System.Net.Http.Json;

namespace SMassTrans.Shared.ModeShift;

public class ModeShiftClient
{
    HttpClient _client;

    private string BaseUrl = "https://www.modeshift.app/api/v1/9814b106-2afe-47c8-919b-bdec6a5e521e";
    private string SettingsUrl = "https://transport.skopjebus.mk/wp-content/plugins/planner/data/settings.json";
    public async Task<Settings?> GetSettings()
    {
        var a = await _client.GetAsync(SettingsUrl);
        return await a.Content.ReadFromJsonAsync<Settings>();
    }
    public async Task<List<Route>?> GetRoutes()
    {
        var a = await _client.GetAsync(BaseUrl + "/transport/planner/routes");
        return await a.Content.ReadFromJsonAsync<List<Route>>();
    }
    public async Task<List<Stop>?> GetStops()
    {
        var a = await _client.GetAsync(BaseUrl + "/transport/planner/stops");
        return await a.Content.ReadFromJsonAsync<List<Stop>>();
    }
    public async Task<List<NearbyTimes>?> GetNearbyTimes(double latitude, double longitude, double radius)
    {
        var a = await _client.GetAsync(BaseUrl + $"/transport/planner/stops/nearbyTimes?latitude={latitude}&longitude={longitude}&radius={radius}");
        return await a.Content.ReadFromJsonAsync<List<NearbyTimes>>();
    }
    public async Task<List<Time>?> GetTimes(int stopNum, int? numberofDepartures=null, int? timerange=null)
    {
        var a = await _client.GetAsync(BaseUrl + $"/transport/planner/stops/{stopNum}/times"); //numberOfDepartures=1&timeRange=undefined
        return await a.Content.ReadFromJsonAsync<List<Time>>();
    }
    //every 5 seconds
    /*public async Task<> GetVehicles(int routeId)
    {
        var a = await _client.GetAsync(BaseUrl + $"/transport/public/vehicles/live?routeId={routeId}"); //&patternHash=00f5221cb82051578726d26f0af934aa5efdb35c51ab87ded3b0137384c6fb19
        return await a.Content.ReadFromJsonAsync<>();
    }*/
}


