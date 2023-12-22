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

    // no longer in use, keeping as notes for when we edit flavor text to justify some items' existence
    /*static Dictionary<Location, Item> locationToVanillaItem = new Dictionary<Location, Item> {
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
    };*/

    // TODO: actual randomization / AP integration; this is a one-off shuffle for test playthroughs
    static Dictionary<Location, Item> locationToRandomItem = new Dictionary<Location, Item>
    {
        { Location.ET_QML, Item.SilentRunning },
        { Location.TH_GALENA_SIGNAL, Item.EntanglementRule },
        { Location.SPACESHIP, Item.Spaceship }, // always fixed, more like an event than an item
        { Location.ET_EP2_SIGNAL, Item.Scout },
        { Location.BH_SHARD_SIGNAL, Item.Nothing },
        { Location.GD_SIW, Item.Signalscope },
        { Location.FROZEN_SHUTTLE, Item.TornadoAdjustment },
        { Location.BH_EP1_SIGNAL, Item.WarpCoreManual },
        { Location.DB_VESSEL, Item.WarpPlatformCodes },
        { Location.IL_CORE, Item.ShrineDoorCodes },
        { Location.GD_BI, Item.Translator },
        { Location.TH_MS_SIGNAL, Item.CameraQuantum },
        { Location.WHS, Item.Nothing },
        { Location.GD_COORDINATES, Item.FrequencyDB },
        { Location.ET_SHARD_SIGNAL, Item.ElectricalInsulation },
        { Location.OPC_CM, Item.FrequencyQF },
        { Location.AR_WHISTLE, Item.FrequencyHS },
        { Location.DB_GRAVE, Item.SignalChert },
        { Location.BH_OBSERVATORY, Item.SignalEsker },
        { Location.ET_COLEUS_CAVE, Item.SignalRiebeck },
        { Location.SS, Item.SignalGabbro },
        { Location.OPC_ENTER, Item.SignalFeldspar },
        { Location.TH_TEPHRA_SIGNAL, Item.SignalMuseumShard },
        { Location.TH_MINES, Item.SignalGroveShard },
        { Location.GD_DEPTHS, Item.SignalCaveShard },
        { Location.DB_EP3_SIGNAL, Item.SignalTowerShard },
        { Location.TH_SEED_CRATER, Item.SignalIslandShard },
        { Location.ET_HEL, Item.SignalQM },
        { Location.TH_ZERO_G, Item.SignalEP1 },
        { Location.QM_SIGNAL, Item.SignalEP2 },
        { Location.DB_HARMONICA, Item.SignalEP3 },
        { Location.FREQ_HIDE_SEEK, Item.SignalGalena },
        { Location.FREQ_QUANTUM, Item.SignalTephra },
        { Location.SOLANUM_SHUTTLE, Item.Nothing },
        { Location.AR_ESL, Item.Nothing },
        { Location.BH_OS_MURAL, Item.CameraGM },
        { Location.ET_LAKEBED_CAVE, Item.Nothing },
        { Location.BH_TOWER, Item.Nothing },
        { Location.BH_BANJO, Item.Nothing },
        { Location.QM_LAND, Item.Nothing },
        { Location.TH_GS_SIGNAL, Item.Nothing },
        { Location.GD_FLUTE, Item.Nothing },
        { Location.ET_DRUM, Item.Nothing },
        { Location.FREQ_DISTRESS, Item.Nothing },
        { Location.GD_CY, Item.Nothing },
        { Location.ET_SC_SHRINE, Item.Coordinates },
        { Location.HL_VTS, Item.Nothing },
        { Location.GD_CORE, Item.Nothing },
        { Location.TH_HAL, Item.Nothing },
        { Location.BH_FORGE, Item.Nothing },
        { Location.AT_ATP, Item.Nothing },
        { Location.GD_TOWER_COMPLETE, Item.Nothing },
        { Location.TH_GM, Item.Nothing },
        { Location.TH_HORNFELS, Item.LaunchCodes }, // fixed for now
        { Location.GD_TOWER_RULE, Item.Nothing },
        { Location.QM_6L, Item.Nothing },
        { Location.DB_JELLY, Item.Nothing },
        { Location.ET_FOSSIL, Item.Nothing },
        { Location.GD_SHARD_SIGNAL, Item.Nothing },
    };

    public static void CheckLocation(Location location)
    {
        var locationChecked = Randomizer.SaveData.locationsChecked;
        if (!locationChecked.ContainsKey(location))
        {
            Randomizer.OWMLModConsole.WriteLine($"'{location}' missing from locationChecked dictionary", OWML.Common.MessageType.Error);
            return;
        }

        if (locationChecked[location])
        {
            Randomizer.OWMLModConsole.WriteLine($"'{location}' has already been checked. Doing nothing.");
            return;
        }
        else
        {
            Randomizer.OWMLModConsole.WriteLine($"Marking '{location}' as checked");
            locationChecked[location] = true;

            var item = locationToRandomItem.ContainsKey(location) ? locationToRandomItem[location] : Item.Nothing;

            // todo: replace this with Archipelago integration
            Randomizer.OWMLModConsole.WriteLine($"Awarding item {item}");

            var itemName = ItemNames.ItemToName(item);
            ArchConsoleManager.AddConsoleText($"You found your <color=orange>{itemName}</color>", false, AudioType.ShipLogMarkLocation);

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
            case Item.Coordinates: Coordinates.SetHasCoordinates(count > 0); break;
            default: break;
        }
        if (ItemNames.itemToFrequency.ContainsKey(item))
            Signalscope.SetFrequencyUsable(ItemNames.itemToFrequency[item], count > 0);
        else if (ItemNames.itemToSignal.ContainsKey(item))
            Signalscope.SetSignalUsable(ItemNames.itemToSignal[item], count > 0);
    }


    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipLogManager), nameof(ShipLogManager.RevealFact))]
    public static void ShipLogManager_RevealFact_Prefix(string id, bool saveGame, bool showNotification)
    {
        var factId = id;

        // Currently, only a subset of ship log facts are location triggers.
        // But I want the released mod to be logging these ids so that players who want a "logsanity"
        // and/or "rumorsanity" option can help assemble the list of locations and rules for it.
        Randomizer.OWMLModConsole.WriteLine($"ShipLogManager.RevealFact {factId}");

        if (logFactToLocation.ContainsKey(factId))
        {
            var locationName = logFactToLocation[factId];
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
        Randomizer.OWMLModConsole.WriteLine($"CharacterDialogueTree.EndConversation {dialogueTreeName}");

        switch (dialogueTreeName)
        {
            case "Hal_Museum": case "Hal_Outside": CheckLocation(Location.TH_HAL); break;
            case "GhostMatterPlaque": CheckLocation(Location.TH_GM); break;
            case "Hornfels": CheckLocation(Location.TH_HORNFELS); break;
        }
    }

    // Currently, translation a Nomai text line is never (directly) a trigger for a location.
    // But I want the released mod to be logging these ids so that players who want a "textsanity"
    // option can help assemble the list of locations and rules for it.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(NomaiText), nameof(NomaiText.SetAsTranslated))]
    public static void NomaiText_SetAsTranslated_Prefix(NomaiText __instance, int id)
    {
        // This gets called every frame when looking at translated text, so avoid logging if it's already been translated (this loop)
        if (__instance._dictNomaiTextData[id].IsTranslated) return;

        var textAssetName = __instance._nomaiTextAsset?.name ?? "(No text asset, likely generated in code?)";
        Randomizer.OWMLModConsole.WriteLine($"NomaiText.SetAsTranslated: {textAssetName} line {id}");
    }
}
