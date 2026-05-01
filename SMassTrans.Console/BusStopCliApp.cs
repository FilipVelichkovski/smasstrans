using SMassTrans.Shared;

namespace SMassTrans.Console;

public class BusStopCliApp
{
    private readonly Skopska _client;

    public BusStopCliApp(Skopska client)
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
            var linesAndTimes = await _client.GetLiveStopLinesForStopAsync(stop.StopId.ToString("0000"));
            System.Console.WriteLine($"{stop.StopName}:");

            foreach (var (lineRouteId, times) in linesAndTimes)
            {
                if (times.Count == 0) continue;

                var routeInfo = await _client.GetRouteInfo(lineRouteId);
                var lineInfo = await _client.GetLineInfo(lineRouteId);

                System.Console.WriteLine($" Line: {lineInfo.Name} - {routeInfo.Name}");
                System.Console.WriteLine($" Next buses in minutes: {string.Join(", ", times.Select(t => t / 60))}");
            }
        }
    }

    public async Task<string> AutocompleteSearchAsync()
    {
        System.Console.WriteLine("name of the stop you're searching for: ");
        string input = string.Empty;
        IEnumerable<string> suggestions =[];
        while (true)
        {
            var keyInfo = System.Console.ReadKey(intercept: true);
            if (keyInfo.Key == ConsoleKey.Enter && suggestions.Any())
            {
                System.Console.WriteLine();
                break;
            }
            else if (keyInfo.Key == ConsoleKey.Backspace && input.Length > 0)
            {
                System.Console.CursorLeft = input.Length - 1;
                System.Console.Write(' ');
                input = input[0..^1];
            }
            else if (!char.IsControl(keyInfo.KeyChar))
            {
                input += keyInfo.KeyChar;
                System.Console.CursorLeft = 0;
                //Console.CursorTop--;
                System.Console.WriteLine(input);
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
        var a = System.Console.CursorTop;
        System.Console.CursorTop++;
        System.Console.CursorLeft = 0;
        System.Console.Write(new string(' ', System.Console.WindowWidth));
        //Console.CursorTop--;

        System.Console.CursorLeft = 0;
        System.Console.WriteLine(string.Join(", ", suggestions));
        System.Console.CursorTop =a;
        // Console.CursorTop -= suggestions.Count() > 0 ? 1 : 0;
    }

}