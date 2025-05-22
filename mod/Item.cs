using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ArchipelagoRandomizer;

public enum Item
{
    LaunchCodes,
    Spaceship, // no longer in use; keeping for backwards compatibility with existing mod save files

    Spacesuit,
    Translator,
    Signalscope,
    Scout,
    CameraGM,
    CameraQuantum,
    WarpPlatformCodes,
    WarpCoreManual,
    EntanglementRule,
    ShrineDoorCodes,
    TornadoAdjustment,
    SilentRunning,
    ElectricalInsulation,
    Coordinates,

    FrequencyDB,
    FrequencyQF,
    FrequencyHS,

    SignalChert,
    SignalEsker,
    SignalRiebeck,
    SignalGabbro,
    SignalFeldspar,
    SignalMuseumShard,
    SignalGroveShard,
    SignalCaveShard,
    SignalTowerShard,
    SignalIslandShard,
    SignalQM,
    SignalEP1,
    SignalEP2,
    SignalEP3,
    SignalGalena,
    SignalTephra,

    Nothing,

    Autopilot,
    LandingCamera,
    EjectButton,
    VelocityMatcher,
    SurfaceIntegrityScanner,

    // The following items are non-unique, i.e. the player can and likely will receive more than 1 of each.
    // The tracker currently relies on this item order to tell whether to display an X or a number.
    OxygenCapacityUpgrade,
    FuelCapacityUpgrade,
    BoostDurationUpgrade,

    OxygenRefill,
    FuelRefill,
    Marshmallow,
    PerfectMarshmallow,
    BurntMarshmallow,

    ShipDamageTrap,
    AudioTrap,
    NapTrap,
    SuitPunctureTrap,
    MapDisableTrap,
    HUDCorruptionTrap,
    // end of non-unique items

    FrequencyDSR,
    SignalDSRTower,
    SignalDSRSatellite,

    LightModulator,
    BreachOverrideCodes,
    RLPaintingCode,
    CIPaintingCode,
    HGPaintingCode,
    DreamTotemPatch,
    RaftDocksPatch,
    LimboWarpPatch,
    ProjectionRangePatch,
    AlarmBypassPatch,

    TranslatorHGT,
    TranslatorTH,
    TranslatorBH,
    TranslatorGD,
    TranslatorDB,
    TranslatorOther,

    // Hearth's Neighbor items
    FrequencyNeighborDistress,
    FrequencyLavaCore,
    FrequencyGalacticCommunication,

    // The Outsider has no signal frequencies or custom items

    // Astral Codec items
    FrequencyAstralCodec,

    // Hearth's Neighbor 2: Magistarium items
    MemoryCubeInterface,
    MagistariumLibraryAccessCode,
    MagistariumDormitoryAccessCode,
    MagistariumEngineAccessCode,

    // Fret's Quest items
    FrequencyHearthianRadio,
};

public static class ItemNames
{
    // Used to help the Inventory Tracker show a single "Story Mod Frequencies" entry for all of these similar items
    private static Item[] StoryModFrequencies = {
        Item.FrequencyNeighborDistress,
        Item.FrequencyLavaCore,
        Item.FrequencyGalacticCommunication,
        Item.FrequencyAstralCodec,
        Item.FrequencyHearthianRadio,
    };
    public static bool IsStoryModFrequency(Item item) => StoryModFrequencies.Contains(item);

