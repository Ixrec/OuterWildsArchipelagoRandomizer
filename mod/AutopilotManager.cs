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
                ApplyHasAutopilotFlag(_hasAutopilot);
                if (_hasAutopilot)
                {
                    var nd = new NotificationData(NotificationTarget.All, "SPACESHIP AUTOPILOT HAS BEEN REPAIRED", 10);
                    NotificationManager.SharedInstance.PostNotification(nd, false);
                }
            }
        }
    }

    static ScreenPrompt autopilotPrompt = null;

    // doing this earlier in Awake causes other methods to throw exceptions when the prompt unexpectedly has 0 buttons instead of 1
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipPromptController), nameof(ShipPromptController.LateInitialize))]
    public static void ShipPromptController_LateInitialize(ShipPromptController __instance)
    {
        autopilotPrompt = __instance._autopilotPrompt;

        // Turns out the autopilot is part of the overall cockpit model, not a small object we can deactivate independently,
        // so there's no GameObject reference we can fetch here and deactivate.

        ApplyHasAutopilotFlag(_hasAutopilot);
    }

    public static void ApplyHasAutopilotFlag(bool hasAutopilot)
    {
        if (autopilotPrompt is null) return;

        if (hasAutopilot)
        {
            autopilotPrompt._commandIdList = new List<InputConsts.InputCommandType> { InputLibrary.autopilot.CommandType };
            // copy-pasted from the body of ShipPromptController.Awake()
            autopilotPrompt.SetText("<CMD>   " + UITextLibrary.GetString(UITextType.ShipAutopilotPrompt));
        }
        else
        {
            autopilotPrompt._commandIdList = new();
            autopilotPrompt.SetText("Autopilot Not Available");
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Autopilot), nameof(Autopilot.FlyToDestination))]
    public static bool Autopilot_FlyToDestination_Prefix()
    {
        if (!_hasAutopilot)
            Randomizer.OWMLModConsole.WriteLine($"Autopilot_FlyToDestination_Prefix blocking attempt to use autopilot");

        return _hasAutopilot; // if we have the AP item, allow the base game code to run, otherwise skip it
    }
}
