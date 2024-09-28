using System;

namespace SMassTrans.Avalonia;

public static class Search
{
    //https://en.wikibooks.org/wiki/Algorithm_Implementation/Strings/Levenshtein_distance#C.23
    //CC BY-SA 4.0
    
    public static int LevenshteinDistance(string source, string target){
        if(string.IsNullOrEmpty(source))
        {
            return string.IsNullOrEmpty(target) ? 0 : target.Length;
        }
        if(string.IsNullOrEmpty(target)) return source.Length;

        if(source.Length > target.Length){
            (target, source) = (source, target);
        }
 
        var m = target.Length;
        var n = source.Length;
        var distance = new int[2, m + 1];
        // Initialize the distance matrix
        for(var j = 1; j <= m; j++) distance[0, j] = j;

        var currentRow = 0;
        for(var i = 1; i <= n; ++i){
            currentRow = i & 1;
            distance[currentRow, 0] = i;
            var previousRow = currentRow ^ 1;
            for(var j = 1; j <= m; j++) {
                var cost = (target[j - 1] == source[i - 1] ? 0 : 1);
                distance[currentRow, j] = Math.Min(Math.Min(
                        distance[previousRow, j] + 1,
                        distance[currentRow, j - 1] + 1),
                    distance[previousRow, j - 1] + cost);
            }
        }
        return distance[currentRow, m];
    }
    public static int ModifiedLevenshteinDistance(string source, string target){
        if(string.IsNullOrEmpty(source))
        {
            return string.IsNullOrEmpty(target) ? 0 : target.Length;
        }
        if(string.IsNullOrEmpty(target)) return source.Length;
        int boost = 0;
        if (source.StartsWith(target))
        {
            boost = -18;
        }
        else if (source.Contains(target))
        {
            boost = -16;
        }
        if(source.Length > target.Length){
            (target, source) = (source, target);
        }
 
        var m = target.Length;
        var n = source.Length;
        var distance = new int[2, m + 1];
        // Initialize the distance matrix
        for(var j = 1; j <= m; j++) distance[0, j] = j;

        var currentRow = 0;
        for(var i = 1; i <= n; ++i){
            currentRow = i & 1;
            distance[currentRow, 0] = i;
            var previousRow = currentRow ^ 1;
            for(var j = 1; j <= m; j++) {
                var cost = (target[j - 1] == source[i - 1] ? 0 : 1);
                distance[currentRow, j] = Math.Min(Math.Min(
                        distance[previousRow, j] + 1,
                        distance[currentRow, j - 1] + 1),
                    distance[previousRow, j - 1] + cost);
            }
        }
        return distance[currentRow, m]+boost;
    }
}