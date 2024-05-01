using HarmonyLib;
using System.Collections.Generic;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class AutopilotManager
{
    private static bool _hasAutopilot = false;

    public static bool hasAutopilot
    {
        get => _hasAutopilot;
        set
        {
            if (_hasAutopilot != value)
            {
                _hasAutopilot = value;

                if (_hasAutopilot)
                {
                    var nd = new NotificationData(NotificationTarget.All, "SPACESHIP AUTOPILOT HAS BEEN REPAIRED", 10);
                    NotificationManager.SharedInstance.PostNotification(nd, false);
                }
            }
        }
    }

    static ScreenPrompt autopilotPrompt = null;
    static ScreenPrompt noAutopilotPrompt = null;

    [HarmonyPostfix, HarmonyPatch(typeof(ShipPromptController), nameof(ShipPromptController.LateInitialize))]
    public static void ShipPromptController_LateInitialize(ShipPromptController __instance)
    {
        autopilotPrompt = __instance._autopilotPrompt;

        noAutopilotPrompt = new ScreenPrompt("Autopilot Not Available", 0);
        Locator.GetPromptManager().AddScreenPrompt(noAutopilotPrompt, PromptPosition.UpperLeft, false);

        // Turns out the autopilot is part of the overall cockpit model, not a small object we can deactivate independently,
        // so there's no GameObject reference we can fetch here and deactivate.
    }

    [HarmonyPostfix, HarmonyPatch(typeof(ShipPromptController), nameof(ShipPromptController.Update))]
    public static void ShipPromptController_Update(ShipPromptController __instance)
    {
        noAutopilotPrompt.SetVisibility(false);
        if (autopilotPrompt.IsVisible() && !_hasAutopilot)
        {
            autopilotPrompt.SetVisibility(false);
            noAutopilotPrompt.SetVisibility(true);
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(Autopilot), nameof(Autopilot.FlyToDestination))]
    public static bool Autopilot_FlyToDestination_Prefix()
    {
        if (!_hasAutopilot)
            APRandomizer.OWMLModConsole.WriteLine($"Autopilot_FlyToDestination_Prefix blocking attempt to use autopilot");

        return _hasAutopilot; // if we have the AP item, allow the base game code to run, otherwise skip it
    }
}
