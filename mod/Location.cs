using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ArchipelagoRandomizer;

public enum Location
{
    SPACESHIP,
    SS,
    ET_DRUM,
    ET_HEL,
    ET_SC_SHRINE,
    ET_QML,
    ET_FOSSIL,
    ET_LAKEBED_CAVE,
    ET_COLEUS_CAVE,
    ET_SHARD_SIGNAL,
    ET_EP2_SIGNAL,
    AT_ATP,
    TH_GM,
    TH_ZERO_G,
    TH_HAL,
    TH_HORNFELS,
    TH_SEED_CRATER,
    TH_MINES,
    TH_MS_SIGNAL,
    TH_GS_SIGNAL,
    TH_GALENA_SIGNAL,
    TH_TEPHRA_SIGNAL,
    AR_WHISTLE,
    AR_ESL,
    BH_BANJO,
    BH_OBSERVATORY,
    BH_OS_MURAL,
    BH_FORGE,
    BH_TOWER,
    BH_SHARD_SIGNAL,
    BH_EP1_SIGNAL,
    HL_VTS,
    WHS,
    OPC_ENTER,
    OPC_CM,
    GD_FLUTE,
    GD_BI,
    GD_CY,
    GD_SIW,
    GD_DEPTHS,
    GD_CORE,
    GD_TOWER_RULE,
    GD_TOWER_COMPLETE,
    GD_COORDINATES,
    GD_SHARD_SIGNAL,
    FROZEN_SHUTTLE,
    IL_CORE,
    SOLANUM_SHUTTLE,
    QM_LAND,
    QM_6L,
    QM_SIGNAL,
    DB_HARMONICA,
    DB_JELLY,
    DB_GRAVE,
    DB_VESSEL,
    DB_EP3_SIGNAL,
    FREQ_DISTRESS,
    FREQ_QUANTUM,
    FREQ_HIDE_SEEK,
};

public static class LocationNames
{
    public static Dictionary<Location, string> locationNames = new Dictionary<Location, string> {
        { Location.SPACESHIP, "Enter Your Spaceship" },

        { Location.SS, "Sun Station (Projection Stone Text)" },

        { Location.ET_HEL, "ET: High Energy Lab (Upper Text Wall)" },
        { Location.ET_SC_SHRINE, "ET: Sunless City Shrine (Entrance Text Wall)" },
        { Location.ET_QML, "ET: Quantum Moon Locator (Text Scroll)" },
        { Location.ET_FOSSIL, "ET: Fossil (Children's Text)" },
        { Location.ET_LAKEBED_CAVE, "ET: Lakebed Cave (Floor Text)" },
        { Location.ET_COLEUS_CAVE, "ET: Coleus' Cave (Text Wall)" },

        { Location.AT_ATP, "Enter the Ash Twin Project" },

        { Location.TH_GM, "TH: Ghost Matter Plaque" },
        { Location.TH_ZERO_G, "TH: Zero-G Repairs" },
        { Location.TH_HAL, "TH: Get the Translator from Hal" },
        { Location.TH_HORNFELS, "TH: Talk to Hornfels" },
        { Location.TH_SEED_CRATER, "TH: Talk to Tektite about Bramble Seed" },
        { Location.TH_MINES, "TH: Mines (Text Wall)" },

        { Location.AR_ESL, "AR: Signal Locator (Text Wall)" },

        { Location.BH_OBSERVATORY, "BH: Southern Observatory (Tornado Text Wall)" },
        { Location.BH_OS_MURAL, "BH: Old Settlement Murals" },
        { Location.BH_FORGE, "BH: Forge (2nd Scroll)" },
        { Location.BH_TOWER, "BH: Tower (Top Floor Text Wall)" },

        { Location.HL_VTS, "Volcanic Testing Site (Text Wall)" },

        { Location.WHS, "WHS (Text Wall)" },

        { Location.OPC_ENTER, "GD: Enter the Orbital Probe Cannon" },
        { Location.OPC_CM, "GD: Control Module Logs (Text Wheels)" },
        { Location.GD_BI, "GD: Bramble Island (Tape Recorder)" },
        { Location.GD_CY, "GD: Construction Yard (Text Wall)" },
        { Location.GD_SIW, "GD: Statue Island Workshop (Text Wheel)" },
        { Location.GD_DEPTHS, "GD: Enter the Ocean Depths" },
        { Location.GD_CORE, "GD: Enter the Core" },
        { Location.GD_TOWER_RULE, "GD: Tower Rule (Pedestal Text)" },
        { Location.GD_TOWER_COMPLETE, "GD: Complete the Tower (Text Wall)" },
        { Location.GD_COORDINATES, "GD: See the Coordinates" }, // spoiler-free name, as opposed to e.g. "Eye of the Universe Coordinates"

        { Location.FROZEN_SHUTTLE, "Frozen Shuttle Log (Text Wheel)" },
        { Location.IL_CORE, "Ruptured Core (Text Wheel)" }, // spoiler-free name, as opposed to e.g. "Interloper Core"

        { Location.QM_LAND, "QM: Land" },
        { Location.SOLANUM_SHUTTLE, "Solanum's Shuttle Log (Text Wheel)" },
        { Location.QM_6L, "QM: Explore the Sixth Location" }, // spoiler-free name, as opposed to e.g. "Meet Solanum"

        { Location.DB_JELLY, "DB: Feldspar's Note" }, // spoiler-free name, as opposed to e.g. "Frozen Jellyfish Note"
        { Location.DB_GRAVE, "DB: Nomai Grave (Text Wheel)" },
        { Location.DB_VESSEL, "DB: Find The Vessel" },

        { Location.FREQ_DISTRESS, "Distress Beacon Frequency" },
        { Location.FREQ_QUANTUM, "Quantum Fluctuations Frequency" },
        { Location.FREQ_HIDE_SEEK, "Hide & Seek Frequency" },

        { Location.ET_DRUM, "ET: Drum Signal" },
        { Location.AR_WHISTLE, "AR: Whistling Signal" },
        { Location.BH_BANJO, "BH: Banjo Signal" },
        { Location.GD_FLUTE, "GD: Flute Signal" },
        { Location.DB_HARMONICA, "DB: Harmonica Signal" },
        { Location.TH_MS_SIGNAL, "TH: Museum Shard Signal" },
        { Location.TH_GS_SIGNAL, "TH: Grove Shard Signal" },
        { Location.ET_SHARD_SIGNAL, "ET: Cave Shard Signal" },
        { Location.BH_SHARD_SIGNAL, "BH: Tower Shard Signal" },
        { Location.GD_SHARD_SIGNAL, "GD: Island Shard Signal" },
        { Location.QM_SIGNAL, "Quantum Moon Signal" },
        { Location.BH_EP1_SIGNAL, "BH: Escape Pod 1 Signal" },
        { Location.ET_EP2_SIGNAL, "ET: Escape Pod 2 Signal" },
        { Location.DB_EP3_SIGNAL, "DB: Escape Pod 3 Signal" },
        { Location.TH_GALENA_SIGNAL, "TH: Galena's Radio Signal" },
        { Location.TH_TEPHRA_SIGNAL, "TH: Tephra's Radio Signal" },
    };