    public static Dictionary<Item, string> itemNames = new Dictionary<Item, string> {
        { Item.LaunchCodes, "Launch Codes" },

        { Item.Spacesuit, "Spacesuit" },
        { Item.Translator, "Translator" },
        { Item.Signalscope, "Signalscope" },
        { Item.Scout, "Scout" },
        { Item.CameraGM, "Ghost Matter Wavelength" },
        { Item.CameraQuantum, "Imaging Rule" },
        { Item.WarpPlatformCodes, "Nomai Warp Codes" },
        { Item.WarpCoreManual, "Warp Core Installation Manual" },
        { Item.EntanglementRule, "Entanglement Rule" },
        { Item.ShrineDoorCodes, "Shrine Door Codes" },
        { Item.TornadoAdjustment, "Tornado Aerodynamic Adjustments" },
        { Item.SilentRunning, "Silent Running Mode" },
        { Item.ElectricalInsulation, "Electrical Insulation" },
        { Item.Coordinates, "Coordinates" },

        { Item.FrequencyDB, "Distress Beacon Frequency" },
        { Item.FrequencyQF, "Quantum Fluctuations Frequency" },
        { Item.FrequencyHS, "Hide & Seek Frequency" },

        { Item.SignalChert, "Chert's Signal" },
        { Item.SignalEsker, "Esker's Signal" },
        { Item.SignalRiebeck, "Riebeck's Signal" },
        { Item.SignalGabbro, "Gabbro's Signal" },
        { Item.SignalFeldspar, "Feldspar's Signal" },
        { Item.SignalMuseumShard, "Museum Shard Signal" },
        { Item.SignalGroveShard, "Grove Shard Signal" },
        { Item.SignalCaveShard, "Cave Shard Signal" },
        { Item.SignalTowerShard, "Tower Shard Signal" },
        { Item.SignalIslandShard, "Island Shard Signal" },
        { Item.SignalQM, "Quantum Moon Signal" },
        { Item.SignalEP1, "Escape Pod 1 Signal" },
        { Item.SignalEP2, "Escape Pod 2 Signal" },
        { Item.SignalEP3, "Escape Pod 3 Signal" },
        { Item.SignalGalena, "Galena's Radio Signal" },
        { Item.SignalTephra, "Tephra's Radio Signal" },

        { Item.Nothing, "Nothing" },

        { Item.Autopilot, "Autopilot" },
        { Item.LandingCamera, "Landing Camera" },
        { Item.EjectButton, "Eject Button" },
        { Item.VelocityMatcher, "Velocity Matcher" },
        { Item.SurfaceIntegrityScanner, "Surface Integrity Scanner" },
        { Item.OxygenCapacityUpgrade, "Oxygen Capacity Upgrade" },
        { Item.FuelCapacityUpgrade, "Fuel Capacity Upgrade" },
        { Item.BoostDurationUpgrade, "Boost Duration Upgrade" },

        { Item.OxygenRefill, "Oxygen Refill" },
        { Item.FuelRefill, "Jetpack Fuel Refill" },
        { Item.Marshmallow, "Marshmallow" },
        { Item.PerfectMarshmallow, "Perfect Marshmallow" },
        { Item.BurntMarshmallow, "Burnt Marshmallow" },

        { Item.ShipDamageTrap, "Ship Damage Trap" },
        { Item.AudioTrap, "Audio Trap" },
        { Item.NapTrap, "Nap Trap" },
        { Item.SuitPunctureTrap, "Suit Puncture Trap" },
        { Item.MapDisableTrap, "Map Disable Trap" },
        { Item.HUDCorruptionTrap, "HUD Corruption Trap" },

        { Item.FrequencyDSR, "Deep Space Radio Frequency" },
        { Item.SignalDSRTower, "Radio Tower Signal" },
        { Item.SignalDSRSatellite, "Deep Space Satellite Signal" },

        { Item.LightModulator, "Stranger Light Modulator" },
        { Item.BreachOverrideCodes, "Breach Override Codes" },
        { Item.RLPaintingCode, "River Lowlands Painting Code" },
        { Item.CIPaintingCode, "Cinder Isles Painting Code" },
        { Item.HGPaintingCode, "Hidden Gorge Painting Code" },
        { Item.DreamTotemPatch, "Dream Totem Patch" },
        { Item.RaftDocksPatch, "Raft Docks Patch" },
        { Item.LimboWarpPatch, "Limbo Warp Patch" },
        { Item.ProjectionRangePatch, "Projection Range Patch" },
        { Item.AlarmBypassPatch, "Alarm Bypass Patch" },

        { Item.TranslatorHGT, "Translator (Hourglass Twins)" },
        { Item.TranslatorTH, "Translator (Timber Hearth)" },
        { Item.TranslatorBH, "Translator (Brittle Hollow)" },
        { Item.TranslatorGD, "Translator (Giant's Deep)" },
        { Item.TranslatorDB, "Translator (Dark Bramble)" },
        { Item.TranslatorOther, "Translator (Other)" },

        // Hearth's Neighbor items
        { Item.FrequencyNeighborDistress, "Neighbor's Distress Signal Frequency" },
        { Item.FrequencyLavaCore, "Lava Core Signals Frequency" },
        { Item.FrequencyGalacticCommunication, "Galactic Communication Frequency" },

        // The Outsider has no signal frequencies or custom items

        // Astral Codec items
        { Item.FrequencyAstralCodec, "Astral Codec Frequency" },

        // Hearth's Neighbor 2: Magistarium items
        { Item.MemoryCubeInterface, "Memory Cube Interface" },
        { Item.MagistariumLibraryAccessCode, "Magistarium Library Access Code" },
        { Item.MagistariumDormitoryAccessCode, "Magistarium Dormitory Access Code" },
        { Item.MagistariumEngineAccessCode, "Magistarium Engine Access Code" },

        // Fret's Quest items
        { Item.FrequencyHearthianRadio, "Hearthian Radio Frequency" },
    };

    public static Dictionary<string, Item> itemNamesReversed = itemNames.ToDictionary(itemName => itemName.Value, itemName => itemName.Key);

    public static string ItemToName(Item item) => itemNames[item];
    public static Item NameToItem(string itemName) => itemNamesReversed[itemName];

