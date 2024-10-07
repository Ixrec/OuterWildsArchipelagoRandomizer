using ArchipelagoRandomizer.InGameTracker;
using System.Collections.Generic;
using System.Linq;

namespace ArchipelagoRandomizer;

public class StoryModMetadata
{
    public class ModMetadata
    {
        public string trackerCategoryName; // shown directly to the user in the ship log / in-game tracker
        public string modManagerUniqueName;
        public string slotDataOption; // the one you added to Options.py and __init__.py
        public string logicCategory;  // the one you used in items.jsonc, locations.jsonc and connections.jsonc
        public string trackerCategoryImageFile;       // $"/mod/InGameTracker/Icons/{...}.png"
        public string trackerLocationInfosFilePrefix; // $"/mod/InGameTracker/LocationInfos/{...}.jsonc" and "{...}_SL.jsonc"
    };

    private static ModMetadata HN1Metadata = new ModMetadata {
        trackerCategoryName = "Hearth's Neighbor",
        modManagerUniqueName = "GameWyrm.HearthsNeighbor",
        slotDataOption = "enable_hn1_mod",
        logicCategory = "hn1",
        trackerCategoryImageFile = "LonelyHermitIcon",
        trackerLocationInfosFilePrefix = "HN1",
    };
    private static ModMetadata OutsiderMetadata = new ModMetadata
    {
        trackerCategoryName = "The Outsider",
        modManagerUniqueName = "SBtT.TheOutsider",
        slotDataOption = "enable_outsider_mod",
        logicCategory = "to",
        trackerCategoryImageFile = "OutsiderLogo",
        trackerLocationInfosFilePrefix = "TO",
    };
    private static ModMetadata ACMetadata = new ModMetadata
    {
        trackerCategoryName = "Astral Codec",
        modManagerUniqueName = "Walker.AstralCodex",
        slotDataOption = "enable_ac_mod",
        logicCategory = "ac",
        trackerCategoryImageFile = "lingering_chime_map",
        trackerLocationInfosFilePrefix = "AC",
    };

    public static ModMetadata[] AllStoryMods = {
        HN1Metadata,
        OutsiderMetadata,
        ACMetadata
    };

    // The order of this dictionary determines the order of story mod tracker categories the user sees
    public static Dictionary<TrackerCategory, ModMetadata> TrackerCategoryToModMetadata = new Dictionary<TrackerCategory, ModMetadata>
    {
        { TrackerCategory.HearthsNeighbor, HN1Metadata },
        { TrackerCategory.TheOutsider, OutsiderMetadata },
        { TrackerCategory.AstralCodec, ACMetadata },
    };

    public static Dictionary<string, ModMetadata> LogicCategoryToModMetadata = AllStoryMods.ToDictionary(mod => mod.logicCategory);
}


