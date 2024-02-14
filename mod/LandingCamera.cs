using HarmonyLib;
using System.Collections.Generic;

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
                ApplyHasLandingCameraFlag(_hasLandingCamera);
                if (_hasLandingCamera)
                {
                    var nd = new NotificationData(NotificationTarget.All, "SPACESHIP LANDING CAMERA HAS BEEN REPAIRED", 10);
                    NotificationManager.SharedInstance.PostNotification(nd, false);
                }
            }
        }
    }

    static ScreenPrompt landingCameraLiftoffPrompt = null; // "(X) Liftoff / Landing Camera" when the ship is on the ground
    static ScreenPrompt landingCameraLandingPrompt = null; // "(X) Landing Mode" when flying toward a planet

    // doing this earlier in Awake causes other methods to throw exceptions when the prompt unexpectedly has 0 buttons instead of 1
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipPromptController), nameof(ShipPromptController.LateInitialize))]
    public static void ShipPromptController_LateInitialize(ShipPromptController __instance)
    {
        landingCameraLandingPrompt = __instance._landingModePrompt;
        landingCameraLiftoffPrompt = __instance._liftoffCamera;

        // Turns out the landing camera is part of the landing gear model, not a small object we can deactivate independently,
        // so there's no GameObject reference we can fetch here and deactivate.

        ApplyHasLandingCameraFlag(_hasLandingCamera);
    }

    public static void ApplyHasLandingCameraFlag(bool hasLandingCamera)
    {
        if (landingCameraLandingPrompt == null) return;

        if (hasLandingCamera)
        {
            landingCameraLandingPrompt._commandIdList = new List<InputConsts.InputCommandType> { InputLibrary.landingCamera.CommandType };
            landingCameraLiftoffPrompt._commandIdList = new List<InputConsts.InputCommandType> { InputLibrary.landingCamera.CommandType };
            // copy-pasted from the body of ShipPromptController.Awake()
            landingCameraLandingPrompt.SetText("<CMD>   " + UITextLibrary.GetString(UITextType.ShipLandingPrompt));
            landingCameraLiftoffPrompt.SetText("<CMD>   " + UITextLibrary.GetString(UITextType.ShipLiftoffLandingPrompt));
        }
        else
        {
            landingCameraLandingPrompt._commandIdList = new();
            landingCameraLiftoffPrompt._commandIdList = new();
            landingCameraLandingPrompt.SetText("Landing Camera Not Available");
            landingCameraLiftoffPrompt.SetText("Landing Camera Not Available");
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipCockpitController), nameof(ShipCockpitController.EnterLandingView))]
    public static bool ShipCockpitController_EnterLandingView_Prefix()
    {
        if (!_hasLandingCamera)
            APRandomizer.OWMLModConsole.WriteLine($"ShipCockpitController_EnterLandingView_Prefix blocking attempt to use the landing camera");

        return _hasLandingCamera; // if we have the AP item, allow the base game code to run, otherwise skip it
    }
}
