using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArchipelagoRandomizer.InGameTracker;

internal class WarpLogicHelper
{
    public List<List<string>> Warps { get; set; }

    public static Dictionary<string, string[]> ConnectedRegions { get; } = new()
    {
        { "Brittle Hollow", ["White Hole Station"] },
        { "Hanging City Ceiling", ["White Hole Station", "Brittle Hollow"] },
    };
    public static Dictionary<string, string> WarpPlatformToLogicalRegion { get; } = new()
    {
        { "SS", "Sun Station" },
        { "ST", "Hourglass Twins" },
        { "ET", "Hourglass Twins" },
        { "ETT", "Hourglass Twins" },
        { "ATP", "Ash Twin Interior" },
        { "ATT", "Hourglass Twins" },
        { "TH", "Timber Hearth" },
        { "THT", "Hourglass Twins" },
        { "BHNG", "Brittle Hollow" },
        { "WHS", "White Hole Station" },
        { "BHF", "Hanging City Ceiling" },
        { "BHT", "Hourglass Twins" },
        { "GD", "Giant's Deep" },
        { "GDT", "Hourglass Twins" },
    };
    public static Dictionary<string, string[]> RegionToWarpPlatforms { get; } = new()
    {
        { "Sun Station", ["SS"] },
        { "Hourglass Twins", ["ST", "ET", "ETT", "ATT", "THT", "BHT", "GDT"] },
        { "Ash Twin Interior", ["ATP"] },
        { "Timber Hearth", ["TH"] },
        { "Brittle Hollow", ["BHNG"] },
        { "White Hole Station", ["WHS"] },
        { "Hanging City Ceiling", ["BHF"] },
        { "Giant's Deep", ["GD"] },
    };
    public static Dictionary<string, HashSet<string>> WarpPlatformRequiredItems { get; } = new()
    {
        { "SS", [ "Spacesuit" ] },
    };

    public bool ConnectionExists(string fromRegion, string toRegion, bool twoWay = false, List<string> skipRegions = null, StringBuilder routeText = null, int iteration = 0)
    {
        var prefix = new string('\t', iteration);
        iteration++;
        // Avoid infinite recursion by keeping list of what regions have been checked.
        skipRegions ??= new List<string>();
        if (skipRegions.Contains(fromRegion) || !RegionToWarpPlatforms.ContainsKey(fromRegion))
            return false;
        routeText ??= new StringBuilder();
        // Check not only local warps but also ones we can walk to (ie. BH's multiple warps).
        var reachableRegions = new List<string>() { fromRegion };
        var connectedRegions = ConnectedRegions.GetValueOrDefault(fromRegion, []);
        if (twoWay) // Check if we can get back.
        {
            foreach (var region in connectedRegions)
                if (ConnectedRegions.GetValueOrDefault(region, []).Contains(fromRegion))
                    reachableRegions.Add(region);
        }
        else
            reachableRegions.AddRange(connectedRegions);
        skipRegions.AddRange(reachableRegions);

        APRandomizer.OWMLModConsole.WriteLine($"{prefix}Checking {string.Join(", ", reachableRegions)}", OWML.Common.MessageType.Debug);
        foreach (var region in reachableRegions)
            foreach (var warp in RegionToWarpPlatforms[region])
            {
                var toWarp = GetConnection(warp);
                var targetRegion = WarpPlatformToLogicalRegion[toWarp];
                APRandomizer.OWMLModConsole.WriteLine($"{prefix}{region}: {warp} - {targetRegion}: {toWarp}", OWML.Common.MessageType.Debug);
                if (targetRegion == toRegion || ConnectionExists(targetRegion, toRegion, twoWay, skipRegions, routeText, iteration))
                {
                    routeText.Insert(0, $", {warp} -> {toWarp}");
                    if (iteration == 1)
                        APRandomizer.OWMLModConsole.WriteLine($"Found route between {fromRegion} - {toRegion}{routeText}", OWML.Common.MessageType.Debug);
                    return true;
                }
            }

        if (iteration == 1)
            APRandomizer.OWMLModConsole.WriteLine($"No warp route between {fromRegion} - {toRegion}", OWML.Common.MessageType.Debug);
        return false;
    }

    public string GetConnection(string fromWarp)
    {
        foreach (var connection in Warps)
        {
            if (connection[0] == fromWarp)
                return connection[1];
            if (connection[1] == fromWarp)
                return connection[0];
        }
        return string.Empty;
    }
}
