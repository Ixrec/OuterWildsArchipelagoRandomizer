using HarmonyLib;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class MedkitAndJetpackFuel
{
    private static bool _hasMedkit = false;

    public static bool hasMedkit
    {
        get => _hasMedkit;
        set
        {
            if (_hasMedkit != value)
            {
                _hasMedkit = value;
                if (_hasMedkit)
                {
                    var nd = new NotificationData(NotificationTarget.All, "SPACESHIP MEDKIT HAS BEEN REPAIRED", 10);
                    NotificationManager.SharedInstance.PostNotification(nd, false);
                }
            }
        }
    }

    private static bool _hasJetpackFuel = false;

    public static bool hasJetpackFuel
    {
        get => _hasJetpackFuel;
        set
        {
            if (_hasJetpackFuel != value)
            {
                _hasJetpackFuel = value;
                if (_hasJetpackFuel)
                {
                    var nd = new NotificationData(NotificationTarget.All, "SPACESHIP JETPACK FUEL TANK HAS BEEN FILLED", 10);
                    NotificationManager.SharedInstance.PostNotification(nd, false);
                }
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerRecoveryPoint), nameof(PlayerRecoveryPoint.OnGainFocus))]
    public static bool PlayerRecoveryPoint_OnGainFocus_Prefix(PlayerRecoveryPoint __instance)
    {
        if (!__instance.GetComponentInParent<ShipBody>())
            return true;

        __instance._healsPlayer = _hasMedkit;
        __instance._refuelsPlayer = _hasJetpackFuel;
        if (_hasMedkit && _hasJetpackFuel)
            return true;

        if (__instance._playerResources == null)
            __instance._playerResources = Locator.GetPlayerTransform().GetComponent<PlayerResources>();
        bool needMedkit = __instance._playerResources.GetHealthFraction() < 1f;
        bool needFuel = __instance._playerResources.GetFuelFraction() < 1f;

        var medkitText = !_hasMedkit ? "Medkit is Broken" : 
            (needMedkit ? "Use Medkit" : "Health Full");
        var refuelText = !_hasJetpackFuel ? "Fuel Tank is Empty" : 
            (needFuel ? "Refill Jetpack" : "Jetpack Full");

        __instance._interactVolume.SetKeyCommandVisible(_hasMedkit && needMedkit || _hasJetpackFuel && needFuel);
        __instance._interactVolume.ChangePrompt($"{medkitText}\n{refuelText}");
        return false;
    }
}
