using HarmonyLib;
using System.Collections.Generic;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class LocationTriggers
{
    static Dictionary<string, Location> logFactToLocation = new Dictionary<string, Location>{
        { "S_SUNSTATION_X2", Location.SS },

        { "CT_HIGH_ENERGY_LAB_X3", Location.ET_HEL },
        { "CT_SUNLESS_CITY_X3", Location.ET_SC_SHRINE },
        { "QM_SIXTH_LOCATION_R1", Location.ET_QML },
        { "CT_ANGLERFISH_FOSSIL_X2", Location.ET_FOSSIL },
        { "CT_LAKEBED_CAVERN_X1", Location.ET_LAKEBED_CAVE },
        { "CT_LAKEBED_CAVERN_X3", Location.ET_COLEUS_CAVE },

        { "TT_TIME_LOOP_DEVICE_X1", Location.AT_ATP },

        // when we add flavor text, probably change this location to trigger on talking to Gossan after the repairs
        { "TH_ZERO_G_CAVE_X2", Location.TH_ZERO_G },
        { "TH_IMPACT_CRATER_X1", Location.TH_SEED_CRATER },
        { "TH_NOMAI_MINE_X1", Location.TH_MINES },

        { "TM_EYE_LOCATOR_X2", Location.AR_ESL },

        { "BH_TORNADO_SIMULATION_X2", Location.BH_OBSERVATORY },
        { "BH_MURAL_2_X1", Location.BH_OS_MURAL },
        { "BH_BLACK_HOLE_FORGE_X6", Location.BH_FORGE },
        { "BH_QUANTUM_RESEARCH_TOWER_X1", Location.BH_TOWER },

        { "VM_VOLCANO_X3", Location.HL_VTS },

        { "WHS_X4", Location.WHS },

        { "ORBITAL_PROBE_CANNON_X1", Location.OPC_ENTER },
        { "OPC_INTACT_MODULE_X2", Location.OPC_CM },
        { "GD_OCEAN_R3", Location.GD_BI },
        { "GD_CONSTRUCTION_YARD_X1", Location.GD_CY },
        { "GD_STATUE_WORKSHOP_X1", Location.GD_SIW },
        { "GD_OCEAN_X1", Location.GD_DEPTHS },
        { "GD_OCEAN_X2", Location.GD_CORE },
        { "GD_QUANTUM_TOWER_X2", Location.GD_TOWER_RULE },
        { "GD_QUANTUM_TOWER_X4", Location.GD_TOWER_COMPLETE },
        { "OPC_EYE_COORDINATES_X1", Location.GD_COORDINATES },

        // Both text wheels provide similar information about the shuttle's mission, but
        // depending on whether you find it at Interloper or Ember Twin you can only
        // get one or the other fact, so I want both facts to map to this "location".
        { "COMET_SHUTTLE_X2", Location.FROZEN_SHUTTLE },
        { "COMET_SHUTTLE_X3", Location.FROZEN_SHUTTLE },
        { "COMET_INTERIOR_X4", Location.IL_CORE },

        { "QUANTUM_MOON_X1", Location.QM_LAND },
        { "QM_SHUTTLE_X2", Location.SOLANUM_SHUTTLE },
        { "QM_SIXTH_LOCATION_X1", Location.QM_6L },

        { "DB_FROZEN_JELLYFISH_X3", Location.DB_JELLY },
        { "DB_NOMAI_GRAVE_X3", Location.DB_GRAVE },
        { "DB_VESSEL_X1", Location.DB_VESSEL },
    };

    static Dictionary<SignalFrequency, Location> frequencyToLocation = new Dictionary<SignalFrequency, Location>{
        { SignalFrequency.EscapePod, Location.FREQ_DISTRESS },
        { SignalFrequency.Quantum, Location.FREQ_QUANTUM },
        { SignalFrequency.HideAndSeek, Location.FREQ_HIDE_SEEK },
        // DLC will add: SignalFrequency.Radio
        // leaving out Default, WarpCore and Statue because I don't believe they get used
    };

    static Dictionary<SignalName, Location> signalToLocation = new Dictionary<SignalName, Location>{
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
        // leaving out Default, HideAndSeek_Arkose and all the White Hole signals because I don't believe they're used
        // leaving out Nomai and Prisoner because I believe those are only available during the finale
    };

    // TODO: actual randomization
    // for now, anything not in this map awards 'Nothing'
    static Dictionary<Location, Item> locationToVanillaItem = new Dictionary<Location, Item> {
        { Location.ET_FOSSIL, Item.SilentRunning },
        { Location.ET_LAKEBED_CAVE, Item.EntanglementRule },
        { Location.TH_GM, Item.CameraGM },
        { Location.TH_ZERO_G, Item.Signalscope },
        { Location.TH_HAL, Item.Translator },
        { Location.TH_SEED_CRATER, Item.Scout },
        { Location.BH_OBSERVATORY, Item.TornadoAdjustment },
        { Location.BH_FORGE, Item.WarpCoreManual },
        { Location.BH_TOWER, Item.ShrineDoorCodes },
        { Location.WHS, Item.WarpPlatformCodes },
        { Location.GD_TOWER_RULE, Item.CameraQuantum },
        { Location.GD_COORDINATES, Item.Coordinates },
        { Location.DB_JELLY, Item.ElectricalInsulation },

        { Location.FREQ_DISTRESS, Item.FrequencyDB },
        { Location.FREQ_QUANTUM, Item.FrequencyQF },
        { Location.FREQ_HIDE_SEEK, Item.FrequencyHS },

        { Location.ET_DRUM, Item.SignalChert },
        { Location.AR_WHISTLE, Item.SignalEsker },
        { Location.BH_BANJO, Item.SignalRiebeck },
        { Location.GD_FLUTE, Item.SignalGabbro },
        { Location.DB_HARMONICA, Item.SignalFeldspar },
        { Location.TH_MS_SIGNAL, Item.SignalMuseumShard },
        { Location.TH_GS_SIGNAL, Item.SignalGroveShard },
        { Location.ET_SHARD_SIGNAL, Item.SignalCaveShard },
        { Location.BH_SHARD_SIGNAL, Item.SignalTowerShard },
        { Location.GD_SHARD_SIGNAL, Item.SignalIslandShard },
        { Location.QM_SIGNAL, Item.SignalQM },
        { Location.BH_EP1_SIGNAL, Item.SignalEP1 },
        { Location.ET_EP2_SIGNAL, Item.SignalEP2 },
        { Location.DB_EP3_SIGNAL, Item.SignalEP3 },
        { Location.TH_GALENA_SIGNAL, Item.SignalGalena },
        { Location.TH_TEPHRA_SIGNAL, Item.SignalTephra },

        { Location.TH_HORNFELS, Item.LaunchCodes },
        { Location.SPACESHIP, Item.Spaceship }
    };

    public static void CheckLocation(Location location)
    {
        var locationChecked = Randomizer.SaveData.locationsChecked;
        if (!locationChecked.ContainsKey(location))
        {
            Randomizer.Instance.ModHelper.Console.WriteLine($"'{location}' missing from locationChecked dictionary", OWML.Common.MessageType.Error);
            return;
        }

        if (locationChecked[location])
        {
            Randomizer.Instance.ModHelper.Console.WriteLine($"'{location}' has already been checked. Doing nothing.");
            return;
        }
        else
        {
            Randomizer.Instance.ModHelper.Console.WriteLine($"Marking '{location}' as checked");
            locationChecked[location] = true;

            var item = locationToVanillaItem.ContainsKey(location) ? locationToVanillaItem[location] : Item.Nothing;
            // todo: replace this with Archipelago integration
            Randomizer.Instance.ModHelper.Console.WriteLine($"Awarding item {item}");

            var itemName = ItemNames.ItemToName(item);
            InGameConsole.Instance.AddNotification($"You found your <color=\"orange\">{itemName}</color>");

            Randomizer.SaveData.itemsAcquired[item] += 1;

            ApplyItemToPlayer(item, Randomizer.SaveData.itemsAcquired[item]);

            Randomizer.Instance.WriteToSaveFile();
        }
    }

    public static void ApplyItemToPlayer(Item item, uint count)
    {
        switch (item)
        {
            case Item.LaunchCodes: break; // Not necessary until launch codes can be shuffled, and it's surprisingly subtle to set them without crashing.
            case Item.Spaceship: break; // Nothing to do for now. Making the ship an item is just planning ahead for random player/ship spawn.

            case Item.Translator: Translator.SetHasTranslator(count > 0); break;
            case Item.Signalscope: Signalscope.SetHasSignalscope(count > 0); break;
            case Item.Scout: Scout.SetHasScout(count > 0); break;
            case Item.CameraGM: GhostMatter.SetHasGhostMatterKnowledge(count > 0); break;
            case Item.CameraQuantum: QuantumImaging.SetHasImagingKnowledge(count > 0); break;
            case Item.WarpPlatformCodes: WarpPlatforms.SetHasNomaiWarpCodes(count > 0); break;
            case Item.WarpCoreManual: WarpCoreManual.SetHasWarpCoreManual(count > 0); break;
            case Item.EntanglementRule: QuantumEntanglement.SetHasEntanglementKnowledge(count > 0); break;
            case Item.ShrineDoorCodes: QuantumShrineDoor.SetHasQuantumShrineCodes(count > 0); break;
            case Item.TornadoAdjustment: Tornadoes.SetHasTornadoKnowledge(count > 0); break;
            case Item.SilentRunning: Anglerfish.SetHasAnglerfishKnowledge(count > 0); break;
            case Item.ElectricalInsulation: Jellyfish.SetHasJellyfishKnowledge(count > 0); break;
            case Item.Coordinates: break; // todo

            // todo: can we disable the OW Ventures frequency?
            case Item.FrequencyDB: Signalscope.SetFrequencyUsable(SignalFrequency.EscapePod, count > 0); break;
            case Item.FrequencyQF: Signalscope.SetFrequencyUsable(SignalFrequency.Quantum, count > 0); break;
            case Item.FrequencyHS: Signalscope.SetFrequencyUsable(SignalFrequency.HideAndSeek, count > 0); break;

            case Item.SignalChert: Signalscope.SetSignalUsable(SignalName.Traveler_Chert, count > 0); break;
            case Item.SignalEsker: Signalscope.SetSignalUsable(SignalName.Traveler_Esker, count > 0); break;
            case Item.SignalRiebeck: Signalscope.SetSignalUsable(SignalName.Traveler_Riebeck, count > 0); break;
            case Item.SignalGabbro: Signalscope.SetSignalUsable(SignalName.Traveler_Gabbro, count > 0); break;
            case Item.SignalFeldspar: Signalscope.SetSignalUsable(SignalName.Traveler_Feldspar, count > 0); break;
            case Item.SignalMuseumShard: Signalscope.SetSignalUsable(SignalName.Quantum_TH_MuseumShard, count > 0); break;
            case Item.SignalGroveShard: Signalscope.SetSignalUsable(SignalName.Quantum_TH_GroveShard, count > 0); break;
            case Item.SignalCaveShard: Signalscope.SetSignalUsable(SignalName.Quantum_CT_Shard, count > 0); break;
            case Item.SignalTowerShard: Signalscope.SetSignalUsable(SignalName.Quantum_BH_Shard, count > 0); break;
            case Item.SignalIslandShard: Signalscope.SetSignalUsable(SignalName.Quantum_GD_Shard, count > 0); break;
            case Item.SignalQM: Signalscope.SetSignalUsable(SignalName.Quantum_QM, count > 0); break;
            case Item.SignalEP1: Signalscope.SetSignalUsable(SignalName.EscapePod_BH, count > 0); break;
            case Item.SignalEP2: Signalscope.SetSignalUsable(SignalName.EscapePod_CT, count > 0); break;
            case Item.SignalEP3: Signalscope.SetSignalUsable(SignalName.EscapePod_DB, count > 0); break;
            case Item.SignalGalena: Signalscope.SetSignalUsable(SignalName.HideAndSeek_Galena, count > 0); break;
            case Item.SignalTephra: Signalscope.SetSignalUsable(SignalName.HideAndSeek_Tephra, count > 0); break;
            default: break;
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
        CheckLocation(Location.TH_HORNFELS);
    }
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerState), nameof(PlayerState.OnEnterShip))]
    public static void PlayerState_OnEnterShip_Prefix()
    {
        // not an important optimization, but a very easy one
        var firstTimeThisLoop = !PlayerState.HasPlayerEnteredShip();
        if (firstTimeThisLoop)
            CheckLocation(Location.SPACESHIP);
    }
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CharacterDialogueTree), nameof(CharacterDialogueTree.EndConversation))]
    public static void CharacterDialogueTree_EndConversation_Prefix(CharacterDialogueTree __instance)
    {
        var dialogueTreeName = __instance._xmlCharacterDialogueAsset.name;
        Randomizer.Instance.ModHelper.Console.WriteLine($"CharacterDialogueTree.EndConversation {dialogueTreeName}");

        if (dialogueTreeName == "Hal_Museum" || dialogueTreeName == "Hal_Outside")
            CheckLocation(Location.TH_HAL);

        if (dialogueTreeName == "GhostMatterPlaque")
            CheckLocation(Location.TH_GM);

        if (dialogueTreeName == "Hornfels")
            CheckLocation(Location.TH_HORNFELS);
    }
}
