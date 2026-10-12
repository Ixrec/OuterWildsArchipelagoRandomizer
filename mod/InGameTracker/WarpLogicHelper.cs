using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArchipelagoRandomizer.InGameTracker;

internal class WarpLogicHelper
{
    public List<List<string>> Warps { get; set; }

    public Dictionary<string, string> WarpPlatformToLogicalRegion { get; } = new()
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
    public Dictionary<string, string[]> RegionToWarpPlatforms { get; } = new()
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
    public Dictionary<string, HashSet<string>> WarpPlatformRequiredItems { get; } = new()
    {
        { "SS", [ "Spacesuit" ] },
        { "ST", [ "Spacesuit" ] },
    };

    public bool ShiplessConnectionExists(string fromRegion, string toRegion, bool twoWay = false)
    {
        if (!RegionToWarpPlatforms.ContainsKey(fromRegion))
            return false;
        return ConnectionExistsRecursiveImpl(fromRegion, toRegion, twoWay, new List<string>(), new StringBuilder(), 0);
    }

    private Dictionary<string, string[]> ItemlessWarpRegionConnections { get; } = new()
    {
        { "Brittle Hollow", ["White Hole Station"] },
        { "Hanging City Ceiling", ["White Hole Station", "Brittle Hollow"] },
    };

    // Here we make the strong assumption that all logic outside of this file is irrelevant,
    // because this is only used to tell if a "shipless warp path" exists between two platforms
    private bool ConnectionExistsRecursiveImpl(string fromRegion, string toRegion, bool twoWay, List<string> skipRegions, StringBuilder routeText, int iteration)
    {
        var prefix = new string('\t', iteration);
        iteration++;

        if (skipRegions.Contains(fromRegion))
            return false;
        // Check not only local warps but also ones we can walk to (ie. BH's multiple warps).
        var warpRegionsReachableWarpless = new List<string>() { fromRegion };
        if (ItemlessWarpRegionConnections.ContainsKey(fromRegion))
            if (twoWay) // Check if we can get back.
                foreach (var region in ItemlessWarpRegionConnections[fromRegion])
                    if (ItemlessWarpRegionConnections[region].Contains(fromRegion))
                        warpRegionsReachableWarpless.Add(region);
            else
                warpRegionsReachableWarpless.AddRange(ItemlessWarpRegionConnections[fromRegion]);
        skipRegions.AddRange(warpRegionsReachableWarpless);

        APRandomizer.OWMLModConsole.WriteLine($"{prefix}Checking {string.Join(", ", warpRegionsReachableWarpless)}", OWML.Common.MessageType.Debug);
        foreach (var region in warpRegionsReachableWarpless)
            foreach (var fromWarp in RegionToWarpPlatforms[region])
            {
                var toWarp = GetConnection(fromWarp);
                var nextTargetRegion = WarpPlatformToLogicalRegion[toWarp];
                APRandomizer.OWMLModConsole.WriteLine($"{prefix}{region}: {fromWarp} - {nextTargetRegion}: {toWarp}", OWML.Common.MessageType.Debug);
                if (nextTargetRegion == toRegion || ConnectionExistsRecursiveImpl(nextTargetRegion, toRegion, twoWay, skipRegions, routeText, iteration))
                {
                    routeText.Insert(0, $", {fromWarp} -> {toWarp}");
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
