using HarmonyLib;
using UnityEngine;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class LaunchCodes
{
    private static bool _hasLaunchCodes = false;

    public static bool hasLaunchCodes
    {
        get => _hasLaunchCodes;
        set
        {
            if (_hasLaunchCodes != value)
            {
                _hasLaunchCodes = value;

                if (_hasLaunchCodes)
                {
                    var nd = new NotificationData(NotificationTarget.All, "LAUNCH CODES VERIFIED. SPACESHIP FLIGHT CONTROLS UNLOCKED.", 10);
                    NotificationManager.SharedInstance.PostNotification(nd, false);
                    if (shipCockpit != null)
                    {
                        shipCockpit._controlsUnlockTime = Time.time;
                    }
                }
            }
        }
    }

    private static ShipCockpitController shipCockpit = null;

    [HarmonyPostfix, HarmonyPatch(typeof(ShipCockpitController), nameof(ShipCockpitController.Start))]
    public static void ShipCockpitController_Start_Postfix(ShipCockpitController __instance)
    {
        shipCockpit = __instance;
        if (!hasLaunchCodes)
        {
            __instance._controlsUnlockTime = Time.time + 1000000f;
            __instance._controlsLocked = true;
        }
    }
}
