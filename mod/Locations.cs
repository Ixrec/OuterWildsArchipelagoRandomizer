using HarmonyLib;
using System.Collections.Generic;
using System.Linq;

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

        { "TT_TIME_LOOP_DEVICE_X1", "Ash Twin: Enter the Ash Twin Project" },

        // when we add flavor text, probably change this location to trigger on talking to Gossan after the repairs
        { "TH_ZERO_G_CAVE_X2", "TH: Do the Zero-G Cave Repairs" },
        { "TH_IMPACT_CRATER_X1", "TH: Bramble Seed Crater" },
        { "TH_NOMAI_MINE_X1", "TH: Nomai Mines (Text Wall)" },

        { "TM_EYE_LOCATOR_X2", "Attlerock: Eye Signal Locator (Text Wall)" },

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

        // Both text wheels provide similar information about the shuttle's mission, but
        // depending on whether you find it at Interloper or Ember Twin you can only
        // get one or the other fact, so I want both facts to map to this "location".
        { "COMET_SHUTTLE_X2", "Frozen Shuttle Log (Text Wheel)" },
        { "COMET_SHUTTLE_X3", "Frozen Shuttle Log (Text Wheel)" },
        { "COMET_INTERIOR_X4", "Ruptured Core (Text Wheel)" }, // spoiler-free name, as opposed to e.g. "Interloper Core"

        { "QUANTUM_MOON_X1", "Land on the Quantum Moon" },
        { "QM_SHUTTLE_X2", "Solanum's Shuttle Log (Text Wheel)" },
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
        { SignalName.HideAndSeek_Galena, "TH: Hide & Seek - Galena's Radio Signal" },
        { SignalName.HideAndSeek_Tephra, "TH: Hide & Seek - Tephra's Radio Signal" },
        // DLC will add: SignalName.RadioTower, SignalName.MapSatellite
        // leaving out Default, HideAndSeek_Arkose and all the White Hole signals because I don't believe they're used
        // leaving out Nomai and Prisoner because I believe those are only available during the finale
    };

    // these three locations have unique triggers

    static string launchCodesLocation = "TH: Learn the Launch Codes from Hornfels";

    static string enterShipLocation = "Enter Your Spaceship";

    // for now, this is the only location triggered by a conversation with no corresponding ship log
    static string translatorLocation = "TH: Get the Translator from Hal";

    static List<string> allLocationNames = logFactToLocation.Select(lftl => lftl.Value)
        .Concat(frequencyToLocation.Select(ftl => ftl.Value))
        .Concat(signalToLocation.Select(stl => stl.Value))
        .Append(launchCodesLocation).Append(enterShipLocation).Append(translatorLocation)
        .Distinct() // won't be necessary if we move to proper enums for item/location names
        .ToList();


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
        { "TH: Museum Shard Signal", "Museum Shard Signal" },
        { "TH: Grove Shard Signal", "Grove Shard Signal" },
        { "ET: Cave Shard Signal", "Cave Shard Signal" },
        { "BH: Tower Shard Signal", "Tower Shard Signal" },
        { "GD: Island Shard Signal", "Island Shard Signal" },
        { "Quantum Moon Signal", "Quantum Moon Signal" },
        { "BH: Escape Pod 1 Signal", "Escape Pod 1 Signal" },
        { "ET: Escape Pod 2 Signal", "Escape Pod 2 Signal" },
        { "DB: Escape Pod 3 Signal", "Escape Pod 3 Signal" },
        { "TH: Hide & Seek - Galena's Radio Signal", "Galena's Radio Signal" },
        { "TH: Hide & Seek - Tephra's Radio Signal", "Tephra's Radio Signal" },

        { "TH: Learn the Launch Codes from Hornfels", "Launch Codes" },
        { "Enter Your Spaceship", "Spaceship" }
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
            Randomizer.Instance.ModHelper.Console.WriteLine($"'{locationName}' has already been checked. Doing nothing.");
            return;
        }
        else
        {
            Randomizer.Instance.ModHelper.Console.WriteLine($"Marking '{locationName}' as checked");
            locationChecked[locationName] = true;

            var item = locationToVanillaItem.ContainsKey(locationName) ? locationToVanillaItem[locationName] : "Nothing";
            // todo: replace this with Archipelago integration
            Randomizer.Instance.ModHelper.Console.WriteLine($"Awarding item '{item}'");

            InGameConsole.Instance.AddNotification($"You found your <color=\"orange\">{item}</color>");

            switch (item)
            {
                case "Translator": Translator.SetHasTranslator(true); break;
                case "Signalscope": Signalscope.SetHasSignalscope(true); break;
                case "Spaceship": break; // Nothing to do for now. Making the ship an item is just planning ahead for random player/ship spawn.
                case "Scout Launcher": break; // todo
                case "Camera": break; // todo
                case "Nomai Warp Codes": WarpPlatforms.SetHasNomaiWarpCodes(true); break;
                case "Warp Core Installation Codes": WarpCoreInstallation.SetHasWarpCoreInstallationCodes(true); break;
                case "Rule of Quantum Imaging": QuantumImaging.SetHasImagingKnowledge(true); break;
                case "Rule of Quantum Entanglement": QuantumEntanglement.SetHasEntanglementKnowledge(true); break;
                case "Quantum Shrine Door Codes": QuantumShrineDoor.SetHasQuantumShrineCodes(true); break;
                case "Tornado Aerodynamic Adjustments": Tornadoes.SetHasTornadoKnowledge(true); break;
                case "Silent Running Mode": Anglerfish.SetHasAnglerfishKnowledge(true); break;
                case "Jellyfish Insulation": Jellyfish.SetHasJellyfishKnowledge(true); break;
                case "Coordinates": break; // todo

                // todo: can we disable the OW Ventures frequency?
                case "Distress Beacon Frequency": Signalscope.LearnFrequency(SignalFrequency.EscapePod); break;
                case "Quantum Fluctuations Frequency": Signalscope.LearnFrequency(SignalFrequency.Quantum); break;
                case "Hide & Seek Frequency": Signalscope.LearnFrequency(SignalFrequency.HideAndSeek); break;

                case "Chert's Drum Signal": Signalscope.LearnSignal(SignalName.Traveler_Chert); break;
                case "Esker's Whistling Signal": Signalscope.LearnSignal(SignalName.Traveler_Esker); break;
                case "Riebeck's Banjo Signal": Signalscope.LearnSignal(SignalName.Traveler_Riebeck); break;
                case "Gabbro's Flute Signal": Signalscope.LearnSignal(SignalName.Traveler_Gabbro); break;
                case "Feldspar's Harmonica Signal": Signalscope.LearnSignal(SignalName.Traveler_Feldspar); break;
                case "Museum Shard Signal": Signalscope.LearnSignal(SignalName.Quantum_TH_MuseumShard); break;
                case "Grove Shard Signal": Signalscope.LearnSignal(SignalName.Quantum_TH_GroveShard); break;
                case "Cave Shard Signal": Signalscope.LearnSignal(SignalName.Quantum_CT_Shard); break;
                case "Tower Shard Signal": Signalscope.LearnSignal(SignalName.Quantum_BH_Shard); break;
                case "Island Shard Signal": Signalscope.LearnSignal(SignalName.Quantum_GD_Shard); break;
                case "Quantum Moon Signal": Signalscope.LearnSignal(SignalName.Quantum_QM); break;
                case "Escape Pod 1 Signal": Signalscope.LearnSignal(SignalName.EscapePod_BH); break;
                case "Escape Pod 2 Signal": Signalscope.LearnSignal(SignalName.EscapePod_CT); break;
                case "Escape Pod 3 Signal": Signalscope.LearnSignal(SignalName.EscapePod_DB); break;
                case "Galena's Radio Signal": Signalscope.LearnSignal(SignalName.HideAndSeek_Galena); break;
                case "Tephra's Radio Signal": Signalscope.LearnSignal(SignalName.HideAndSeek_Tephra); break;
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
            CheckLocation(locationName);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerData), nameof(PlayerData.LearnLaunchCodes))]
    public static void PlayerData_LearnLaunchCodes_Prefix()
    {
        CheckLocation(launchCodesLocation);
    }
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerState), nameof(PlayerState.OnEnterShip))]
    public static void PlayerState_OnEnterShip_Prefix()
    {
        // not an important optimization, but a very easy one
        var firstTimeThisLoop = !PlayerState.HasPlayerEnteredShip();
        if (firstTimeThisLoop)
            CheckLocation(enterShipLocation);
    }
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CharacterDialogueTree), nameof(CharacterDialogueTree.EndConversation))]
    public static void CharacterDialogueTree_EndConversation_Prefix(CharacterDialogueTree __instance)
    {
        var dialogueTreeName = __instance._xmlCharacterDialogueAsset.name;
        Randomizer.Instance.ModHelper.Console.WriteLine($"CharacterDialogueTree.EndConversation {dialogueTreeName}");
        if (dialogueTreeName == "Hal_Museum" || dialogueTreeName == "Hal_Outside")
            CheckLocation(translatorLocation);
    }
}
