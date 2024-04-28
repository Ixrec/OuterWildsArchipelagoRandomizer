using HarmonyLib;
using System.Collections.Generic;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class EjectButton
{
    private static bool _hasEjectButton = false;

    public static bool hasEjectButton
    {
        get => _hasEjectButton;
        set
        {
            if (_hasEjectButton != value)
            {
                _hasEjectButton = value;
                ApplyHasEjectButtonFlag(_hasEjectButton);
                if (_hasEjectButton)
                {
                    var nd = new NotificationData(NotificationTarget.All, "SPACESHIP EJECT BUTTON COVER HAS BEEN UNSTUCK", 10);
                    NotificationManager.SharedInstance.PostNotification(nd, false);
                }
            }
        }
    }

    private static SingleInteractionVolume ejectButtonSIV = null;

    [HarmonyPostfix, HarmonyPatch(typeof(ShipEjectionSystem), nameof(ShipEjectionSystem.Start))]
    public static void ShipEjectionSystem_Start_Postfix(ShipEjectionSystem __instance)
    {
        ejectButtonSIV = __instance._interactVolume;

        ApplyHasEjectButtonFlag(_hasEjectButton);
    }

    [HarmonyPrefix, HarmonyPatch(typeof(ShipEjectionSystem), nameof(ShipEjectionSystem.OnPressInteract))]
    public static bool ShipEjectionSystem_OnPressInteract_Prefix(ShipEjectionSystem __instance)
    {
        if (!_hasEjectButton)
            APRandomizer.OWMLModConsole.WriteLine($"ShipEjectionSystem_OnPressInteract_Prefix blocking attempt to interact with the eject button");

        return _hasEjectButton; // if we have the AP item, allow the base game code to run, otherwise skip it
    }

    [HarmonyPostfix, HarmonyPatch(typeof(ShipEjectionSystem), nameof(ShipEjectionSystem.OnLoseFocus))]
    public static void ShipEjectionSystem_OnLoseFocus_Postfix(ShipEjectionSystem __instance)
    {
        if (!_hasEjectButton)
        {
            ejectButtonSIV?.ChangePrompt("Eject Button Cover is Stuck");
        }
    }

    public static void ApplyHasEjectButtonFlag(bool hasEjectButton)
    {
        if (ejectButtonSIV == null) return;

        if (hasEjectButton)
        {
            ejectButtonSIV.ChangePrompt(UITextType.ShipEjectPrompt);
            ejectButtonSIV.SetKeyCommandVisible(true);
        }
        else
        {
            ejectButtonSIV.ChangePrompt("Eject Button Cover is Stuck");
            ejectButtonSIV.SetKeyCommandVisible(false);
        }
    }
}