    public static Dictionary<string, Item> frequencyToItem = new Dictionary<string, Item>
    {
        { "EscapePod", Item.FrequencyDB },
        { "Quantum", Item.FrequencyQF },
        { "HideAndSeek", Item.FrequencyHS },
        { "Radio", Item.FrequencyDSR },

        // Hearth's Neighbor frequency items
        { "NEIGHBOR'S DISTRESS SIGNAL", Item.FrequencyNeighborDistress },
        { "Lava Core Signals", Item.FrequencyLavaCore },
        { "GALACTIC COMMUNICATION", Item.FrequencyGalacticCommunication },

        // The Outsider has no signal frequencies

        // Astral Codec frequency items
        { "Astral Codec", Item.FrequencyAstralCodec },

        // Fret's Quest frequency items
        { "Hearthian Radio", Item.FrequencyHearthianRadio },
    };
    public static Dictionary<Item, string> itemToFrequency = frequencyToItem.ToDictionary(fti => fti.Value, fti => fti.Key);

    public static Dictionary<string, Item> signalToItem = new Dictionary<string, Item>
    {
        { "Traveler_Chert", Item.SignalChert },
        { "Traveler_Esker", Item.SignalEsker },
        { "Traveler_Riebeck", Item.SignalRiebeck },
        { "Traveler_Gabbro", Item.SignalGabbro },
        { "Traveler_Feldspar", Item.SignalFeldspar },
        { "Quantum_TH_MuseumShard", Item.SignalMuseumShard },
        { "Quantum_TH_GroveShard", Item.SignalGroveShard },
        { "Quantum_CT_Shard", Item.SignalCaveShard },
        { "Quantum_BH_Shard", Item.SignalTowerShard },
        { "Quantum_GD_Shard", Item.SignalIslandShard },
        { "Quantum_QM", Item.SignalQM },
        { "EscapePod_BH", Item.SignalEP1 },
        { "EscapePod_CT", Item.SignalEP2 },
        { "EscapePod_DB", Item.SignalEP3 },
        { "HideAndSeek_Galena", Item.SignalGalena },
        { "HideAndSeek_Tephra", Item.SignalTephra },
        { "RadioTower", Item.SignalDSRTower },
        { "MapSatellite", Item.SignalDSRSatellite },
    };
    public static Dictionary<Item, string> itemToSignal = signalToItem.ToDictionary(sti => sti.Value, sti => sti.Key);

