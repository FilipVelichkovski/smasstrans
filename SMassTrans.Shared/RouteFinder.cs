using SMassTrans.Shared.ModeShift;

namespace SMassTrans.Shared;

public class RouteFinder : RouteFinder<Stop>
{
}

public class RouteFinder<TStop> where TStop : IStop
{
    public record Point(double lon, double lat);

    public async Task<List<List<(TStop, LineRouteId)>>> FindPathAsync(Point start, Point end, IClient<TStop, LineInfo> client, IEnumerable<LineInfo> lines)
    {
        // 105 = osnovni ucilista
        // 106 = lica so posebni potrebi
        int[] blocklistedLines = [105, 106];
        var allstops = await client.GetStops();
        var routeinfo = await client.GetAllRouteInfo();
        var startStops = allstops.Select(x => new Tuple<double, TStop>(CoordinateHelper.GetDistanceTo(start.lat, start.lon, x.Lat, x.Lon), x))
            .OrderBy(x => x.Item1).Take(5).Select(x => x.Item2).ToList();
        var endStops = allstops.Select(x => new Tuple<double, TStop>(CoordinateHelper.GetDistanceTo(end.lat, end.lon, x.Lat, x.Lon), x))
            .OrderBy(x => x.Item1).Take(5).Select(x => x.Item2).ToList();

        // Build stop->lines mapping from routeinfo (no API calls)
        var stopLinesDict = new Dictionary<int, List<LineRouteId>>();
        foreach (var route in routeinfo)
        {
            foreach (var stopIdStr in route.StopIds)
            {
                if (!int.TryParse(stopIdStr, out int stopId)) continue;
                if (!stopLinesDict.ContainsKey(stopId))
                    stopLinesDict[stopId] = new List<LineRouteId>();
                stopLinesDict[stopId].Add(route.LineRouteId);
            }
        }

        // BFS node: (currentStop, pathSoFar, linesUsed, hops, currentLine, currentIndexInRoute)
        var queue = new Queue<(TStop stop, List<(TStop, LineRouteId)> path, List<LineRouteId> linesUsed, int hops, LineRouteId? currentLine, int? currentIdx)>();
        var visited = new HashSet<(int stopId, string lineId, int hops, string? currentLineId, int? currentIdx)>();

        // Build a lookup for LineRouteId -> RouteInfo
        var lineRouteDict = routeinfo.ToDictionary(r => r.LineRouteId);

        foreach (var startStop in startStops)
        {
            if (!stopLinesDict.TryGetValue(startStop.StopId, out var linesAtStart)) continue;
            foreach (var line in linesAtStart)
            {
                if (!lineRouteDict.TryGetValue(line, out var route)) continue;
                var idx = route.StopIds.FindIndex(sid => int.TryParse(sid, out var id) && id == startStop.StopId);
                if (idx == -1) continue;
                queue.Enqueue((startStop, new List<(TStop, LineRouteId)> { (startStop, line) }, new List<LineRouteId> { line }, 0, line, idx));
                visited.Add((startStop.StopId, line.LineId, 0, line.LineId, idx));
            }
        }

        var foundPaths = new List<List<(TStop, LineRouteId)>>();
        const int maxPaths = 30;
        while (queue.Count > 0 && foundPaths.Count < maxPaths)
        {
            var (currentStop, path, linesUsed, hops, currentLine, currentIdx) = queue.Dequeue();

            // Check if current stop is close to any end stop
            if (endStops.Any(es => es.StopId == currentStop.StopId))
            {
                foundPaths.Add(new List<(TStop, LineRouteId)>(path));
                continue;
            }

            if (hops >= 3) continue; // max 3 hops (4 lines)

            bool busMoved = false;

            // Continue on the same line (forward only)
            if (currentLine != null && currentIdx != null && lineRouteDict.TryGetValue(currentLine.Value, out var route))
            {
                // Only move forward in the route
                for (int nextIdx = currentIdx.Value + 1; nextIdx < route.StopIds.Count; nextIdx++)
                {
                    if (!int.TryParse(route.StopIds[nextIdx], out int nextStopId)) continue;
                    var nextStop = allstops.FirstOrDefault(s => s.StopId == nextStopId);
                    if (nextStop == null) continue;
                    if (visited.Contains((nextStop.StopId, currentLine.Value.LineId, hops, currentLine.Value.LineId, nextIdx))) continue;
                    visited.Add((nextStop.StopId, currentLine.Value.LineId, hops, currentLine.Value.LineId, nextIdx));
                    var newPath = new List<(TStop, LineRouteId)>(path) { (nextStop, currentLine.Value) };
                    queue.Enqueue((nextStop, newPath, new List<LineRouteId>(linesUsed), hops, currentLine, nextIdx));
                    busMoved = true;
                }
            }

            // Transfers: try all other lines at this stop that go forward
            if (stopLinesDict.TryGetValue(currentStop.StopId, out var transferLines))
            {
                foreach (var transferLine in transferLines)
                {
                    if (currentLine != null && transferLine.LineId == currentLine.Value.LineId) continue; // skip same line
                    if (!lineRouteDict.TryGetValue(transferLine, out var transferRoute)) continue;
                    var idx = transferRoute.StopIds.FindIndex(sid => int.TryParse(sid, out var id) && id == currentStop.StopId);
                    if (idx == -1 || idx == transferRoute.StopIds.Count - 1) continue; // must not be last stop
                    // Only allow transfer if the next stop is closer to the end point
                    if (!int.TryParse(transferRoute.StopIds[idx + 1], out int nextStopId)) continue;
                    var nextStop = allstops.FirstOrDefault(s => s.StopId == nextStopId);
                    if (nextStop == null) continue;
                    var curDist = CoordinateHelper.GetDistanceTo(end.lat, end.lon, currentStop.Lat, currentStop.Lon);
                    var nextDist = CoordinateHelper.GetDistanceTo(end.lat, end.lon, nextStop.Lat, nextStop.Lon);
                    if (nextDist >= curDist) continue;
                    int nextHops = hops + 1;
                    if (nextHops > 3) continue;
                    if (visited.Contains((nextStop.StopId, transferLine.LineId, nextHops, transferLine.LineId, idx + 1))) continue;
                    visited.Add((nextStop.StopId, transferLine.LineId, nextHops, transferLine.LineId, idx + 1));
                    var newPath = new List<(TStop, LineRouteId)>(path) { (nextStop, transferLine) };
                    var newLinesUsed = new List<LineRouteId>(linesUsed) { transferLine };
                    queue.Enqueue((nextStop, newPath, newLinesUsed, nextHops, transferLine, idx + 1));
                    busMoved = true;
                }
            }

            if (!busMoved)
            {
                double walkDistance = 300.0;
                var walkedStops = allstops.Where(s => s.StopId != currentStop.StopId &&
                    CoordinateHelper.GetDistanceTo(currentStop.Lat, currentStop.Lon, s.Lat, s.Lon) <= walkDistance &&
                    !path.Any(p => p.Item1.StopId == s.StopId)).ToList();
                foreach (var walkStop in walkedStops)
                {
                    if (visited.Contains((walkStop.StopId, "WALK", hops, null, null))) continue;
                    visited.Add((walkStop.StopId, "WALK", hops, null, null));
                    var newPath = new List<(TStop, LineRouteId)>(path) { (walkStop, default(LineRouteId)) };
                    queue.Enqueue((walkStop, newPath, new List<LineRouteId>(linesUsed), hops, null, null));
                }
            }
        }

        return foundPaths;
    }
}