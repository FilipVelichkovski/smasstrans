using SMassTrans.Shared;

public class BusStopCLIApp
{
    private readonly Skopska _client;

    public BusStopCLIApp(Skopska client)
    {
        _client = client;
    }

    public async Task InitializeAsync()
    {
        await _client.InitAsync();
    }

    public async Task SearchAndDisplayNextBusesAsync(string stopNameQuery)
    {
        var stops = await _client.GetStops();
        var matchingStops = stops.Where(x => x.StopName.Contains(stopNameQuery, StringComparison.OrdinalIgnoreCase));

        foreach (var stop in matchingStops)
        {
            var linesAndTimes = await _client.GetStopLinesForStopId(stop.StopId.ToString("0000"));
            Console.WriteLine($"{stop.StopName}:");

            foreach (var (lineRouteId, times) in linesAndTimes)
            {
                if (times.Count == 0) continue;

                var routeInfo = await _client.GetRouteInfo(lineRouteId);
                var lineInfo = await _client.GetLineInfo(lineRouteId);

                Console.WriteLine($" Line: {lineInfo.Name} - {routeInfo.Name}");
                Console.WriteLine($" Next buses in minutes: {string.Join(", ", times.Select(t => t / 60))}");
            }
        }
    }

    public async Task<string> AutocompleteSearchAsync()
    {
        Console.WriteLine("name of the stop you're searching for: ");
        string input = string.Empty;
        IEnumerable<string> suggestions =[];
        while (true)
        {
            var keyInfo = Console.ReadKey(intercept: true);
            if (keyInfo.Key == ConsoleKey.Enter && suggestions.Any())
            {
                Console.WriteLine();
                break;
            }
            else if (keyInfo.Key == ConsoleKey.Backspace && input.Length > 0)
            {
                Console.CursorLeft = input.Length - 1;
                Console.Write(' ');
                input = input[0..^1];
            }
            else if (!char.IsControl(keyInfo.KeyChar))
            {
                input += keyInfo.KeyChar;
                Console.CursorLeft = 0;
                //Console.CursorTop--;
                Console.WriteLine(input);
                suggestions = await FetchSuggestionsAsync(input);
                DisplaySuggestions(suggestions);
            }
        }

        return suggestions.First();
    }

    private async Task<IEnumerable<string>> FetchSuggestionsAsync(string input)
    {
        var allStops = await _client.GetStops();
        return allStops.Where(s => s.StopName.StartsWith(input, StringComparison.OrdinalIgnoreCase))
                       .Select(s => s.StopName);
    }

    private void DisplaySuggestions(IEnumerable<string> suggestions)
    {
        var a = Console.CursorTop;
        Console.CursorTop++;
        Console.CursorLeft = 0;
        Console.Write(new string(' ', Console.WindowWidth));
        //Console.CursorTop--;

        Console.CursorLeft = 0;
        Console.WriteLine(string.Join(", ", suggestions));
        Console.CursorTop =a;
       // Console.CursorTop -= suggestions.Count() > 0 ? 1 : 0;
    }

}