    // The OW autosplitter can be driven by persistent condition changes, so these enable autosplitting with the randomizer.
    // For now, we define a condition for every progression item.
    public static Dictionary<Item, string> itemToPersistentCondition = new Dictionary<Item, string>
    {
        { Item.LaunchCodes,          "HAS_AP_ITEM_LAUNCH_CODES" },
        { Item.Spacesuit,            "HAS_AP_ITEM_SPACESUIT" },
        { Item.Translator,           "HAS_AP_ITEM_TRANSLATOR" },
        { Item.TranslatorHGT,        "HAS_AP_ITEM_TRANSLATOR_HOURGLASS_TWINS" },
        { Item.TranslatorTH,         "HAS_AP_ITEM_TRANSLATOR_TIMBER_HEARTH" },
        { Item.TranslatorBH,         "HAS_AP_ITEM_TRANSLATOR_BRITTLE_HOLLOW" },
        { Item.TranslatorGD,         "HAS_AP_ITEM_TRANSLATOR_GIANTS_DEEP" },
        { Item.TranslatorDB,         "HAS_AP_ITEM_TRANSLATOR_DARK_BRAMBLE" },
        { Item.TranslatorOther,      "HAS_AP_ITEM_TRANSLATOR_OTHER" },
        { Item.Signalscope,          "HAS_AP_ITEM_SIGNALSCOPE" },
        { Item.Scout,                "HAS_AP_ITEM_SCOUT" },
        { Item.CameraGM,             "HAS_AP_ITEM_GHOST_MATTER_WAVELENGTH" },
        { Item.CameraQuantum,        "HAS_AP_ITEM_IMAGING_RULE" },
        { Item.WarpPlatformCodes,    "HAS_AP_ITEM_NOMAI_WARP_CODES" },
        { Item.WarpCoreManual,       "HAS_AP_ITEM_WARP_CORE_INSTALLATION_MANUAL" },
        { Item.EntanglementRule,     "HAS_AP_ITEM_ENTANGLEMENT_RULE" },
        { Item.ShrineDoorCodes,      "HAS_AP_ITEM_SHRINE_DOOR_CODES" },
        { Item.TornadoAdjustment,    "HAS_AP_ITEM_TORNADO_AERODYNAMIC_ADJUSTMENTS" },
        { Item.SilentRunning,        "HAS_AP_ITEM_SILENT_RUNNING_MODE" },
        { Item.ElectricalInsulation, "HAS_AP_ITEM_ELECTRICAL_INSULATION" },
        { Item.Coordinates,          "HAS_AP_ITEM_COORDINATES" },
        { Item.FrequencyDB,          "HAS_AP_ITEM_DISTRESS_BEACON_FREQUENCY" },
        { Item.FrequencyQF,          "HAS_AP_ITEM_QUANTUM_FLUCTUATIONS_FREQUENCY" },
        { Item.FrequencyHS,          "HAS_AP_ITEM_HIDE_AND_SEEK_FREQUENCY" },
        { Item.SignalFeldspar,       "HAS_AP_ITEM_FELDSPARS_SIGNAL" },
        { Item.SignalQM,             "HAS_AP_ITEM_QUANTUM_MOON_SIGNAL" },
        { Item.SignalEP3,            "HAS_AP_ITEM_ESCAPE_POD_3_SIGNAL" },
        { Item.FrequencyDSR,         "HAS_AP_ITEM_DEEP_SPACE_RADIO_FREQUENCY" },
        { Item.LightModulator,       "HAS_AP_ITEM_CONTROL_SENSOR_WAVELENGTH" },
        { Item.BreachOverrideCodes,  "HAS_AP_ITEM_BREACH_OVERRIDE_CODES" },
        { Item.RLPaintingCode,       "HAS_AP_ITEM_RIVER_LOWLANDS_PAINTING_CODE" },
        { Item.CIPaintingCode,       "HAS_AP_ITEM_CINDER_ISLES_PAINTING_CODE" },
        { Item.HGPaintingCode,       "HAS_AP_ITEM_HIDDEN_GORGE_PAINTING_CODE" },
        { Item.DreamTotemPatch,      "HAS_AP_ITEM_DREAM_TOTEM_PATCH" },
        { Item.RaftDocksPatch,       "HAS_AP_ITEM_RAFT_DOCKS_PATCH" },
        { Item.LimboWarpPatch,       "HAS_AP_ITEM_RAFT_LIMBO_PATCH" },
        { Item.ProjectionRangePatch, "HAS_AP_ITEM_PROJECTION_RANGE_PATCH" },
        { Item.AlarmBypassPatch,     "HAS_AP_ITEM_ALARM_BYPASS_PATCH" },
        // story mod progression items
        { Item.FrequencyNeighborDistress,      "HAS_AP_ITEM_HN1_NEIGHBORS_DISTRESS_SIGNAL_FREQUENCY" },
        { Item.FrequencyLavaCore,              "HAS_AP_ITEM_HN1_LAVA_CORE_SIGNALS_FREQUENCY" },
        { Item.FrequencyGalacticCommunication, "HAS_AP_ITEM_HN1_GALACTIC_COMMUNICATION_FREQUENCY" },
        { Item.FrequencyAstralCodec,           "HAS_AP_ITEM_AC_ASTRAL_CODEC_FREQUENCY" },
        { Item.MemoryCubeInterface,            "HAS_AP_ITEM_HN2_MEMORY_CUBE_INTERFACE" },
        { Item.MagistariumLibraryAccessCode,   "HAS_AP_ITEM_HN2_LIBRARY_ACCESS_CODE" },
        { Item.MagistariumDormitoryAccessCode, "HAS_AP_ITEM_HN2_DORMITORY_ACCESS_CODE" },
        { Item.MagistariumEngineAccessCode,    "HAS_AP_ITEM_HN2_ENGINE_ACCESS_CODE" },
        { Item.FrequencyHearthianRadio,        "HAS_AP_ITEM_FQ_HEARTHIAN_RADIO_FREQUENCY" },
    };

    // leave these as null until we load the ids, so any attempt to work with ids before that will fail loudly
    public static Dictionary<long, Item> archipelagoIdToItem = null;
    public static Dictionary<Item, long> itemToArchipelagoId = null;

    public static void LoadArchipelagoIds(string itemsFilepath)
    {
        var itemsData = JArray.Parse(File.ReadAllText(itemsFilepath));
        archipelagoIdToItem = new();
        itemToArchipelagoId = new();
        foreach (var itemData in itemsData)
        {
            // Skip event items, since they intentionally don't have ids
            if (itemData["code"].Type == JTokenType.Null) continue;

            var archipelagoId = (long)itemData["code"];
            var name = (string)itemData["name"];

            if (!itemNamesReversed.ContainsKey(name))
                throw new System.Exception($"LoadArchipelagoIds failed: unknown item name {name}");

            var item = itemNamesReversed[name];
            archipelagoIdToItem.Add(archipelagoId, item);
            itemToArchipelagoId.Add(item, archipelagoId);
        }
    }
}