    public static Dictionary<string, Location> locationNamesReversed = locationNames.ToDictionary(ln => ln.Value, ln => ln.Key);

    public static string LocationToName(Location location) => locationNames[location];
    public static Location NameToLocation(string locationName) => locationNamesReversed[locationName];

    public static Dictionary<SignalFrequency, Location> frequencyToLocation = new Dictionary<SignalFrequency, Location>{
        { SignalFrequency.EscapePod, Location.FREQ_DISTRESS },
        { SignalFrequency.Quantum, Location.FREQ_QUANTUM },
        { SignalFrequency.HideAndSeek, Location.FREQ_HIDE_SEEK },
        // DLC will add: SignalFrequency.Radio
        // left out Default, WarpCore and Statue because I don't believe they get used
    };
    public static Dictionary<Location, SignalFrequency> locationToFrequency = frequencyToLocation.ToDictionary(ftl => ftl.Value, ftl => ftl.Key);

    public static Dictionary<SignalName, Location> signalToLocation = new Dictionary<SignalName, Location>{
        { SignalName.Traveler_Chert, Location.ET_DRUM },
        { SignalName.Traveler_Esker, Location.AR_WHISTLE },
        { SignalName.Traveler_Riebeck, Location.BH_BANJO },
        { SignalName.Traveler_Gabbro, Location.GD_FLUTE },
        { SignalName.Traveler_Feldspar, Location.DB_HARMONICA },
        { SignalName.Quantum_TH_MuseumShard, Location.TH_MS_SIGNAL },
        { SignalName.Quantum_TH_GroveShard, Location.TH_GS_SIGNAL },
        { SignalName.Quantum_CT_Shard, Location.ET_SHARD_SIGNAL },
        { SignalName.Quantum_BH_Shard, Location.BH_SHARD_SIGNAL },
        { SignalName.Quantum_GD_Shard, Location.GD_SHARD_SIGNAL },
        { SignalName.Quantum_QM, Location.QM_SIGNAL },
        { SignalName.EscapePod_BH, Location.BH_EP1_SIGNAL },
        { SignalName.EscapePod_CT, Location.ET_EP2_SIGNAL },
        { SignalName.EscapePod_DB, Location.DB_EP3_SIGNAL },
        { SignalName.HideAndSeek_Galena, Location.TH_GALENA_SIGNAL },
        { SignalName.HideAndSeek_Tephra, Location.TH_TEPHRA_SIGNAL },
        // DLC will add: SignalName.RadioTower, SignalName.MapSatellite
        // left out Default, HideAndSeek_Arkose and all the White Hole signals because I don't believe they're used
        // left out Nomai and Prisoner because I believe those are only available during the finale
    };
    public static Dictionary<Location, SignalName> locationToSignal = signalToLocation.ToDictionary(stl => stl.Value, stl => stl.Key);

    // leave these as null until we load the ids, so any attempt to work with ids before that will fail loudly
    public static Dictionary<long, Location> archipelagoIdToLocation = null;
    public static Dictionary<Location, long> locationToArchipelagoId = null;

    public static void LoadArchipelagoIds(string locationsFilepath)
    {
        var locationsData = JArray.Parse(File.ReadAllText(locationsFilepath));
        archipelagoIdToLocation = new();
        locationToArchipelagoId = new();
        foreach (var locationData in locationsData)
        {
            // Skip event locations, since they intentionally don't have ids
            if (locationData["address"].Type == JTokenType.Null) continue;

            var archipelagoId = (long)locationData["address"];
            var name = (string)locationData["name"];

            if (!locationNamesReversed.ContainsKey(name))
                throw new System.Exception($"LoadArchipelagoIds failed: unknown location name {name}");

            var location = locationNamesReversed[name];
            archipelagoIdToLocation.Add(archipelagoId, location);
            locationToArchipelagoId.Add(location, archipelagoId);
        }
    }
};
