using HarmonyLib;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class Oxygen
{
    private static uint _oxygenRefills = 0;

    public static uint oxygenRefills
    {
        get => _oxygenRefills;
        set
        {
            if (value > _oxygenRefills)
            {
                _oxygenRefills = value;
                RefillOxygen();
            }
        }
    }

    static PlayerResources playerResources = null;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerResources), nameof(PlayerResources.Awake))]
    public static void PlayerResources_Awake(PlayerResources __instance) => playerResources = __instance;

    private static void RefillOxygen()
    {
        if (playerResources != null)
        {
            playerResources._currentOxygen = PlayerResources._maxOxygen;

            // Based on the part of PlayerResources.UpdateOxygen() that handles vanilla refills
            Locator.GetPlayerAudioController().PlayRefillOxygen();
            var nd = new NotificationData(NotificationTarget.Player, UITextLibrary.GetString(UITextType.NotificationO2), 3f, false);
            NotificationManager.SharedInstance.PostNotification(nd, false);
        }
    }
}
