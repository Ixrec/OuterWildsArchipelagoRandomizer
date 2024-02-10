using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class Anglerfish
{
    private static bool _hasAnglerfishKnowledge = false;

    public static bool hasAnglerfishKnowledge
    {
        get => _hasAnglerfishKnowledge;
        set
        {
            if (_hasAnglerfishKnowledge != value)
            {
                _hasAnglerfishKnowledge = value;

                if (_hasAnglerfishKnowledge)
                {
                    var nd = new NotificationData(NotificationTarget.All, "RECONFIGURING SPACESHIP FOR SILENT RUNNING MODE", 10);
                    NotificationManager.SharedInstance.PostNotification(nd, false);
                }
            }
        }
    }

    static bool shipMakingNoise = false;
    static bool playerMakingNoise = false;

    // In the vanilla game, _noiseRadius is always between 0 and 400.
    // In testing, 200 seemed like an ideal distance for this because it allows you to think you
    // might make it, but it's still too high to survive the 3 fish at the start of fog zone 2.
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipNoiseMaker), nameof(ShipNoiseMaker.Update))]
    public static void ShipNoiseMaker_Update_Postfix(ref ShipNoiseMaker __instance)
    {
        if (!hasAnglerfishKnowledge)
            __instance._noiseRadius = Math.Max(__instance._noiseRadius, 200);

        var newShipNoise = __instance._noiseRadius > 0;
        if (newShipNoise != shipMakingNoise)
        {
            shipMakingNoise = newShipNoise;
            UpdatePromptText();
        }
    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerNoiseMaker), nameof(PlayerNoiseMaker.Update))]
    public static void PlayerNoiseMaker_Update_Postfix(ref PlayerNoiseMaker __instance)
    {
        if (!hasAnglerfishKnowledge)
            __instance._noiseRadius = Math.Max(__instance._noiseRadius, 200);

        var newPlayerNoise = __instance._noiseRadius > 0;
        if (newPlayerNoise != playerMakingNoise)
        {
            playerMakingNoise = newPlayerNoise;
            UpdatePromptText();
        }
    }

    static string activeText = "Silent Running Mode: <color=green>Active</color>";
    static string inactiveText = "Silent Running Mode: <color=red>Inactive</color>";
    static ScreenPrompt silentRunningPrompt = new(activeText, 0);

    public static void UpdatePromptText()
    {
        if (shipMakingNoise || playerMakingNoise)
            silentRunningPrompt.SetText(inactiveText);
        else
            silentRunningPrompt.SetText(activeText);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.LateInitialize))]
    public static void ToolModeUI_LateInitialize_Postfix()
    {
        Locator.GetPromptManager().AddScreenPrompt(silentRunningPrompt, PromptPosition.UpperRight, false);
    }
    [HarmonyPrefix]
    [HarmonyPatch(typeof(AnglerfishController), nameof(AnglerfishController.ApplyDrag))]
    public static bool AnglerfishController_ApplyDrag(AnglerfishController __instance)
    {
        return false; // no drag
    }
    [HarmonyPrefix]
    [HarmonyPatch(typeof(AlignWithDirection), nameof(AlignWithDirection.InitAlignment))]
    public static void AlignWithDirection_InitAlignment(AlignWithDirection __instance)
    {
        Randomizer.OWMLModConsole.WriteLine($"AlignWithDirection.InitAlignment {__instance.name}");
    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(AnglerfishController), nameof(AnglerfishController.UpdateMovement))]
    public static void AnglerfishController_UpdateMovement(AnglerfishController __instance)
    {
        if (__instance._currentState != AnglerfishController.AnglerState.Lurking)
            Randomizer.OWMLModConsole.WriteLine($"AnglerfishController_UpdateMovement {__instance.gameObject.name} {__instance._currentState} {__instance._localDisturbancePos}");
    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.Update))]
    public static void ToolModeUI_Update_Postfix()
    {
        if (OWInput.IsNewlyPressed(InputLibrary.down2))
        {
            Randomizer.OWMLModConsole.WriteLine($"down2");

            // The following borrows heavily from https://github.com/Outer-Wilds-New-Horizons/new-horizons/ 's DetailBuilder.cs

            var th = Locator.GetAstroObject(AstroObject.Name.TimberHearth).gameObject;
            var thSector = SectorManager.s_sectors.Find(s => s._name == Sector.Name.TimberHearth);
            var thSpeed = OWPhysics.CalculateOrbitVelocity(th.GetAttachedOWRigidbody(), th.GetComponent<AstroObject>().GetPrimaryBody().GetAttachedOWRigidbody()).magnitude;
            var thRigidbody = Locator.GetAstroObject(AstroObject.Name.TimberHearth).GetOWRigidbody();

            var dbFish = GameObject.Find("DB_HubDimension_Body/Sector_HubDimension/Interactables_HubDimension/Anglerfish_Body");

            List<string> assetBundlesList = new();
            foreach (var streamingHandle in dbFish.GetComponentsInChildren<StreamingMeshHandle>())
            {
                var assetBundle = streamingHandle.assetBundle;
                assetBundlesList.SafeAdd(assetBundle);
                // the full NH code also checks for streaming *materials* here, but anglerfish have no materials I guess
            }

            Randomizer.OWMLModConsole.WriteLine($"calling LoadStreamingAssets() on {string.Join(", ", assetBundlesList)}");
            foreach (var b in assetBundlesList)
                StreamingManager.LoadStreamingAssets(b);

            Randomizer.OWMLModConsole.WriteLine($"spawning fish {dbFish} under {th.name}");
            var fish = GameObject.Instantiate(dbFish);
            fish.name = "TestFish1";
            fish.transform.SetParent(th.transform, false);
            fish.transform.position = th.transform.position + new Vector3(500, 0, 0);
            fish.transform.localScale = new Vector3(1, 1, 1);
            var ac = fish.GetComponent<AnglerfishController>();
            ac.SetSector(null);
            ac._chaseSpeed += thSpeed;
            var rb = fish.GetComponent<Rigidbody>();
            rb.isKinematic = false;
            var owrb = fish.GetComponent<OWRigidbody>();
            owrb.enabled = true;
            //owrb.SetVelocity(thRigidbody.GetVelocity());
            fish.GetComponent<CenterOfTheUniverseOffsetApplier>().enabled = true;
            fish.GetComponentInChildren<SkinnedMeshRenderer>().enabled = true;

            fish.SetActive(true);
            owrb.Unsuspend();
            //this.OnAnglerUnsuspended(this._currentState)
            const BindingFlags flags = BindingFlags.Instance
                | BindingFlags.Static
                | BindingFlags.Public
                | BindingFlags.NonPublic
                | BindingFlags.DeclaredOnly;
            if (typeof(AnglerfishController)
                    .GetField("OnAnglerUnsuspended", flags)?
                    .GetValue(ac) is not MulticastDelegate multiDelegate)
                return;
            multiDelegate.DynamicInvoke([ac._currentState]);

            // the "corrected" fish that's not glued to the player has:
            // _restoreCachedVelocityOnUnsuspend = true
            // OnSuspendOWRigidbody / OnUnsuspendOWRigidbody have handlers
            // _cachedRelativeVelocity is nonzero
            // _childColliders is nonempty
            //var awtb = fish.AddComponent<AlignWithTargetBody>();
            //awtb.SetTargetBody(thRigidbody);

            fish = GameObject.Instantiate(dbFish);
            fish.name = "TestFish2";
            fish.transform.SetParent(th.transform, false);
            fish.transform.position = th.transform.position + new Vector3(0, 500, 0);
            fish.transform.localScale = new Vector3(1, 1, 1);
            ac = fish.GetComponent<AnglerfishController>();
            ac._sector = thSector;
            ac._chaseSpeed += thSpeed;
            ac.OnSectorOccupantsUpdated();
            rb = fish.GetComponent<Rigidbody>();
            rb.isKinematic = false;
            fish.GetComponent<OWRigidbody>().enabled = true;
            fish.GetComponent<CenterOfTheUniverseOffsetApplier>().enabled = true;
            fish.GetComponentInChildren<SkinnedMeshRenderer>().enabled = true;

            fish = GameObject.Instantiate(dbFish);
            fish.name = "TestFish3";
            fish.transform.SetParent(th.transform, false);
            fish.transform.position = th.transform.position + new Vector3(0, 0, 500);
            fish.transform.localScale = new Vector3(1, 1, 1);
            ac = fish.GetComponent<AnglerfishController>();
            ac._sector = thSector;
            ac._chaseSpeed += thSpeed;
            ac.OnSectorOccupantsUpdated();
            rb = fish.GetComponent<Rigidbody>();
            rb.isKinematic = false;
            fish.GetComponent<OWRigidbody>().enabled = true;
            fish.GetComponent<CenterOfTheUniverseOffsetApplier>().enabled = true;
            fish.GetComponentInChildren<SkinnedMeshRenderer>().enabled = true;

            /*fish = GameObject.Instantiate(dbFish);
            fish.name = "TestFish";
            fish.transform.SetParent(th.transform, false);
            fish.transform.position = new Vector3(300, 300, 0);
            fish.transform.localScale = new Vector3(1, 1, 1);
            fish.SetActive(true);

            fish = GameObject.Instantiate(dbFish);
            fish.name = "TestFish";
            fish.transform.SetParent(th.transform, false);
            fish.transform.position = new Vector3(0, 300, 300);
            fish.transform.localScale = new Vector3(1, 1, 1);
            fish.SetActive(true);

            fish = GameObject.Instantiate(dbFish);
            fish.name = "TestFish";
            fish.transform.SetParent(th.transform, false);
            fish.transform.position = new Vector3(300, 0, 300);
            fish.transform.localScale = new Vector3(1, 1, 1);
            fish.SetActive(true);*/

            /*var ship = Locator.GetShipBody().gameObject;
            Randomizer.OWMLModConsole.WriteLine($"spawning fish {dbFish} under {ship.name}");
            var fish2 = GameObject.Instantiate(dbFish);
            fish2.name = "TestFishShip";
            fish2.transform.SetParent(ship.transform, false);
            fish.transform.position = new Vector3(0, 0, 0);
            fish.transform.localScale = new Vector3(1, 1, 1);
            fish.SetActive(true);*/
        }

        silentRunningPrompt.SetVisibility(
            hasAnglerfishKnowledge &&
            (OWInput.IsInputMode(InputMode.Character) || OWInput.IsInputMode(InputMode.ShipCockpit)) &&
            (
                Locator.GetPlayerSectorDetector().IsWithinSector(Sector.Name.DarkBramble) ||
                Locator.GetPlayerSectorDetector().IsWithinSector(Sector.Name.BrambleDimension)
            )
        );
    }
}
