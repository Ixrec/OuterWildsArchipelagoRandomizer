using HarmonyLib;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class ShipLights
{
    private static bool _hasLights = false;
    public static bool hasLights
    {
        get => _hasLights;
        set
        {
            if (_hasLights != value)
            {
                _hasLights = value;
                
                if (_hasLights && APRandomizer.APSession != null)
                {
                    var nd = new NotificationData(NotificationTarget.All, "SPACESHIP LIGHT BULBS INSTALLED", 10);
                    NotificationManager.SharedInstance.PostNotification(nd, false);
                    if (Locator.GetFlashlight() != null)
                        GlobalMessenger.AddListener("EnterShip", new Callback(Locator.GetFlashlight().TurnOff));
                    foreach (var light in lightObjects)
                        light?.SetActive(true);
                    if (PlayerState.IsInsideShip() && Locator.GetShipTransform() != null)
                        Locator.GetShipTransform().GetComponentInChildren<ShipCockpitController>().SetEnableShipLights(true, true);
                }
            }
        }
    }

    private static ScreenPrompt noLightsPrompt;
    private static List<GameObject> lightObjects;
    private static ShipLight headLight;
    private static bool landingCameraOverride = false;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipBody), nameof(ShipBody.Start))]
    public static void ShipBody_Start_Prefix(ShipBody __instance)
    {
        noLightsPrompt = new ScreenPrompt("Headlights Not Available");
        Locator.GetPromptManager().AddScreenPrompt(noLightsPrompt, PromptPosition.Center, false);
        lightObjects = [
                GameObject.Find("Ship_Body/Module_Cabin/Geo_Cabin/Cabin_Tech/Cabin_Tech_Exterior/BottomLantern"),
                GameObject.Find("Ship_Body/Module_Cabin/Geo_Cabin/Cabin_Tech/Cabin_Tech_Exterior/TopLantern"),
                GameObject.Find("Ship_Body/Module_Cabin/Geo_Cabin/Cabin_Tech/Cabin_Tech_Interior/Cabin_lightBulb"),
                GameObject.Find("Ship_Body/Module_Cabin/Geo_Cabin/Cabin_Tech/Cabin_Tech_Interior/CabinPoster_lightBulb"),
                GameObject.Find("Ship_Body/Module_Cabin/Lights_Cabin"),
                GameObject.Find("Ship_Body/Module_Cockpit/Geo_Cockpit/Cockpit_Geometry/Cockpit_Interior/cockpit_lightBulb"),
                GameObject.Find("Ship_Body/Module_Cockpit/Lights_Cockpit/Pointlight_HEA_ShipCockpit"), // Don't disable screen (landing camera more useful).
                GameObject.Find("Ship_Body/Module_Supplies/Geo_Supplies/Supplies_Tech/FloorLantern_lightBulb"),
                GameObject.Find("Ship_Body/Module_Supplies/Geo_Supplies/Supplies_Tech/PosterFrame_lightBulb"),
                GameObject.Find("Ship_Body/Module_Supplies/Geo_Supplies/Supplies_Tech/Supplies_lightBulb"),
                GameObject.Find("Ship_Body/Module_Supplies/Lights_Supplies"),
            ];
        if (!_hasLights)
            foreach (var light in lightObjects)
                light?.SetActive(false);
    }

    // Handle player trying to enable headlights.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipCockpitController), nameof(ShipCockpitController.UpdateShipLightInput))]
    public static bool ShipCockpitController_UpdateShipLightInput_Prefix(ShipCockpitController __instance)
    {
        if (OWInput.IsNewlyPressed(InputLibrary.flashlight, InputMode.All) && !__instance._shipSystemFailure)
        {
            if (_hasLights)
            {
                __instance._externalLightsOn = !__instance._externalLightsOn;
                __instance.SetEnableShipLights(__instance._externalLightsOn, true);
                string text = (__instance._externalLightsOn ? UITextLibrary.GetString(UITextType.NotificationShipLightsOn) : UITextLibrary.GetString(UITextType.NotificationShipLightsOff));
                NotificationData notificationData = new NotificationData(NotificationTarget.Ship, text, 5f, true);
                NotificationManager.SharedInstance.PostNotification(notificationData, false);
            }
            else
            {
                noLightsPrompt.SetVisibility(true);

                // Unfortunately the prompt manager has no delay features, so this is the only simple solution.
                Task.Run(async () =>
                {
                    await Task.Delay(3000);
                    noLightsPrompt.SetVisibility(false);
                });
            }
        }
        return false;
    }

    // Handle rest of the cases (light turning on automatically when getting into cockpit).
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipLight), nameof(ShipLight.SetOn))]
    public static void ShipLight_SetOn_Prefix(ref bool on)
    {
        on = on && (_hasLights || landingCameraOverride);
    }

    // Don't disable lights if using landing camera to make landing camera item more useful.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipCockpitController), nameof(ShipCockpitController.EnterLandingView))]
    public static void ShipCockpitController_EnterLandingView_Prefix()
    {
        if (LandingCamera.hasLandingCamera)
            landingCameraOverride = true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipCockpitController), nameof(ShipCockpitController.ExitLandingView))]
    public static void ShipCockpitController_ExitLandingView_Prefix()
    {
        landingCameraOverride = false;
    }

    // Remove QOL of player turning flashlight off automatically when entering the ship.
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Flashlight), nameof(Flashlight.Awake))]
    public static void Flashlight_Awake_Postfix(Flashlight __instance)
    {
        if (!_hasLights)
            GlobalMessenger.RemoveListener("EnterShip", new Callback(__instance.TurnOff));
    }
}
