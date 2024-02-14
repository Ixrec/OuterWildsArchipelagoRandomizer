using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class LocationTriggers
{
    // Only for default locations. Logsanity locations have the log fact in their id so they don't need an additional hardcoded map.
    static Dictionary<string, Location> logFactToDefaultLocation = new Dictionary<string, Location>{
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

        // Consider "control module logs" checked if *any* of the three sets of text wheels get translated
        { "OPC_INTACT_MODULE_X1", Location.OPC_CM },
        { "OPC_INTACT_MODULE_X2", Location.OPC_CM },
        { "OPC_SUNKEN_MODULE_R2", Location.OPC_CM }, // the 3rd set only gives rumors

        { "GD_OCEAN_R3", Location.GD_BI },
        { "GD_CONSTRUCTION_YARD_X1", Location.GD_CY },
        { "GD_STATUE_WORKSHOP_X1", Location.GD_SIW },
        { "GD_OCEAN_X1", Location.GD_DEPTHS },
        { "GD_OCEAN_X2", Location.GD_CORE },
        { "GD_QUANTUM_TOWER_X2", Location.GD_TOWER_RULE },
        { "GD_QUANTUM_TOWER_X4", Location.GD_TOWER_COMPLETE },

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

        { Location.TH_HORNFELS, Item.LaunchCodes }
    };*/


    public static void CheckLocation(Location location)
    {
        var locationChecked = Randomizer.SaveData.locationsChecked;
        if (!locationChecked.ContainsKey(location))
        {
            if (LocationNames.IsLogsanityLocation(location) && !(Randomizer.SlotData != null && Randomizer.SlotData.ContainsKey("logsanity") && (long)Randomizer.SlotData["logsanity"] == 1))
                Randomizer.OWMLModConsole.WriteLine($"'{location}' is a logsanity location, and this world does not have logsanity enabled. Doing nothing.");
            else
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
            Randomizer.OWMLModConsole.WriteLine($"Marking '{location}' as checked in mod save file", OWML.Common.MessageType.Info);
            locationChecked[location] = true;
            Randomizer.Instance.WriteToSaveFile();

            if (LocationNames.locationToArchipelagoId.ContainsKey(location))
            {
                var locationId = LocationNames.locationToArchipelagoId[location];
                Randomizer.OWMLModConsole.WriteLine($"Telling AP server that location ID {locationId} ({location}) was just checked", OWML.Common.MessageType.Info);

                // we want to time out relatively quickly if the server happens to be down
                var checkLocationTask = Task.Run(() => Randomizer.APSession.Locations.CompleteLocationChecks(locationId));
                if (!checkLocationTask.Wait(TimeSpan.FromSeconds(1)))
                    throw new Exception("CompleteLocationChecks() task timed out");
            }
            else
            {
                Randomizer.OWMLModConsole.WriteLine($"Location {location} appears to be an 'event location', so not sending anything to the AP server");
            }
        }
    }

    public static void ApplyItemToPlayer(Item item, uint count)
    {
        if (ItemNames.itemToFrequency.ContainsKey(item))
        {
            SignalscopeManager.SetFrequencyUsable(ItemNames.itemToFrequency[item], count > 0);
            return;
        }
        else if (ItemNames.itemToSignal.ContainsKey(item))
        {
            SignalscopeManager.SetSignalUsable(ItemNames.itemToSignal[item], count > 0);
            return;
        }

        switch (item)
        {
            case Item.LaunchCodes: break; // Not necessary until launch codes can be shuffled, and it's surprisingly subtle to set them without crashing.

            case Item.Translator: Translator.hasTranslator = (count > 0); break;
            case Item.Signalscope: SignalscopeManager.hasSignalscope = (count > 0); break;
            case Item.Scout: Scout.hasScout = (count > 0); break;
            case Item.CameraGM: GhostMatter.hasGhostMatterKnowledge = (count > 0); break;
            case Item.CameraQuantum: QuantumImaging.hasImagingKnowledge = (count > 0); break;
            case Item.WarpPlatformCodes: WarpPlatforms.hasNomaiWarpCodes = (count > 0); break;
            case Item.WarpCoreManual: WarpCoreManual.hasWarpCoreManual = (count > 0); break;
            case Item.EntanglementRule: QuantumEntanglement.hasEntanglementKnowledge = (count > 0); break;
            case Item.ShrineDoorCodes: QuantumShrineDoor.hasQuantumShrineCodes = (count > 0); break;
            case Item.TornadoAdjustment: Tornadoes.hasTornadoKnowledge = (count > 0); break;
            case Item.SilentRunning: Anglerfish.hasAnglerfishKnowledge = (count > 0); break;
            case Item.ElectricalInsulation: Jellyfish.hasJellyfishKnowledge = (count > 0); break;
            case Item.Coordinates: Coordinates.hasCoordinates = (count > 0); break;
            case Item.Autopilot: AutopilotManager.hasAutopilot = (count > 0); break;
            case Item.LandingCamera: LandingCamera.hasLandingCamera = (count > 0); break;
            case Item.EjectButton: EjectButton.hasEjectButton = (count > 0); break;
            case Item.VelocityMatcher: VelocityMatcher.hasVelocityMatcher = (count > 0); break;
            case Item.SurfaceIntegrityScanner: SurfaceIntegrity.hasSurfaceIntegrityScanner = (count > 0); break;
            case Item.OxygenRefill: Oxygen.oxygenRefills = count; break;
            case Item.FuelRefill: Jetpack.fuelRefills = count; break;
            case Item.Marshmallow: Marshmallows.normalMarshmallows = count; break;
            case Item.PerfectMarshmallow: Marshmallows.perfectMarshmallows = count; break;
            case Item.BurntMarshmallow: Marshmallows.burntMarshmallows = count; break;
            case Item.ShipDamageTrap: ShipDamage.shipDamageTraps = count; break;

            // for backwards-compatibility
            case Item.Spaceship: break; case Item.Nothing: break;
            default:
                Randomizer.OWMLModConsole.WriteLine($"unknown item: {item}", OWML.Common.MessageType.Error);
                break;
        }
    }


    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipLogManager), nameof(ShipLogManager.RevealFact))]
    public static void ShipLogManager_RevealFact_Prefix(string id, bool saveGame, bool showNotification)
    {
        var factId = id;
        Randomizer.OWMLModConsole.WriteLine($"ShipLogManager.RevealFact {factId}");

        if (logFactToDefaultLocation.ContainsKey(factId))
            CheckLocation(logFactToDefaultLocation[factId]);

        if (Randomizer.SlotData != null && Randomizer.SlotData.ContainsKey("logsanity") && (long)Randomizer.SlotData["logsanity"] == 1) {
            // Because logsanity locations correspond exactly 1-to-1 to ship log facts,
            // we can simply parse the fact id instead of writing another hardcoded map.
            if (Enum.TryParse<Location>($"SLF__{factId}", out var location))
                CheckLocation(location);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerData), nameof(PlayerData.LearnLaunchCodes))]
    public static void PlayerData_LearnLaunchCodes_Prefix()
    {
        CheckLocation(Location.TH_HORNFELS);
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
