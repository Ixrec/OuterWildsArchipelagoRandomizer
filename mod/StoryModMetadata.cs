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
        public string trackerLocationInfosFilePrefix; // $"/mod/InGameTracker/LocationInfos/{...}.jsonc" and "{...}_SLF.jsonc"
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
        // copy-pasted https://github.com/StreetlightsBehindTheTrees/Outer-Wilds-The-Outsider/blob/main/TheOutsiderFixes/assets/TheOutsiderLogo.png
        // and combined it with the vanilla DB ship log image
        trackerCategoryImageFile = "TheOutsider",
        trackerLocationInfosFilePrefix = "TO",
    };
    private static ModMetadata ACMetadata = new ModMetadata
    {
        trackerCategoryName = "Astral Codec",
        modManagerUniqueName = "Walker.AstralCodex",
        slotDataOption = "enable_ac_mod",
        logicCategory = "ac",
        // copy-pasted from https://github.com/2walker2/Astral-Codex/blob/main/Walker.AstralCodex/planets/ship_log/images/lingering_chime_map.png
        trackerCategoryImageFile = "lingering_chime_map",
        trackerLocationInfosFilePrefix = "AC",
    };
    private static ModMetadata HN2Metadata = new ModMetadata
    {
        trackerCategoryName = "Hearth's Neighbor 2: Magistarium",
        modManagerUniqueName = "GameWyrm.HearthsNeighbor2",
        slotDataOption = "enable_hn2_mod",
        logicCategory = "hn2",
        // copy-pasted from https://github.com/GameWyrm/hearths-neighbor-2/blob/master/HearthsNeighbor2/planets/magistarium.png
        trackerCategoryImageFile = "magistarium",
        trackerLocationInfosFilePrefix = "HN2",
    };
    private static ModMetadata FQMetadata = new ModMetadata
    {
        trackerCategoryName = "Fret's Quest",
        modManagerUniqueName = "Samster68.FretsQuest",
        slotDataOption = "enable_fq_mod",
        logicCategory = "fq",
        // copy-pasted https://github.com/Samster68OW/fretsquest/blob/main/old_subtitle.png and put it on top of
        // https://github.com/Samster68OW/fretsquest/blob/main/planets/ShipLogs/icons/UI_Magic_Banjo.png
        trackerCategoryImageFile = "FretsQuest",
        trackerLocationInfosFilePrefix = "FQ",
    };

    public static ModMetadata[] AllStoryMods = {
        OutsiderMetadata,
        ACMetadata,
        HN1Metadata,
        HN2Metadata,
        FQMetadata
    };

    // The order of this dictionary determines the order of story mod tracker categories the user sees
    public static Dictionary<TrackerCategory, ModMetadata> TrackerCategoryToModMetadata = new Dictionary<TrackerCategory, ModMetadata>
    {
        { TrackerCategory.TheOutsider, OutsiderMetadata },
        { TrackerCategory.AstralCodec, ACMetadata },
        { TrackerCategory.HearthsNeighbor, HN1Metadata },
        { TrackerCategory.HearthsNeighbor2Magistarium, HN2Metadata },
        { TrackerCategory.FretsQuest, FQMetadata },
    };

    public static Dictionary<string, ModMetadata> LogicCategoryToModMetadata = AllStoryMods.ToDictionary(mod => mod.logicCategory);
}


