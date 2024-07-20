using HarmonyLib;

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
                }
            }
        }
    }

    private static SingleInteractionVolume shipCockpitSIV = null;

    [HarmonyPostfix, HarmonyPatch(typeof(ShipCockpitController), nameof(ShipCockpitController.Start))]
    public static void ShipCockpitController_Start_Postfix(ShipCockpitController __instance)
    {
        shipCockpitSIV = __instance._interactVolume;

        ApplyHasLaunchCodesFlag(_hasLaunchCodes);
    }

    [HarmonyPrefix, HarmonyPatch(typeof(ShipCockpitController), nameof(ShipCockpitController.OnPressInteract))]
    public static bool ShipCockpitController_OnPressInteract_Prefix(ShipCockpitController __instance)
    {
        if (!_hasLaunchCodes)
            APRandomizer.OWMLModConsole.WriteLine($"ShipCockpitController_OnPressInteract_Prefix blocking attempt to interact with the cockpit");

        return _hasLaunchCodes; // if we have the AP item, allow the base game code to run, otherwise skip it
    }

    public static void ApplyHasLaunchCodesFlag(bool hasLaunchCodes)
    {
        if (shipCockpitSIV == null) return;
        if (shipCockpitSIV._screenPrompt == null) return;

        if (hasLaunchCodes)
        {
            shipCockpitSIV.ChangePrompt(UITextType.ShipBuckleUpPrompt);
            shipCockpitSIV.SetKeyCommandVisible(true);
        }
        else
        {
            shipCockpitSIV.ChangePrompt("Launch Codes Required");
            shipCockpitSIV.SetKeyCommandVisible(false);
        }
    }
}
