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
};

public static class ItemNames
{
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
    };

    public static Dictionary<string, Item> itemNamesReversed = itemNames.ToDictionary(itemName => itemName.Value, itemName => itemName.Key);

    public static string ItemToName(Item item) => itemNames[item];
    public static Item NameToItem(string itemName) => itemNamesReversed[itemName];

    public static Dictionary<SignalFrequency, Item> frequencyToItem = new Dictionary<SignalFrequency, Item>
    {
        { SignalFrequency.EscapePod, Item.FrequencyDB },
        { SignalFrequency.Quantum, Item.FrequencyQF },
        { SignalFrequency.HideAndSeek, Item.FrequencyHS },
    };
    public static Dictionary<Item, SignalFrequency> itemToFrequency = frequencyToItem.ToDictionary(fti => fti.Value, fti => fti.Key);

    public static Dictionary<SignalName, Item> signalToItem = new Dictionary<SignalName, Item>
    {
        { SignalName.Traveler_Chert, Item.SignalChert },
        { SignalName.Traveler_Esker, Item.SignalEsker },
        { SignalName.Traveler_Riebeck, Item.SignalRiebeck },
        { SignalName.Traveler_Gabbro, Item.SignalGabbro },
        { SignalName.Traveler_Feldspar, Item.SignalFeldspar },
        { SignalName.Quantum_TH_MuseumShard, Item.SignalMuseumShard },
        { SignalName.Quantum_TH_GroveShard, Item.SignalGroveShard },
        { SignalName.Quantum_CT_Shard, Item.SignalCaveShard },
        { SignalName.Quantum_BH_Shard, Item.SignalTowerShard },
        { SignalName.Quantum_GD_Shard, Item.SignalIslandShard },
        { SignalName.Quantum_QM, Item.SignalQM },
        { SignalName.EscapePod_BH, Item.SignalEP1 },
        { SignalName.EscapePod_CT, Item.SignalEP2 },
        { SignalName.EscapePod_DB, Item.SignalEP3 },
        { SignalName.HideAndSeek_Galena, Item.SignalGalena },
        { SignalName.HideAndSeek_Tephra, Item.SignalTephra },
    };
    public static Dictionary<Item, SignalName> itemToSignal = signalToItem.ToDictionary(sti => sti.Value, sti => sti.Key);

    // The OW autosplitter can be driven by persistent condition changes, so these enable autosplitting with the randomizer.
    // For now, we define a condition for every progression item.
    public static Dictionary<Item, string> itemToPersistentCondition = new Dictionary<Item, string>
    {
        { Item.LaunchCodes,          "HAS_AP_ITEM_LAUNCH_CODES" },
        { Item.Spacesuit,            "HAS_AP_ITEM_SPACESUIT" },
        { Item.Translator,           "HAS_AP_ITEM_TRANSLATOR" },
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
