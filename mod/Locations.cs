using HarmonyLib;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using static NomaiWarpPlatform;
using UnityEngine.UIElements;
using UnityEngine;
using Delaunay;
using NAudio.CoreAudioApi;
using static System.Net.WebRequestMethods;
using static RumbleManager;
using System;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class Locations
{
    static Dictionary<string, string> logFactToLocation = new Dictionary<string, string>{
        { "S_SUNSTATION_X2", "Sun Station (Projection Stone Text)" },
        { "CT_HIGH_ENERGY_LAB_X3", "ET: High Energy Lab (Lower Text Wall)" },
        { "CT_SUNLESS_CITY_X3", "ET: Sunless City Eye Shrine (Entrance Text Wall)" },
        { "CT_QUANTUM_MOON_LOCATOR_X2", "ET: Quantum Moon Locator (Text Scroll)" },
        { "CT_ANGLERFISH_FOSSIL_X2", "ET: Anglerfish Fossil (Children's Text)" },
        { "CT_LAKEBED_CAVERN_X1", "ET: Lakebed Cave (Floor Text)" },
        { "CT_LAKEBED_CAVERN_X3", "ET: Quantum Caves (Trapped Coleus' Wall Text)" },

        // Both text wheels provide similar information about the shuttle's mission, but
        // depending on whether you find it at Interloper or Ember Twin you can only
        // get one or the other fact, so I want both facts to map to this "location".
        { "COMET_SHUTTLE_X2", "Frozen Shuttle Log (Text Wheel)" },
        { "COMET_SHUTTLE_X3", "Frozen Shuttle Log (Text Wheel)" },

        { "TT_TIME_LOOP_DEVICE_X1", "Ash Twin: Enter the Ash Twin Project" },

        // does this fact trigger when talking to Gossan, or do we need an end convo trigger instead?
        { "TH_ZERO_G_CAVE_X2", "TH: Do the Zero-G Cave Repairs" },

        { "TH_IMPACT_CRATER_X1", "TH: Bramble Seed Crater" },
        { "TH_NOMAI_MINE_X1", "TH: Nomai Mines (Text Wall)" },

        { "TM_EYE_LOCATOR_X2", "Attlerock: Eye Signal Locator (Text Wall)" },

        { "QM_SHUTTLE_X2", "Solanum's Shuttle Log (Text Wheel)" },
        { "BH_TORNADO_SIMULATION_X2", "BH: Southern Observatory (Text Wall)" },
        { "BH_MURAL_2_X1", "BH: Old Settlement Murals" },
        { "BH_BLACK_HOLE_FORGE_X6", "BH: Black Hole Forge (2nd Scroll)" },
        { "BH_QUANTUM_RESEARCH_TOWER_X1", "BH: Tower of Quantum Knowledge (Top Floor Text Wall)" },

        { "VM_VOLCANO_X3", "Volcanic Testing Site (Text Wall)" },

        { "WHS_X4", "White Hole Station (Text Wall)" },

        { "ORBITAL_PROBE_CANNON_X1", "GD: Enter the Orbital Probe Cannon" },
        { "OPC_INTACT_MODULE_X2", "GD: Control Module Logs (Text Wheels)" },
        { "GD_OCEAN_R3", "GD: Bramble Island Log" },
        { "GD_CONSTRUCTION_YARD_X1", "GD: Construction Yard (Text Wall)" },
        { "GD_STATUE_WORKSHOP_X1", "GD: Statue Island Workshop" },
        { "GD_OCEAN_X1", "GD: Enter the Ocean Depths" },
        { "GD_OCEAN_X2", "GD: Enter the Core" },
        { "GD_QUANTUM_TOWER_X2", "GD: Tower of Quantum Trials Rule Pedestal" },
        { "GD_QUANTUM_TOWER_X4", "GD: Complete the Tower of Quantum Trials" },
        { "OPC_EYE_COORDINATES_X1", "GD: Probe Tracking Module Coordinates" }, // spoiler-free name, as opposed to e.g. "Eye of the Universe Coordinates"

        { "COMET_INTERIOR_X4", "Interloper Core (Text Wheel)" },

        { "QUANTUM_MOON_X1", "Land on the Quantum Moon" },
        { "QM_SIXTH_LOCATION_X1", "Explore the Sixth Location" }, // spoiler-free name, as opposed to e.g. "Meet Solanum"

        { "DB_FROZEN_JELLYFISH_X3", "DB: Frozen Jellyfish Note" },
        { "DB_NOMAI_GRAVE_X3", "DB: Nomai Grave (Text Wheel)" },
        { "DB_VESSEL_X1", "DB: Find The Vessel" },
    };

    static Dictionary<SignalFrequency, string> frequencyToLocation = new Dictionary<SignalFrequency, string>{
        { SignalFrequency.EscapePod, "Distress Beacon Frequency" },
        { SignalFrequency.Quantum, "Quantum Fluctuations Frequency" },
        { SignalFrequency.HideAndSeek, "Hide & Seek Frequency" },
        // DLC will add: SignalFrequency.Radio
        // leaving out Default, WarpCore and Statue because I don't believe they get used
    };

    static Dictionary<SignalName, string> signalToLocation = new Dictionary<SignalName, string>{
        { SignalName.Traveler_Chert, "ET: Chert's Drum Signal" },
        { SignalName.Traveler_Esker, "Attlerock: Esker's Whistling Signal" },
        { SignalName.Traveler_Riebeck, "BH: Riebeck's Banjo Signal" },
        { SignalName.Traveler_Gabbro, "GD: Gabbro's Flute Signal" },
        { SignalName.Traveler_Feldspar, "DB: Feldspar's Harmonica Signal" },
        { SignalName.Quantum_TH_MuseumShard, "TH: Museum Shard Signal" },
        { SignalName.Quantum_TH_GroveShard, "TH: Grove Shard Signal" },
        { SignalName.Quantum_CT_Shard, "ET: Cave Shard Signal" },
        { SignalName.Quantum_BH_Shard, "BH: Tower Shard Signal" },
        { SignalName.Quantum_GD_Shard, "GD: Island Shard Signal" },
        { SignalName.Quantum_QM, "Quantum Moon Signal" },
        { SignalName.EscapePod_BH, "BH: Escape Pod 1 Signal" },
        { SignalName.EscapePod_CT, "ET: Escape Pod 2 Signal" },
        { SignalName.EscapePod_DB, "DB: Escape Pod 3 Signal" },
        { SignalName.HideAndSeek_Galena, "TH: Hidden Galena Signal" },
        { SignalName.HideAndSeek_Tephra, "TH: Hidden Tephra Signal" },
        // DLC will add: SignalName.RadioTower, SignalName.MapSatellite
        // leaving out Default, HideAndSeek_Arkose and all the White Hole signals because I don't believe they're used
        // leaving out Nomai and Prisoner because I believe those are only available during the finale
    };

    static string launchCodesLocation = "TH: Learn the Launch Codes from Hornfels";



    static List<string> allLocationNames = logFactToLocation.Select(lftl => lftl.Value)
        .Concat(frequencyToLocation.Select(ftl => ftl.Value))
        .Concat(signalToLocation.Select(stl => stl.Value))
        .Append(launchCodesLocation)
        .Distinct() // won't be necessary if we move to proper enums for item/location names
        .ToList();

    /* locations that don't fit the formats above:
        // "TH: Get the Translator from Hal" - Translator - none // forgot about conversation triggers
            // EndConversation Hal_Museum OR EndConversation Hal_Outside
        //"TH: Play Hide and Seek" - Hide and Seek Frequency - Signalscope
        //"Enter the Ship for the first time" - Spaceship - Launch Codes
    */



    // TODO: save state management
    static Dictionary<string, bool> locationChecked = allLocationNames
        .ToDictionary(ln => ln, _ => false);

    // TODO: actual randomization
    // for now, anything not in this map awards 'Nothing'
    static Dictionary<string, string> locationToVanillaItem = new Dictionary<string, string> {
        { "ET: Anglerfish Fossil (Children's Text)", "Silent Running Mode" },
        { "ET: Lakebed Cave (Floor Text)", "Rule of Quantum Entanglement" },
        { "TH: Do the Zero-G Cave Repairs", "Signalscope" }, // todo: make this trigger after talking to Gossan *and* the "fact" is revealed
        { "TH: Get the Translator from Hal", "Translator" },
        { "TH: Bramble Seed Crater", "Scout Launcher" },
        { "BH: Southern Observatory (Text Wall)", "Tornado Aerodynamic Adjustments" },
        { "BH: Black Hole Forge (2nd Scroll)", "Warp Core Installation Codes" },
        { "BH: Tower of Quantum Knowledge (Top Floor Text Wall)", "Quantum Shrine Door Codes" },
        { "White Hole Station (Text Wall)", "Nomai Warp Codes" },
        { "GD: Tower of Quantum Trials Rule Pedestal", "Rule of Quantum Imaging" },
        { "GD: Probe Tracking Module Coordinates", "Coordinates" },
        { "DB: Frozen Jellyfish Note", "Jellyfish Insulation" },

        { "Distress Beacon Frequency", "Distress Beacon Frequency" },
        { "Quantum Fluctuations Frequency", "Quantum Fluctuations Frequency" },
        { "Hide & Seek Frequency", "Hide & Seek Frequency" },

        { "ET: Chert's Drum Signal", "Chert's Drum Signal" },
        { "Attlerock: Esker's Whistling Signal", "Esker's Whistling Signal" },
        { "BH: Riebeck's Banjo Signal", "Riebeck's Banjo Signal" },
        { "GD: Gabbro's Flute Signal", "Gabbro's Flute Signal" },
        { "DB: Feldspar's Harmonica Signal", "Feldspar's Harmonica Signal" },
        //{ "TH: Museum Shard Signal", "Museum Shard Signal" },
        { "TH: Museum Shard Signal", "Grove Shard Signal" }, // testing
        { "TH: Grove Shard Signal", "Museum Shard Signal" },
        { "ET: Cave Shard Signal", "Cave Shard Signal" },
        { "BH: Tower Shard Signal", "Tower Shard Signal" },
        { "GD: Island Shard Signal", "Island Shard Signal" },
        { "Quantum Moon Signal", "Quantum Moon Signal" },
        { "BH: Escape Pod 1 Signal", "Escape Pod 1 Signal" },
        { "ET: Escape Pod 2 Signal", "Escape Pod 2 Signal" },
        { "DB: Escape Pod 3 Signal", "Escape Pod 3 Signal" },
        { "TH: Hidden Galena Signal", "Hidden Galena Signal" },
        { "TH: Hidden Tephra Signal", "Hidden Tephra Signal" },

        { "TH: Learn the Launch Codes from Hornfels", "Launch Codes" },
    };

    public static void CheckLocation(string locationName)
    {
        if (!locationChecked.ContainsKey(locationName))
        {
            Randomizer.Instance.ModHelper.Console.WriteLine($"'{locationName}' missing from locationChecked dictionary", OWML.Common.MessageType.Error);
            return;
        }

        if (locationChecked[locationName])
        {
            // some location triggers like LearnFrequency/Signal can potentially get called every update, so this is far too spammy to log by default
            // Randomizer.Instance.ModHelper.Console.WriteLine($"'{locationName}' has already been checked. Doing nothing.");
            return;
        }
        else
        {
            Randomizer.Instance.ModHelper.Console.WriteLine($"Marking '{locationName}' as checked");
            locationChecked[locationName] = true;

            if (!locationToVanillaItem.ContainsKey(locationName))
            {
                Randomizer.Instance.ModHelper.Console.WriteLine($"'{locationName}' missing from locationToVanillaItem dictionary", OWML.Common.MessageType.Error);
                return;
            }

            var item = locationToVanillaItem[locationName] ?? "Nothing";
            // todo: replace this with Archipelago integration
            // todo: make an in-game console for this message
            Randomizer.Instance.ModHelper.Console.WriteLine($"Awarding item '{item}'");
            switch (item)
            {
                case "Translator": break; // todo
                case "Signalscope": Signalscope.SetHasSignalscope(true); break;
                case "Scout Launcher": break; // todo
                case "Camera": break; // todo
                case "Nomai Warp Codes": WarpPlatforms.SetHasNomaiWarpCodes(true); break;
                case "Warp Core Installation Codes": break; // todo
                case "Rule of Quantum Imaging": QuantumImaging.SetHasImagingKnowledge(true); break;
                case "Rule of Quantum Entanglement": QuantumEntanglement.SetHasEntanglementKnowledge(true); break;
                case "Quantum Shrine Door Codes": break; // todo
                case "Tornado Aerodynamic Adjustments": Tornadoes.SetHasTornadoKnowledge(true); break;
                case "Silent Running Mode": Anglerfish.SetHasAnglerfishKnowledge(true); break;
                case "Jellyfish Insulation": Jellyfish.SetHasJellyfishKnowledge(true); break;
                case "Coordinates": break; // todo

                case "Distress Beacon Frequency": Signalscope.LearnFrequency(SignalFrequency.EscapePod); break;
                case "Quantum Fluctuations Frequency": Signalscope.LearnFrequency(SignalFrequency.Quantum); break;
                case "Hide & Seek Frequency": Signalscope.LearnFrequency(SignalFrequency.HideAndSeek); break;

                case "Grove Shard Signal": Signalscope.LearnSignal(SignalName.Quantum_TH_GroveShard); break;
                    // todo: all the signals
                default: break;
            }
        }
    }


    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipLogManager), nameof(ShipLogManager.RevealFact))]
    public static void ShipLogManager_RevealFact_Prefix(string id, bool saveGame, bool showNotification)
    {
        var factId = id;
        Randomizer.Instance.ModHelper.Console.WriteLine($"ShipLogManager.RevealFact {factId} {saveGame} {showNotification}");
        if (logFactToLocation.ContainsKey(factId))
        {
            var locationName = logFactToLocation[factId];
            Randomizer.Instance.ModHelper.Console.WriteLine($"ShipLogManager.RevealFact(\"{factId}\", ...) matched trigger for location '{locationName}'");
            CheckLocation(locationName);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerData), nameof(PlayerData.LearnFrequency))]
    public static void PlayerData_LearnFrequency_Prefix(SignalFrequency frequency)
    {
        if (frequencyToLocation.ContainsKey(frequency))
        {
            var locationName = frequencyToLocation[frequency];
            // since this gets called every update when you're scanning a signal source, it's too spammy to log by default
            // Randomizer.Instance.ModHelper.Console.WriteLine($"PlayerData.LearnFrequency({frequency}, ...) matched trigger for location '{locationName}'");
            CheckLocation(locationName);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerData), nameof(PlayerData.LearnSignal))]
    public static void PlayerData_LearnSignal_Prefix(SignalName signalName)
    {
        if (signalToLocation.ContainsKey(signalName))
        {
            var locationName = signalToLocation[signalName];
            // since this gets called every update when you're scanning a signal source, it's too spammy to log by default
            // Randomizer.Instance.ModHelper.Console.WriteLine($"PlayerData.LearnSignal({signalName}, ...) matched trigger for location '{locationName}'");
            CheckLocation(locationName);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerData), nameof(PlayerData.LearnLaunchCodes))]
    public static void PlayerData_LearnLaunchCodes_Prefix()
    {
        Randomizer.Instance.ModHelper.Console.WriteLine($"PlayerData.LearnLaunchCodes");
        CheckLocation(launchCodesLocation);
    }
}
