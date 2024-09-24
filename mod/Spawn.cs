﻿using HarmonyLib;
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
            APRandomizer.OWMLModConsole.WriteLine($"Spawn::OnCompleteSceneLoad() ensuring that the time loop has started and the player will spawn in their suit");

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
            APRandomizer.OWMLModConsole.WriteLine($"executing instant SuitUp() due to spawnInSuitNextUpdate");
            Locator.GetPlayerSuit().SuitUp(isTrainingSuit: false, instantSuitUp: true, putOnHelmet: true);
            spawnInSuitNextUpdate = false;

            // hide the suit model inside the ship, so the player won't see a "duplicate" suit
            Spacesuit.SetSpacesuitVisible(false);
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(AlignPlayerWithForce), nameof(AlignPlayerWithForce.OnSuitUp))]
    public static bool AlignPlayerWithForce_OnSuitUp(AlignPlayerWithForce __instance)
    {
        if (spawnInSuitNextUpdate)
        {
            APRandomizer.OWMLModConsole.WriteLine($"skipping AlignPlayerWithForce::OnSuitUp() call so the player wakes up facing the sky despite wearing the spacesuit");
            return false;
        }
        //APRandomizer.OWMLModConsole.WriteLine($"normal AlignPlayerWithForce::OnSuitUp() call");
        return true;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(PlayerSpawner), nameof(PlayerSpawner.SpawnPlayer))]
    public static bool PlayerSpawner_SpawnPlayer(PlayerSpawner __instance)
    {
        if (!APRandomizer.IsVanillaSystemLoaded())
        {
            APRandomizer.OWMLModConsole.WriteLine($"PlayerSpawner_SpawnPlayer doing nothing, since we're not in the vanilla solar system");
            return true; // let vanilla impl run
        }
        if (spawnChoice == SpawnChoice.Vanilla || spawnChoice == SpawnChoice.TimberHearth)
        {
            APRandomizer.OWMLModConsole.WriteLine($"PlayerSpawner_SpawnPlayer doing nothing, since we're spawning in TH village");
            return true; // let vanilla impl run
        }
        else if (spawnChoice == SpawnChoice.HourglassTwins)
        {
            var chertCampfireGO = GameObject.Find("CaveTwin_Body/Sector_CaveTwin/Sector_NorthHemisphere/Sector_NorthSurface/Sector_Lakebed/Interactables_Lakebed/Lakebed_VisibleFrom_Far/Prefab_HEA_Campfire");
            var emberTwinOWRB = Locator.GetAstroObject(AstroObject.Name.CaveTwin).GetOWRigidbody();
            OWRigidbody playerRigidBody = Locator.GetPlayerBody();
            OWRigidbody shipRigidBody = Locator.GetShipBody();

            var offsetFromCampfire = new Vector3(3, 0, -3);
            var playerPos = chertCampfireGO.transform.TransformPoint(offsetFromCampfire);
            playerRigidBody.WarpToPositionRotation(playerPos, chertCampfireGO.transform.rotation);
            Locator.GetPlayerCameraController().SetDegreesY(80f);

            var offsetFromPlanet = new Vector3(9, 152.45f, 16);
            var shipPos = emberTwinOWRB.transform.TransformPoint(offsetFromPlanet);
            shipRigidBody.WarpToPositionRotation(shipPos, emberTwinOWRB.transform.rotation);

            playerRigidBody.SetVelocity(emberTwinOWRB.GetVelocity());
            playerRigidBody.GetRequiredComponent<MatchInitialMotion>().SetBodyToMatch(emberTwinOWRB);
            shipRigidBody.SetVelocity(emberTwinOWRB.GetVelocity());
            shipRigidBody.GetRequiredComponent<MatchInitialMotion>().SetBodyToMatch(emberTwinOWRB);
            return false;
        }
        else if (spawnChoice == SpawnChoice.BrittleHollow)
        {
            // unfortunately VisibleFrom_BH contains two children named Prefab_HEA_Campfire, so we have to use GetChild() to pick the correct one
            var riebeckOldCampfireGO = GameObject.Find("BrittleHollow_Body/Sector_BH/Sector_Crossroads/Interactables_Crossroads/VisibleFrom_BH").transform.GetChild(3);
            var brittleHollowOWRB = Locator.GetAstroObject(AstroObject.Name.BrittleHollow).GetOWRigidbody();
            OWRigidbody playerRigidBody = Locator.GetPlayerBody();
            OWRigidbody shipRigidBody = Locator.GetShipBody();

            var offsetFromCampfire = new Vector3(0, 0, -3);
            var playerPos = riebeckOldCampfireGO.transform.TransformPoint(offsetFromCampfire);
            playerRigidBody.WarpToPositionRotation(playerPos, riebeckOldCampfireGO.transform.rotation);
            Locator.GetPlayerCameraController().SetDegreesY(80f);

            var offsetFromPlanet = new Vector3(-6, 10, 285);
            var shipPos = brittleHollowOWRB.transform.TransformPoint(offsetFromPlanet);
            shipRigidBody.WarpToPositionRotation(shipPos, riebeckOldCampfireGO.transform.rotation);

            playerRigidBody.SetVelocity(brittleHollowOWRB.GetVelocity());
            playerRigidBody.GetRequiredComponent<MatchInitialMotion>().SetBodyToMatch(brittleHollowOWRB);
            shipRigidBody.SetVelocity(brittleHollowOWRB.GetVelocity());
            shipRigidBody.GetRequiredComponent<MatchInitialMotion>().SetBodyToMatch(brittleHollowOWRB);
            return false;
        }
        else if (spawnChoice == SpawnChoice.GiantsDeep)
        {
            var statueIslandGO = GameObject.Find("StatueIsland_Body");
            var statueIslandOWRB = statueIslandGO.GetComponent<OWRigidbody>();
            OWRigidbody playerRigidBody = Locator.GetPlayerBody();
            OWRigidbody shipRigidBody = Locator.GetShipBody();

            var playerPos = statueIslandGO.transform.TransformPoint(new Vector3(0, 40, 30));
            playerRigidBody.WarpToPositionRotation(playerPos, statueIslandGO.transform.rotation);
            Locator.GetPlayerCameraController().SetDegreesY(80f);

            var shipPos = statueIslandGO.transform.TransformPoint(new Vector3(-30, 4f, -85));
            shipRigidBody.WarpToPositionRotation(shipPos, statueIslandGO.transform.rotation);

            playerRigidBody.SetVelocity(statueIslandOWRB.GetVelocity());
            playerRigidBody.GetRequiredComponent<MatchInitialMotion>().SetBodyToMatch(statueIslandOWRB);
            shipRigidBody.SetVelocity(statueIslandOWRB.GetVelocity());
            shipRigidBody.GetRequiredComponent<MatchInitialMotion>().SetBodyToMatch(statueIslandOWRB);
            return false;
        }
        else if (spawnChoice == SpawnChoice.Stranger)
        {
            var sunsideHangarGO = GameObject.Find("RingWorld_Body/Sector_RingWorld/Sector_LightSideDockingBay/Geo_LightSideDockingBay/Structure_IP_Docking_Bay/DockingBay_Col");
            var ringWorldOWRB = Locator.GetAstroObject(AstroObject.Name.RingWorld).GetComponent<OWRigidbody>();
            OWRigidbody playerRigidBody = Locator.GetPlayerBody();
            OWRigidbody shipRigidBody = Locator.GetShipBody();

            var playerPos = sunsideHangarGO.transform.TransformPoint(new Vector3(4, -11.75f, 25));
            playerRigidBody.WarpToPositionRotation(playerPos, sunsideHangarGO.transform.rotation);
            Locator.GetPlayerCameraController().SetDegreesY(80f);

            var shipPos = sunsideHangarGO.transform.TransformPoint(new Vector3(4, -12.25f, -5));
            shipRigidBody.WarpToPositionRotation(shipPos, sunsideHangarGO.transform.rotation);

            playerRigidBody.SetVelocity(ringWorldOWRB.GetVelocity());
            playerRigidBody.GetRequiredComponent<MatchInitialMotion>().SetBodyToMatch(ringWorldOWRB);
            shipRigidBody.SetVelocity(ringWorldOWRB.GetVelocity());
            shipRigidBody.GetRequiredComponent<MatchInitialMotion>().SetBodyToMatch(ringWorldOWRB);
            return false;
        }
        else throw new System.ArgumentException($"spawnChoice had an invalid value of {spawnChoice}");
    }

    // Hearing the TH Village music outside of TH is no big deal, but in many cases
    // it ends up overlapping and fighting with the music volumes on other planets,
    // so we only let it stay active if we're spawning on TH like the base game expects.
    [HarmonyPostfix, HarmonyPatch(typeof(VillageMusicVolume), nameof(VillageMusicVolume.Awake))]
    public static void VillageMusicVolume_Awake_Postfix(VillageMusicVolume __instance)
    {
        if (spawnChoice != SpawnChoice.Vanilla && spawnChoice != SpawnChoice.TimberHearth)
        {
            APRandomizer.OWMLModConsole.WriteLine($"VillageMusicVolume_Awake_Postfix() calling this.Deactivate() since we aren't spawning on TH");
            __instance.Deactivate();
        }
    }

    // useful for testing
    /*[HarmonyPrefix, HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.Update))]
    public static void ToolModeUI_Update_Prefix()
    {
        if (OWInput.SharedInputManager.IsNewlyPressed(InputLibrary.up2))
        {
            spawnChoice = (SpawnChoice)(((int)spawnChoice + 1) % 5);
            APRandomizer.OWMLModConsole.WriteLine($"spawnChoice changed to {spawnChoice}");
        }
        if (OWInput.SharedInputManager.IsNewlyPressed(InputLibrary.down2))
        {
            spawnChoice = (SpawnChoice)(((int)spawnChoice - 1) % 5);
            APRandomizer.OWMLModConsole.WriteLine($"spawnChoice changed to {spawnChoice}");
        }
    }*/
}
