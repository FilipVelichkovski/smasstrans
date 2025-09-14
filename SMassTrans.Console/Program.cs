using System.Globalization;
using SMassTrans.Shared;



HttpClient hclient = new();
var client = new Skopska(hclient);
await client.InitAsync();
var app = new BusStopCLIApp(client);

await app.InitializeAsync();
var stopNameQuery = await app.AutocompleteSearchAsync();
await app.SearchAndDisplayNextBusesAsync(stopNameQuery);
   

/*
var stops = await client.GetStops();
while (true)
{
    foreach (var stop in stops.Where(x => x.StopName.Contains("ЦЕНТАР РЕКОРД-")))
    {
        var a = await stop.GetLinesAndTimes();
        Console.WriteLine($"{stop.StopName} ");
        foreach (var line in a.Where(x => x.Value.Count != 0))
        {
            var nameline = await client.GetRouteInfo(line.Key);
            var lineline = await client.GetLineInfo(line.Key);
            Console.WriteLine($" {lineline.Name} {nameline.Name} {string.Join(',', line.Value.Select(x => x / 60d))}m");
        }
    }
    await Task.Delay(50000);
}*/



/*await client.GetRoutes();
await client.GetLines();
await client.GetStopLines();


//await client.GetStopTimes(186,1125);

// routeid 1125
/*
Stormwin s=new();

s.DataRecieved+=(x,y)=>{
var activeBuses=y.Infos.Where(x=>x.Status=="COURSE_LEAVE" || x.Status=="COURSE_EXECUTE" || x.Status=="WAIT_STOP").ToList();
Console.WriteLine($"{y.Id} {activeBuses.Count}/{y.Infos.Count} buses going at avg {activeBuses.Select(x=>x.Speed).Average()}km/h");
foreach(var bus in activeBuses)
{
    Console.WriteLine($"{bus.Id} {bus.Status} {bus.Lat} {bus.Lon} {bus.Speed}km/h");
}
};

await s.SubscribeToLine("1125");
Console.ReadLine();*/