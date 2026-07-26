using HarmonyLib;

namespace ArchipelagoRandomizer.Compatibility.NomaiVR;

/// <summary>
/// Patches EnterLandingView to re-disable the landing cam. NomaiVR enables it, along with light and audio,
/// even if our Prefix in LandingCamera "disables" it.
/// </summary>
[HarmonyPatch]
public class VRLandingCamera
{
    [HarmonyPostfix, HarmonyPatch(typeof(ShipCockpitController), nameof(ShipCockpitController.EnterLandingView))]
    public static void ShipCockpitController_EnterLandingView_Postfix(ShipCockpitController __instance)
    {
        if (VRCompatibility.IsNomaiVRLoaded && !LandingCamera.hasLandingCamera && __instance._landingCam.enabled)
        {
            __instance._landingCam.enabled = false;
            __instance._landingLight.SetOn(false);
            __instance._shipAudioController.PlayLandingCamOff();
        }
    }
}
