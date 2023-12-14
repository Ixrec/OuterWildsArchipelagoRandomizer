using HarmonyLib;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArchipelagoRandomizer;

// In the vanilla game, "have I scanned X with the Signalscope?" and
// "do I know X's frequency + signal?" are always both true or both false.
// To randomize frequencies and signals, we need to separate those two concepts.
//
// We use the term "scanned" for frequencies and signals that the player has pointed the
// Signalscope at and are stored in the vanilla save file.
// We use the term "usable" for frequencies and signals that the player has received the
// Archipelago items for, and thus our mod wants to allow the use of.

[HarmonyPatch]
internal class Signalscope
{
    public static bool hasSignalscope = false;

    public static void SetHasSignalscope(bool hasSignalscope)
    {
        if (Signalscope.hasSignalscope != hasSignalscope)
        {
            Signalscope.hasSignalscope = hasSignalscope;
            ApplyHasSignalscopeFlag(hasSignalscope);
        }
    }

    // The signalscope is the only tool in vanilla which you can equip without even wearing the suit, and thus
    // by far the easiest tool to try equipping without any "[Y] Equip Signalscope" prompt for me to edit.

    // So this "duplicate" prompt is for when the player presses Y and I know there won't be an existing prompt about it.
    static ScreenPrompt signalscopeNotAvailablePrompt = new ScreenPrompt("Signalscope Not Available", 0);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.LateInitialize))]
    public static void ToolModeUI_LateInitialize_Postfix()
    {
        Locator.GetPromptManager().AddScreenPrompt(signalscopeNotAvailablePrompt, PromptPosition.Center, false);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ToolModeSwapper), nameof(ToolModeSwapper.EquipToolMode))]
    public static bool ToolModeSwapper_EquipToolMode_Prefix(ToolMode mode)
    {
        if (mode == ToolMode.SignalScope && !hasSignalscope)
        {
            Randomizer.Instance.ModHelper.Console.WriteLine($"blocked attempt to equip Signalscope");

            if (!Locator.GetPlayerSuit().IsWearingSuit() && !OWInput.IsInputMode(InputMode.ShipCockpit))
            {
                signalscopeNotAvailablePrompt.SetVisibility(true);

                // not the most robust delay code, but this is already a corner case and
                // the prompt manager has no delay features, so not worth investing in this
                Task.Run(async () => {
                    await Task.Delay(3000);
                    signalscopeNotAvailablePrompt.SetVisibility(false);
                });
            }

            return false;
        }

        return true;
    }

    // These are the Signalscope prompts which do exist in the vanilla game for me to edit.

    static ScreenPrompt equipSignalscopePrompt = null;
    static ScreenPrompt centerEquipSignalScopePrompt = null; // only shown in specific places, e.g. hide & seek

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.Start))]
    public static void ToolModeUI_Start_Postfix(ToolModeUI __instance)
    {
        equipSignalscopePrompt = __instance._signalscopePrompt;
        centerEquipSignalScopePrompt = __instance._centerSignalscopePrompt;

        ApplyHasSignalscopeFlag(hasSignalscope);
    }

    public static void ApplyHasSignalscopeFlag(bool hasSignalscope)
    {
        if (equipSignalscopePrompt is null || centerEquipSignalScopePrompt is null) return;

        if (hasSignalscope)
        {
            equipSignalscopePrompt._commandIdList = new List<InputConsts.InputCommandType> { InputLibrary.signalscope.CommandType };
            centerEquipSignalScopePrompt._commandIdList = new List<InputConsts.InputCommandType> { InputLibrary.signalscope.CommandType };
            // copy-pasted from the body of ToolModeUI.Start()
            equipSignalscopePrompt.SetText(UITextLibrary.GetString(UITextType.SignalscopePrompt) + "   <CMD>");
            centerEquipSignalScopePrompt.SetText(UITextLibrary.GetString(UITextType.SignalscopePrompt) + "   <CMD>");
        }
        else
        {
            equipSignalscopePrompt._commandIdList = new();
            centerEquipSignalScopePrompt._commandIdList = new();
            equipSignalscopePrompt.SetText("Signalscope Not Available");
            centerEquipSignalScopePrompt.SetText("Signalscope Not Available");
        }
    }

    // The rest of this code deals with the Frequency and Signal items

    public static HashSet<SignalFrequency> usableFrequencies = new HashSet<SignalFrequency> { SignalFrequency.Traveler };
    public static HashSet<SignalName> usableSignals = new();
    public static void SetFrequencyUsable(SignalFrequency frequency, bool usable)
    {
        if (usable) usableFrequencies.Add(frequency); else usableFrequencies.Remove(frequency);
    }
    public static void SetSignalUsable(SignalName signal, bool usable)
    {
        if (usable) usableSignals.Add(signal); else usableSignals.Remove(signal);
    }

    // PlayerData._currentGameSave.knownFrequencies and .knownSignals are where the scanned items are stored.
    // We want to keep allowing the game to write to those parts of the save file when the player scans
    // objects, while intercepting all reads so that we can make the game act as if the player knows a
    // completely different set of frequencies and signals from the ones they've scanned in-game.

    // Aside from LearnFrequency/Signal() which we still want writing to the player's save file,
    // these 4 methods are all the direct reads and writes of .knownFrequencies/Signals
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerData), nameof(PlayerData.KnowsFrequency))]

    public static bool PlayerData_KnowsFrequency_Prefix(SignalFrequency frequency, ref bool __result)
    {
        __result = usableFrequencies.Contains(frequency); // override return value
        return false; // skip vanilla implementation
    }
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerData), nameof(PlayerData.KnowsMultipleFrequencies))]
    public static bool PlayerData_KnowsMultipleFrequencies_Prefix(ref bool __result)
    {
        __result = usableFrequencies.Count > 1; // override return value
        return false; // skip vanilla implementation
    }
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerData), nameof(PlayerData.KnowsSignal))]
    public static bool PlayerData_KnowsSignal_Prefix(SignalName signalName, ref bool __result)
    {
        __result = usableSignals.Contains(signalName); // override return value
        return false; // skip vanilla implementation
    }
    // In vanilla, this is used to forget the Hide & Seek frequency
    // after each loop. But we never want to forget anything.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerData), nameof(PlayerData.ForgetFrequency))]
    public static bool PlayerData_ForgetFrequency_Prefix(SignalFrequency frequency)
    {
        Randomizer.Instance.ModHelper.Console.WriteLine($"preventing PlayerData.ForgetFrequency({frequency})");
        return false; // skip vanilla implementation, never forget a frequency
    }

    // The above patches are enough to separate signalscope scans from signalscope knowledge,
    // but we can do better.

    // The first UX problem I want to solve is that scanning a signal source puts the game
    // in an infinite loop of relearning it, complete with annoying eternal beeping.
    // That's what these next two patches are for:

    // If the player has scanned this frequency/signal before, and the vanilla code is calling
    // these methods only because they haven't received the AP item for that frequency/signal yet,
    // then don't "relearn" it.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(AudioSignal), nameof(AudioSignal.IdentifyFrequency))]
    public static bool AudioSignal_IdentifyFrequency_Prefix(AudioSignal __instance)
    {
        if (PlayerData._currentGameSave.knownFrequencies[AudioSignal.FrequencyToIndex(__instance.GetFrequency())])
            return false; // skip vanilla implementation
        return true;
    }
    [HarmonyPrefix]
    [HarmonyPatch(typeof(AudioSignal), nameof(AudioSignal.IdentifySignal))]
    public static bool AudioSignal_IdentifySignal_Prefix(AudioSignal __instance)
    {
        var signalName = __instance.GetName();
        var wasScanned = PlayerData._currentGameSave.knownSignals.ContainsKey((int)signalName) &&
            PlayerData._currentGameSave.knownSignals[(int)signalName];
        var isUsable = usableSignals.Contains(signalName);

        if (wasScanned && !isUsable)
            return false; // skip vanilla implementation
        return true;
    }

    // The second UX problem I want to solve is the "Unidentified Signal Nearby" notifications for
    // signal sources the player has already scanned.

    [HarmonyPrefix]
    [HarmonyPatch(typeof(AudioSignalDetectionTrigger), nameof(AudioSignalDetectionTrigger.Update))]
    public static bool AudioSignalDetectionTrigger_Update_Prefix(AudioSignalDetectionTrigger __instance)
    {
        var signalName = __instance._signal.GetName();
        var wasScanned = PlayerData._currentGameSave.knownSignals.ContainsKey((int)signalName) &&
            PlayerData._currentGameSave.knownSignals[(int)signalName];
        var isUsable = usableSignals.Contains(signalName);
        var notAlreadyShowingNotification = !__instance._isDetecting;

        if (wasScanned && !isUsable && notAlreadyShowingNotification)
        {
            // Randomizer.Instance.ModHelper.Console.WriteLine($"preventing AudioSignalDetectionTrigger.Update {__instance._signal.GetName()}");
            return false; // skip vanilla implementation
        }
        return true;
    }
}
