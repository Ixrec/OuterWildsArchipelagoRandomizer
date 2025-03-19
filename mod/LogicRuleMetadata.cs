using ArchipelagoRandomizer.InGameTracker;
using System.Collections.Generic;
using System.Linq;

namespace ArchipelagoRandomizer;

public class LogicRuleMetadata
{
    public class LogicMetadata
    {
        public string slotDataOption; // the one you added to Options.py and __init__.py
        public string logicCategory;  // the one you used in items.jsonc, locations.jsonc and connections.jsonc
    };

    private static LogicMetadata FeldsparViaDBSurface = new LogicMetadata {
        slotDataOption = "feldspar_via_db_surface",
        logicCategory = "feldspar_via_db_surface"
    };

    public static LogicMetadata[] AllLogicRules = {
        FeldsparViaDBSurface
    };

    public static Dictionary<string, LogicMetadata> LogicCategories = AllLogicRules.ToDictionary(rule => rule.logicCategory);
}


