using SMassTrans.Console;
using SMassTrans.Shared;
using SMassTrans.Shared.ModeShift;

HttpClient hclient = new();
// ModeShift (SkopjeBus) client implementing IClient<Stop, LineInfo>
var client = new ModeShiftClient(hclient);
await client.InitAsync();
var app = new BusStopCliApp<Stop>(client);

await app.InitializeAsync();
var stopNameQuery = await app.AutocompleteSearchAsync();
await app.SearchAndDisplayNextBusesAsync(stopNameQuery);