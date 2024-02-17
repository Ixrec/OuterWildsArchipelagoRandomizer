using HarmonyLib;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class Jetpack
{
    private static uint _fuelRefills = 0;

    public static uint fuelRefills
    {
        get => _fuelRefills;
        set
        {
            if (value > _fuelRefills)
            {
                _fuelRefills = value;
                RefillFuel();
            }
        }
    }

    static PlayerResources playerResources = null;

    [HarmonyPrefix, HarmonyPatch(typeof(PlayerResources), nameof(PlayerResources.Awake))]
    public static void PlayerResources_Awake(PlayerResources __instance) => playerResources = __instance;

    private static void RefillFuel()
    {
        if (playerResources != null)
        {
            playerResources._currentFuel = PlayerResources._maxFuel;

            // Based on the parts of PlayerRecoveryPoint.OnPressInteract() and PlayerResources.StartRefillResources() that handle vanilla fuel-only refills
            // In vanilla this is a pinned notification, which doesn't fit suddenly receiving an AP item, so also based on the oxygen refill code.
            Locator.GetPlayerAudioController().PlayRefuel();
            var nd = new NotificationData(NotificationTarget.Player, UITextLibrary.GetString(UITextType.NotificationRefuel), 3f, false);
            NotificationManager.SharedInstance.PostNotification(nd, false);
        }
    }
}
