using HarmonyLib;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class LandingCamera
{
    private static bool _hasLandingCamera = false;

    public static bool hasLandingCamera
    {
        get => _hasLandingCamera;
        set
        {
            if (_hasLandingCamera != value)
            {
                _hasLandingCamera = value;
                if (_hasLandingCamera)
                {
                    var nd = new NotificationData(NotificationTarget.All, "SPACESHIP LANDING CAMERA HAS BEEN REPAIRED", 10);
                    NotificationManager.SharedInstance.PostNotification(nd, false);
                }
            }
        }
    }

    static ScreenPrompt landingCameraLiftoffPrompt = null; // "(X) Liftoff / Landing Camera" when the ship is on the ground
    static ScreenPrompt noLandingCameraLiftoffPrompt = null;

    static ScreenPrompt landingCameraLandingPrompt = null; // "(X) Landing Mode" when flying toward a planet
    static ScreenPrompt noLandingCameraLandingPrompt = null;

    [HarmonyPostfix, HarmonyPatch(typeof(ShipPromptController), nameof(ShipPromptController.LateInitialize))]
    public static void ShipPromptController_LateInitialize(ShipPromptController __instance)
    {
        landingCameraLandingPrompt = __instance._landingModePrompt;
        landingCameraLiftoffPrompt = __instance._liftoffCamera;

        noLandingCameraLandingPrompt = new ScreenPrompt("Landing Camera Not Available", 0);
        Locator.GetPromptManager().AddScreenPrompt(noLandingCameraLandingPrompt, PromptPosition.UpperLeft, false);
        noLandingCameraLiftoffPrompt = new ScreenPrompt("Landing Camera Not Available", 0);
        Locator.GetPromptManager().AddScreenPrompt(noLandingCameraLiftoffPrompt, PromptPosition.UpperLeft, false);

        // Turns out the landing camera is part of the landing gear model, not a small object we can deactivate independently,
        // so there's no GameObject reference we can fetch here and deactivate.
    }

    [HarmonyPostfix, HarmonyPatch(typeof(ShipPromptController), nameof(ShipPromptController.Update))]
    public static void ShipPromptController_Update_Postfix(ShipPromptController __instance)
    {
        noLandingCameraLandingPrompt.SetVisibility(false);
        if (landingCameraLandingPrompt.IsVisible() && !_hasLandingCamera)
        {
            landingCameraLandingPrompt.SetVisibility(false);
            noLandingCameraLandingPrompt.SetVisibility(true);
        }

        noLandingCameraLiftoffPrompt.SetVisibility(false);
        if (landingCameraLiftoffPrompt.IsVisible() && !_hasLandingCamera)
        {
            landingCameraLiftoffPrompt.SetVisibility(false);
            noLandingCameraLiftoffPrompt.SetVisibility(true);
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(ShipPromptController), nameof(ShipPromptController.HideAllPrompts))]
    public static void ShipPromptController_HideAllPrompts(ShipPromptController __instance)
    {
        noLandingCameraLandingPrompt.SetVisibility(false);
        noLandingCameraLiftoffPrompt.SetVisibility(false);
    }

    [HarmonyPrefix, HarmonyPatch(typeof(ShipCockpitController), nameof(ShipCockpitController.EnterLandingView))]
    public static bool ShipCockpitController_EnterLandingView_Prefix()
    {
        if (!_hasLandingCamera)
            APRandomizer.OWMLModConsole.WriteLine($"ShipCockpitController_EnterLandingView_Prefix blocking attempt to use the landing camera");

        return _hasLandingCamera; // if we have the AP item, allow the base game code to run, otherwise skip it
    }
}
