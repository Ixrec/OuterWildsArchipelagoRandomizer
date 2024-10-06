using HarmonyLib;
using System.Collections.Generic;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class SurfaceIntegrity
{
    private static bool _hasSurfaceIntegrityScanner = false;

    public static bool hasSurfaceIntegrityScanner
    {
        get => _hasSurfaceIntegrityScanner;
        set
        {
            if (_hasSurfaceIntegrityScanner != value)
            {
                _hasSurfaceIntegrityScanner = value;
                ApplyHasSurfaceIntegrityScannerFlag(_hasSurfaceIntegrityScanner);
                if (_hasSurfaceIntegrityScanner && Scout.hasScout)
                {
                    var nd = new NotificationData(NotificationTarget.All, "SURFACE INTEGRITY SCANNER ADDED TO SCOUT", 10);
                    NotificationManager.SharedInstance.PostNotification(nd, false);
                }
            }
        }
    }

    private static ProbeAnchor probeAnchor = null;

    [HarmonyPostfix, HarmonyPatch(typeof(ProbeAnchor), nameof(ProbeAnchor.Awake))]
    public static void ProbeAnchor_Awake_Postfix(ProbeAnchor __instance) => probeAnchor = __instance;

    [HarmonyPrefix, HarmonyPatch(typeof(ProbeAnchor), nameof(ProbeAnchor.BuildIntegrityString))]
    public static bool ProbeAnchor_BuildIntegrityString_Prefix(ProbeAnchor __instance, ref string __result)
    {
        __result = string.Empty; // if we do skip the base game code, make sure the return value will be "" instead of null

        return _hasSurfaceIntegrityScanner; // if we have the AP item, allow the base game code to run, otherwise skip it
    }

    public static void ApplyHasSurfaceIntegrityScannerFlag(bool hasSurfaceIntegrityScanner)
    {
        if (hasSurfaceIntegrityScanner && probeAnchor != null)
        {
            string text = probeAnchor.BuildIntegrityString();
            if (text != probeAnchor._probeNotification.displayMessage)
                probeAnchor._probeNotification.displayMessage = text;
        }
    }
}
