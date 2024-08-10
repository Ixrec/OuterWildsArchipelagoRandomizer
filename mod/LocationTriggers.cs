using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class LocationTriggers
{
    // Only for default locations. Logsanity locations have the log fact in their id so they don't need an additional hardcoded map.
    public static Dictionary<string, Location> logFactToDefaultLocation = new Dictionary<string, Location>{
        { "S_SUNSTATION_X2", Location.SS },

        { "CT_HIGH_ENERGY_LAB_X3", Location.ET_HEL },
        { "CT_SUNLESS_CITY_X3", Location.ET_SC_SHRINE },
        { "QM_SIXTH_LOCATION_R1", Location.ET_QML },
        { "CT_ANGLERFISH_FOSSIL_X2", Location.ET_FOSSIL },
        { "CT_LAKEBED_CAVERN_X1", Location.ET_LAKEBED_CAVE },
        { "CT_LAKEBED_CAVERN_X3", Location.ET_COLEUS_CAVE },
        { "CT_SUNLESS_CITY_X1", Location.ET_SC },

        { "TT_TIME_LOOP_DEVICE_X1", Location.AT_ATP },

        // when we add flavor text, probably change this location to trigger on talking to Gossan after the repairs
        { "TH_ZERO_G_CAVE_X2", Location.TH_ZERO_G },
        { "TH_IMPACT_CRATER_X1", Location.TH_SEED_CRATER },
        { "TH_NOMAI_MINE_X1", Location.TH_MINES },

        { "TM_EYE_LOCATOR_X2", Location.AR_ESL },

        { "BH_TORNADO_SIMULATION_X2", Location.BH_OBSERVATORY },
        { "BH_MURAL_1_X1", Location.BH_OS_MURAL },
        { "BH_MURAL_2_X1", Location.BH_OS_MURAL },
        { "BH_MURAL_3_X1", Location.BH_OS_MURAL },
        { "BH_BLACK_HOLE_FORGE_X6", Location.BH_FORGE },
        { "BH_QUANTUM_RESEARCH_TOWER_X1", Location.BH_TOWER },
        { "BH_HANGING_CITY_X4", Location.BH_HC_SHRINE },
        { "BH_HANGING_CITY_X1", Location.BH_HC },

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
        { "GD_STATUE_ISLAND_X2", Location.GD_STATUE },

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
        { "DB_FROZEN_JELLYFISH_X1", Location.DB_JELLY_TR },
        { "DB_NOMAI_GRAVE_X3", Location.DB_GRAVE },
        { "DB_VESSEL_X1", Location.DB_VESSEL },
    };

    // manual scrolls
    public static Dictionary<string, Location> ManualScrollLocations = new()
        {
            { "BH_City_School_BigBangLesson", Location.BH_SOLANUM_REPORT },
            { "TT_Tower_CT", Location.AT_HGT_TOWERS },
            { "TT_Tower_BH_1", Location.AT_BH_TOWER }
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
        var locationChecked = APRandomizer.SaveData.locationsChecked;
        if (!locationChecked.ContainsKey(location))
        {
            if (LocationNames.IsLocationActive(location))
                APRandomizer.OWMLModConsole.WriteLine($"'{location}' missing from locationChecked dictionary", OWML.Common.MessageType.Error);
            // else location is a logsanity location in a non-logsanity world, or a DLC location in a non-DLC world, etc. Doing nothing.
            return;
        }

        if (locationChecked[location]) return;

        locationChecked[location] = true;
        APRandomizer.WriteToSaveFile();

        if (LocationNames.locationToArchipelagoId.ContainsKey(location))
        {
            if (APRandomizer.DisableInGameLocationSending && LoadManager.GetCurrentScene() == OWScene.SolarSystem) return;

            var locationId = LocationNames.locationToArchipelagoId[location];

            // we want to time out relatively quickly if the server happens to be down, but don't
            // block whatever we (and the vanilla game) were doing on waiting for the AP server response
            var _ = Task.Run(() =>
            {
                // TODO: session.Socket.OnError ?
                var checkLocationTask = Task.Run(() => APRandomizer.APSession.Locations.CompleteLocationChecks(locationId));
                if (!checkLocationTask.Wait(TimeSpan.FromSeconds(2)))
                {
                    var msg = $"AP server timed out when we tried to tell it that you checked location '{LocationNames.locationNames[location]}'. Did the connection go down?";
                    APRandomizer.OWMLModConsole.WriteLine(msg, OWML.Common.MessageType.Warning);
                    APRandomizer.InGameAPConsole.AddText($"<color='orange'>{msg}</color>");
                }
            });
        }
        else
        {
            APRandomizer.OWMLModConsole.WriteLine($"Location {location} appears to be an 'event location', so not sending anything to the AP server");
        }
    }

    public static void ApplyItemToPlayer(Item item, uint count)
    {
        if (ItemNames.itemToFrequency.ContainsKey(item))
        {
            SignalsAndFrequencies.SetFrequencyUsable(ItemNames.itemToFrequency[item], count > 0);
            return;
        }
        else if (ItemNames.itemToSignal.ContainsKey(item))
        {
            SignalsAndFrequencies.SetSignalUsable(ItemNames.itemToSignal[item], count > 0);
            return;
        }

        switch (item)
        {
            case Item.LaunchCodes: LaunchCodes.hasLaunchCodes = (count > 0); break;
            case Item.Spacesuit: Spacesuit.hasSpacesuit = (count > 0); break;
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
            case Item.OxygenCapacityUpgrade: SuitResources.oxygenCapacityUpgrades = count; break;
            case Item.FuelCapacityUpgrade: SuitResources.fuelCapacityUpgrades = count; break;
            case Item.BoostDurationUpgrade: SuitResources.boostDurationUpgrades = count; break;
            case Item.OxygenRefill: SuitResources.oxygenRefills = count; break;
            case Item.FuelRefill: SuitResources.fuelRefills = count; break;
            case Item.Marshmallow: Marshmallows.normalMarshmallows = count; break;
            case Item.PerfectMarshmallow: Marshmallows.perfectMarshmallows = count; break;
            case Item.BurntMarshmallow: Marshmallows.burntMarshmallows = count; break;
            case Item.ShipDamageTrap: ShipDamage.shipDamageTraps = count; break;
            case Item.AudioTrap: AudioTrap.audioTraps = count; break;
            case Item.NapTrap: NapTrap.napTraps = count; break;

            // for backwards-compatibility
            case Item.Spaceship: break; case Item.Nothing: break;
            default:
                APRandomizer.OWMLModConsole.WriteLine($"unknown item: {item}", OWML.Common.MessageType.Error);
                break;
        }

        if (PlayerData._currentGameSave != null)
            if (ItemNames.itemToPersistentCondition.TryGetValue(item, out var condition))
                PlayerData.SetPersistentCondition(condition, (count > 0)); // for now, only unique items have conditions
    }


    [HarmonyPrefix, HarmonyPatch(typeof(ShipLogManager), nameof(ShipLogManager.RevealFact))]
    public static void ShipLogManager_RevealFact_Prefix(string id, bool saveGame, bool showNotification)
    {
        var factId = id;
        APRandomizer.OWMLModConsole.WriteLine($"ShipLogManager.RevealFact {factId}");

        if (logFactToDefaultLocation.ContainsKey(factId))
            CheckLocation(logFactToDefaultLocation[factId]);

        if (APRandomizer.SlotData != null && APRandomizer.SlotData.ContainsKey("logsanity") && (long)APRandomizer.SlotData["logsanity"] == 1)
        {
            // Because logsanity locations correspond exactly 1-to-1 to ship log facts,
            // we can simply parse the fact id instead of writing another hardcoded map.
            if (Enum.TryParse<Location>($"SLF__{factId}", out var location))
                CheckLocation(location);
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(PlayerData), nameof(PlayerData.LearnLaunchCodes))]
    public static void PlayerData_LearnLaunchCodes_Prefix()
    {
        CheckLocation(Location.TH_HORNFELS);
    }
    [HarmonyPrefix, HarmonyPatch(typeof(CharacterDialogueTree), nameof(CharacterDialogueTree.EndConversation))]
    public static void CharacterDialogueTree_EndConversation_Prefix(CharacterDialogueTree __instance)
    {
        // EndConversation() is often called "spuriously" by the base game, and by other mods,
        // because it's expected to check InConversation() before doing anything.
        // This turned out to be critical for New Horizons compatibility, since one of its vanilla fixes calls EndConversation().
        if (!__instance.InConversation())
            return;

        var dialogueTreeName = __instance._xmlCharacterDialogueAsset.name;
        APRandomizer.OWMLModConsole.WriteLine($"CharacterDialogueTree.EndConversation {dialogueTreeName}");

        // If it ever comes up, avoid using "Feldspar_Journal" or "Gabbro_1" here.
        // Those "conversations" seem to spontaneously complete themselves every so often no matter what the player's doing.
        switch (dialogueTreeName)
        {
            case "Hal_Museum": case "Hal_Outside": CheckLocation(Location.TH_HAL); break;
            case "GhostMatterPlaque": CheckLocation(Location.TH_GM); break;
            case "Hornfels": CheckLocation(Location.TH_HORNFELS); break;
            case "Hornfels_CampfireNote": case "Hornfels_CampfireNote_Vanilla": CheckLocation(Location.TH_CAMPFIRE_NOTE); break;
            case "Chert_QuantumSignalNotes": CheckLocation(Location.TH_GROVE_PLAQUE); break;
            case "Riebeck_AttlerockRecording": CheckLocation(Location.AR_ESL_TR); break;
            case "Chert_MoonCraterNotes": CheckLocation(Location.AR_ICE_TR); break;
            case "Esker_SignalscopeLog": CheckLocation(Location.AR_LL); break;
            case "Feldspar_FuelStashNote": CheckLocation(Location.BH_NG_NOTE); break;
            case "Riebeck_SouthPoleRecording": CheckLocation(Location.BH_OBSERVATORY_TR); break;
            case "Riebeck_Journal_1": CheckLocation(Location.BH_CAMPSITE_NOTE); break;
            case "Chert_QuantumMoonLocatorNotes": CheckLocation(Location.ET_QML_TR); break;
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(NomaiText), nameof(NomaiText.SetAsTranslated))]
    public static void NomaiText_SetAsTranslated_Prefix(NomaiText __instance, int id)
    {
        // This gets called every frame when looking at translated text, so avoid logging if it's already been translated (this loop)
        if (__instance._dictNomaiTextData[id].IsTranslated) return;

        var textAssetName = __instance._nomaiTextAsset?.name ?? "(No text asset, likely generated in code?)";

        if (ManualScrollLocations.ContainsKey(textAssetName))
        {
            CheckLocation(ManualScrollLocations[textAssetName]);
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(PlayerRecoveryPoint), nameof(PlayerRecoveryPoint.OnPressInteract))]
    public static void PlayerRecoveryPoint_OnPressInteract(PlayerRecoveryPoint __instance)
    {
        var parentName = __instance?.gameObject?.transform?.parent?.name;
        APRandomizer.OWMLModConsole.WriteLine($"PlayerRecoveryPoint_OnPressInteract __instance?.name={__instance?.name}, parentName={parentName}");

        // The only non-tank recovery point I know of is the ship's medkit, which has instanceName=PlayerRecoveryPoint and parentName=Systems_Supplies.
        // This has to be StartsWith() instead of == because Esker's tank is "Prefab_HEA_FuelTank (1)"
        var instanceName = __instance?.name ?? "";
        var isFuelTank = instanceName.StartsWith("Prefab_HEA_FuelTank");
        if (!isFuelTank) return;

        switch (parentName)
        {
            case "Interactables_SouthPole": CheckLocation(Location.ET_QML_TANK); break;
            case "Interactables_Lakebed": CheckLocation(Location.ET_CHERT_TANK); break;
            case "FuelStash": CheckLocation(Location.BH_NG_TANK); break;
            case "Interactables_Crossroads": CheckLocation(Location.BH_RIEBECK_TANK); break;
            case "Interactables_BrambleIsland": CheckLocation(Location.GD_BI_TANK); break;
            case "Interactables_GabbroIsland": CheckLocation(Location.GD_GABBRO_TANK); break;
            case "Interactables_PioneerDimension": CheckLocation(Location.DB_FELDSPAR_TANK); break;
            // Attlerock (THM = Timber Hearth Moon) has two fuel tanks, so we need additional checks to tell which is which
            case "Interactables_THM":
                if (instanceName == "Prefab_HEA_FuelTank")
                    CheckLocation(Location.AR_ICE_TANK);
                else if (instanceName == "Prefab_HEA_FuelTank (1)")
                    CheckLocation(Location.AR_ESKER_TANK);
                break;
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(DialogueConditionTrigger), nameof(DialogueConditionTrigger.OnEntry))]
    public static void DialogueConditionManager_OnEntry(DialogueConditionTrigger __instance, UnityEngine.GameObject hitObj)
    {
        APRandomizer.OWMLModConsole.WriteLine($"DialogueConditionTrigger.OnEntry: {__instance.name}, {__instance._conditionID}, _persistentCondition={__instance._persistentCondition}, {hitObj.name}");
        switch (__instance._conditionID)
        {
            case "FoundGabbroShip": CheckLocation(Location.GD_SHIP); break;
        }
    }

    // In vanilla DB_VESSEL_X2 is for putting the dead warp core into the socket, but in rando that often forces
    // a whole 2nd Vessel trip just for one log fact, so here we change it to just picking up the dead core.
    [HarmonyPostfix, HarmonyPatch(typeof(OWItem), nameof(OWItem.PickUpItem))]
    private static void OWItem_PickUpItem_Postfix(OWItem __instance, UnityEngine.Transform holdTranform)
    {
        if (
            __instance.GetItemType() == ItemType.WarpCore &&
            (__instance as WarpCoreItem).GetWarpCoreType() == WarpCoreType.VesselBroken
        ) {
            Locator.GetShipLogManager().RevealFact("DB_VESSEL_X2");
        }
    }
}
