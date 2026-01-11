using HarmonyLib;
using UnityEngine;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class Spawn
{
    enum SpawnChoice
    {
        Vanilla,
        HourglassTwins,
        TimberHearth,
        BrittleHollow,
        GiantsDeep,
        Stranger,
    }

    private static SpawnChoice spawnChoice = SpawnChoice.Vanilla;

    public static void ApplySlotData(long spawnChoiceSlotData)
    {
        switch (spawnChoiceSlotData)
        {
            case /*"vanilla"*/         0: spawnChoice = SpawnChoice.Vanilla; break;
            case /*"hourglass_twins"*/ 1: spawnChoice = SpawnChoice.HourglassTwins; break;
            case /*"timber_hearth"*/   2: spawnChoice = SpawnChoice.TimberHearth; break;
            case /*"brittle_hollow"*/  3: spawnChoice = SpawnChoice.BrittleHollow; break;
            case /*"giants_deep"*/     4: spawnChoice = SpawnChoice.GiantsDeep; break;
            case /*"stranger"*/        5: spawnChoice = SpawnChoice.Stranger; break;
        }
    }

    private static bool spawnInSuitNextUpdate = false;

    public static void OnCompleteSceneLoad(OWScene _scene, OWScene loadScene)
    {
        if (loadScene != OWScene.SolarSystem) return;

        if (spawnChoice != SpawnChoice.Vanilla)
        {
            //APRandomizer.OWMLModConsole.WriteLine($"Spawn::OnCompleteSceneLoad() ensuring that the time loop has started and the player will spawn in their suit");

            spawnInSuitNextUpdate = true;

            // For whatever reason, the base game uses the LAUNCH_CODES_GIVEN condition to track
            // the time loop being started, not whether the launch codes have been given yet.

            // We're calling these methods directly on the GameSave instead of PlayerData, because
            // PlayerData::SetPersistentCondition() specifically avoids saving LAUNCH_CODES_GIVEN.
            if (!PlayerData._currentGameSave.PersistentConditionExists("LAUNCH_CODES_GIVEN"))
                PlayerData._currentGameSave.SetPersistentCondition("LAUNCH_CODES_GIVEN", true);
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(ShipLogManager), nameof(ShipLogManager.Start))]
    public static void ShipLogManager_Start_Postfix(ShipLogManager __instance)
    {
        // The Village 2 logsanity check has a few issues:
        // - In the base game, it can be missed if you somehow die in between talking to Hornfels and syncing with the statue.
        // - Non-vanilla spawns make it unreachable.
        // Since this isn't "intended missable" like Village 3, I'd rather not completely remove the location from logsanity,
        // so instead we have to trigger this ship log fact ourselves if the player can no longer get it themselves.
        // Since this file has to fiddle with LAUNCH_CODES_GIVEN anyway, this seems like the least bad place to put it.
        if (PlayerData._currentGameSave.PersistentConditionExists("LAUNCH_CODES_GIVEN") && !__instance.IsFactRevealed("TH_VILLAGE_X2"))
        {
            APRandomizer.OWMLModConsole.WriteLine($"auto-revealing Village 2 ship log because the time loop has already started");
            __instance.RevealFact("TH_VILLAGE_X2");
        }

        if (APRandomizer.SlotEnabledEotEDLC() && !__instance.IsFactRevealed("IP_RING_WORLD_X1"))
        {
            APRandomizer.OWMLModConsole.WriteLine($"auto-revealing The Stranger ship log because EotE DLC is enabled");
            __instance.RevealFact("IP_RING_WORLD_X1");
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(PlayerSpawner), nameof(PlayerSpawner.Update))]
    public static void PlayerSpawner_Update(PlayerSpacesuit __instance)
    {
        if (spawnInSuitNextUpdate)
        {
            // hide the suit model inside the ship, so the player won't see a "duplicate" suit
            APRandomizer.OWMLModConsole.WriteLine($"PlayerSpawner_Update hiding spacesuit");
            Spacesuit.SetSpacesuitVisible(false);

            // The SuitUp() call must be done *after* SetSpacesuitVisible(), like in the base game's SPV.OnPressInteract(),
            // because SPV.OnSuitUp() strongly assumes that SPV._containsSuit has already been updated.
            // This is because there are two SPVs in the game, and it's correct for e.g. the training suit to disable
            // its prompt once you put on the ship suit, and vice versa, but the only way for each SPV to know if it's the one
            // that just got picked up is by assuming SPV._containsSuit has been updated already.
            // If we get this wrong, then you lose the ability to take off your suit, breaking HN2's final puzzle.
            Locator.GetPlayerSuit().SuitUp(isTrainingSuit: false, instantSuitUp: true, putOnHelmet: true);
            spawnInSuitNextUpdate = false;
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(AlignPlayerWithForce), nameof(AlignPlayerWithForce.OnSuitUp))]
    public static bool AlignPlayerWithForce_OnSuitUp(AlignPlayerWithForce __instance)
    {
        if (spawnInSuitNextUpdate)
        {
            //APRandomizer.OWMLModConsole.WriteLine($"skipping AlignPlayerWithForce::OnSuitUp() call so the player wakes up facing the sky despite wearing the spacesuit");
            return false;
        }
        //APRandomizer.OWMLModConsole.WriteLine($"normal AlignPlayerWithForce::OnSuitUp() call");
        return true;
    }

    private static bool NewHorizonsWarpingToVanillaSystem = false;
    public static void OnChangeStarSystemEvent(string system) => NewHorizonsWarpingToVanillaSystem = true;
    // NH doesn't appear to have an event for "done spawning player". I tested StarSystemLoadedEvent, and that one fires *before* SpawnPlayer().
    // So in practice we're relying on the assumption that there will always be exactly one SpawnPlayer() call per ChangeStarSystemEvent.

    // In general, when other mods might be patching the same method we are, postfix patches that overwrite the result are more robust than
    // prefix patches that skip the vanilla method (and all other mods' patches, which is the really dangerous part).
    // We know NewHorizons also patches SpawnPlayer, so it's definitely worth favoring postfix here.
    [HarmonyPostfix, HarmonyPatch(typeof(PlayerSpawner), nameof(PlayerSpawner.SpawnPlayer))]
    public static void PlayerSpawner_SpawnPlayer(PlayerSpawner __instance)
    {
        if (!APRandomizer.IsVanillaSystemLoaded())
        {
            APRandomizer.OWMLModConsole.WriteLine($"PlayerSpawner_SpawnPlayer doing nothing, since we're not in the vanilla solar system");
            return;
        }
        if (NewHorizonsWarpingToVanillaSystem)
        {
            APRandomizer.OWMLModConsole.WriteLine($"PlayerSpawner_SpawnPlayer doing nothing, since NewHorizons is warping is back to the vanilla system");
            NewHorizonsWarpingToVanillaSystem = false;
            return;
        }

        OWRigidbody anchorBody = null;
        GameObject playerTargetGO = null;
        Vector3 playerOffset = Vector3.zero;
        Vector3 shipPosition = Vector3.zero;
        Quaternion shipRotation = Quaternion.identity;

        if (spawnChoice == SpawnChoice.Vanilla || spawnChoice == SpawnChoice.TimberHearth)
        {
            APRandomizer.OWMLModConsole.WriteLine($"PlayerSpawner_SpawnPlayer doing nothing, since we're spawning in TH village");
        }
        else if (spawnChoice == SpawnChoice.HourglassTwins)
        {
            playerTargetGO = GameObject.Find("CaveTwin_Body/Sector_CaveTwin/Sector_NorthHemisphere/Sector_NorthSurface/Sector_Lakebed/Interactables_Lakebed/Lakebed_VisibleFrom_Far/Prefab_HEA_Campfire");
            anchorBody = Locator.GetAstroObject(AstroObject.Name.CaveTwin).GetOWRigidbody();
            playerOffset = new Vector3(3, 0, -3);
            shipRotation = anchorBody.transform.rotation;
            shipPosition = anchorBody.transform.TransformPoint(new Vector3(9, 152.45f, 16));
        }
        else if (spawnChoice == SpawnChoice.BrittleHollow)
        {
            // unfortunately VisibleFrom_BH contains two children named Prefab_HEA_Campfire, so we have to use GetChild() to pick the correct one
            playerTargetGO = GameObject.Find("BrittleHollow_Body/Sector_BH/Sector_Crossroads/Interactables_Crossroads/VisibleFrom_BH").transform.GetChild(3).gameObject;
            anchorBody = Locator.GetAstroObject(AstroObject.Name.BrittleHollow).GetOWRigidbody();
            playerOffset = new Vector3(0, 0, -3);
            Quaternion shipOffsetAngle = new Quaternion(0f, -0.7933533f, 0f, 0.6087614f); // equivalent to Rotate(0, -105, 0)
            shipRotation = playerTargetGO.transform.rotation * shipOffsetAngle;
            shipPosition = anchorBody.transform.TransformPoint(new Vector3(-6, 15, 285));
        }
        else if (spawnChoice == SpawnChoice.GiantsDeep)
        {
            playerTargetGO = GameObject.Find("StatueIsland_Body");
            anchorBody = playerTargetGO.GetComponent<OWRigidbody>();
            playerOffset = new Vector3(0, 40, 30);
            shipRotation = playerTargetGO.transform.rotation;
            shipPosition = playerTargetGO.transform.TransformPoint(new Vector3(-30, 4f, -85));
        }
        else if (spawnChoice == SpawnChoice.Stranger)
        {
            playerTargetGO = GameObject.Find("RingWorld_Body/Sector_RingWorld/Sector_LightSideDockingBay/Geo_LightSideDockingBay/Structure_IP_Docking_Bay/DockingBay_Col");
            anchorBody = Locator.GetAstroObject(AstroObject.Name.RingWorld).GetComponent<OWRigidbody>();
            playerOffset = new Vector3(4, -11.75f, 25);
            shipRotation = playerTargetGO.transform.rotation;
            shipPosition = playerTargetGO.transform.TransformPoint(new Vector3(4, -12.25f, -5));
        }
        else throw new System.ArgumentException($"spawnChoice had an invalid value of {spawnChoice}");

        if (anchorBody != null && playerTargetGO != null)
        {
            Locator.GetPlayerCameraController().SetDegreesY(80f);
            var playerPos = playerTargetGO.transform.TransformPoint(playerOffset);

            OWRigidbody playerRigidBody = Locator.GetPlayerBody();
            playerRigidBody.WarpToPositionRotation(playerPos, playerTargetGO.transform.rotation);
            playerRigidBody.SetVelocity(anchorBody.GetVelocity());
            playerRigidBody.GetRequiredComponent<MatchInitialMotion>().SetBodyToMatch(anchorBody);

            OWRigidbody shipRigidBody = Locator.GetShipBody();
            shipRigidBody.WarpToPositionRotation(shipPosition, shipRotation);
            shipRigidBody.SetVelocity(anchorBody.GetVelocity());
            shipRigidBody.GetRequiredComponent<MatchInitialMotion>().SetBodyToMatch(anchorBody);
        }
    }

    // Hearing the TH Village music outside of TH is no big deal, but in many cases
    // it ends up overlapping and fighting with the music volumes on other planets,
    // so we only let it stay active if we're spawning on TH like the base game expects.
    [HarmonyPostfix, HarmonyPatch(typeof(VillageMusicVolume), nameof(VillageMusicVolume.Awake))]
    public static void VillageMusicVolume_Awake_Postfix(VillageMusicVolume __instance)
    {
        if (spawnChoice != SpawnChoice.Vanilla && spawnChoice != SpawnChoice.TimberHearth)
        {
            //APRandomizer.OWMLModConsole.WriteLine($"VillageMusicVolume_Awake_Postfix() calling this.Deactivate() since we aren't spawning on TH");
            __instance.Deactivate();
        }
    }

    // useful for testing
    /*[HarmonyPrefix, HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.Update))]
    public static void ToolModeUI_Update_Prefix()
    {
        var totalChoices = System.Enum.GetNames(typeof(SpawnChoice)).Length;
        if (OWInput.SharedInputManager.IsNewlyPressed(InputLibrary.up2))
        {
            spawnChoice = (SpawnChoice)(((int)spawnChoice + 1) % totalChoices);
            APRandomizer.OWMLModConsole.WriteLine($"spawnChoice changed to {spawnChoice}");
        }
        if (OWInput.SharedInputManager.IsNewlyPressed(InputLibrary.down2))
        {
            spawnChoice = (SpawnChoice)(((int)spawnChoice - 1) % totalChoices);
            APRandomizer.OWMLModConsole.WriteLine($"spawnChoice changed to {spawnChoice}");
        }
    }*/
}
