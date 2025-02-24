using ArchipelagoRandomizer.InGameTracker;
using System.Collections.Generic;
using System.Linq;

namespace ArchipelagoRandomizer;

public class LogicRuleMetadata
{
    public class LogicMetadata
    {
        public string trackerCategoryName; // shown directly to the user in the ship log / in-game tracker
        public string slotDataOption; // the one you added to Options.py and __init__.py
        public string logicCategory;  // the one you used in items.jsonc, locations.jsonc and connections.jsonc
    };

    private static LogicMetadata FeldsparQuickAccess = new LogicMetadata {
        trackerCategoryName = "Feldspar via Quick Access",
        slotDataOption = "feldspar_quick_access",
        logicCategory = "feldspar_quick_access"
    };

    public static LogicMetadata[] AllLogicRules = {
        FeldsparQuickAccess
    };

    public static Dictionary<string, LogicMetadata> LogicCategories = AllLogicRules.ToDictionary(rule => rule.logicCategory);
}


