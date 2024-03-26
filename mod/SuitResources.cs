using HarmonyLib;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class SuitResources
{
    // properties for `filler` refill items

    private static uint _fuelRefills = 0;

    public static uint fuelRefills
    {
        get => _fuelRefills;
        set
        {
            if (value > _fuelRefills)
                RefillFuel();

            _fuelRefills = value;
        }
    }
    private static uint _oxygenRefills = 0;

    public static uint oxygenRefills
    {
        get => _oxygenRefills;
        set
        {
            if (value > _oxygenRefills)
                RefillOxygen();

            _oxygenRefills = value;
        }
    }

    // private state, constants and reference-fetching setup patches to enable the real item implementations

    private static PlayerResources playerResources = null;

    [HarmonyPrefix, HarmonyPatch(typeof(PlayerResources), nameof(PlayerResources.Awake))]
    public static void PlayerResources_Awake(PlayerResources __instance)
    {
        playerResources = __instance;
    }

    // implementing the `filler` refill items

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

    // may be useful for testing even lower, logic-relevant oxygen/fuel/boost limits
    /*[HarmonyPrefix, HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.Update))]
    public static void ToolModeUI_Update_Prefix()
    {
        if (OWInput.SharedInputManager.IsNewlyPressed(InputLibrary.left2))
        {
            _oxygenCapacityUpgrades = (_oxygenCapacityUpgrades + 1) % 3;
            APRandomizer.OWMLWriteLine($"_oxygenCapacityUpgrades={_oxygenCapacityUpgrades}");
            ApplyMaxOxygen();
        }
        if (OWInput.SharedInputManager.IsNewlyPressed(InputLibrary.right2))
        {
            _fuelCapacityUpgrades = (_fuelCapacityUpgrades + 1) % 3;
            APRandomizer.OWMLWriteLine($"_fuelCapacityUpgrades={_fuelCapacityUpgrades}");
            ApplyMaxFuel();
        }
        if (OWInput.SharedInputManager.IsNewlyPressed(InputLibrary.down2))
        {
            _boostDurationUpgrades = (_boostDurationUpgrades + 1) % 3;
            APRandomizer.OWMLWriteLine($"_boostDurationUpgrades={_boostDurationUpgrades}");

            ApplyMaxBoost();
        }
    }*/
}
