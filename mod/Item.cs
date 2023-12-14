using System.Collections.Generic;
using System.Linq;

namespace ArchipelagoRandomizer;

public enum Item
{
    LaunchCodes,
    Spaceship,

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
};

public static class ItemNames
{
    public static Dictionary<Item, string> itemNames = new Dictionary<Item, string> {
        { Item.LaunchCodes, "Launch Codes" },
        { Item.Spaceship, "Spaceship" },

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
    };

    public static Dictionary<string, Item> itemNamesReversed = itemNames.ToDictionary(ln => ln.Value, ln => ln.Key);

    public static string ItemToName(Item item) => itemNames[item];
    public static Item NameToItem(string itemName) => itemNamesReversed[itemName];
}
