﻿using HarmonyLib;
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
internal class SignalscopeManager
{
    private static bool _hasSignalscope = false;

    public static bool hasSignalscope
    {
        get => _hasSignalscope;
        set
        {
            if (_hasSignalscope != value)
            {
                _hasSignalscope = value;
                ApplyHasSignalscopeFlag(_hasSignalscope);
            }
        }
    }

    // The signalscope is the only tool in vanilla which you can equip without even wearing the suit, and thus
    // by far the easiest tool to try equipping without any "[Y] Equip Signalscope" prompt for me to edit.

    // So this "duplicate" prompt is for when the player presses Y and I know there won't be an existing prompt about it.
    static ScreenPrompt signalscopeNotAvailablePrompt = new ScreenPrompt("Signalscope Not Available", 0);

    [HarmonyPostfix, HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.LateInitialize))]
    public static void ToolModeUI_LateInitialize_Postfix()
    {
        Locator.GetPromptManager().AddScreenPrompt(signalscopeNotAvailablePrompt, PromptPosition.Center, false);
    }

    [HarmonyPrefix, HarmonyPatch(typeof(ToolModeSwapper), nameof(ToolModeSwapper.EquipToolMode))]
    public static bool ToolModeSwapper_EquipToolMode_Prefix(ToolMode mode)
    {
        if (mode == ToolMode.SignalScope && !hasSignalscope)
        {
            APRandomizer.OWMLModConsole.WriteLine($"blocked attempt to equip Signalscope");

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

    [HarmonyPostfix, HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.Start))]
    public static void ToolModeUI_Start_Postfix(ToolModeUI __instance)
    {
        equipSignalscopePrompt = __instance._signalscopePrompt;
        centerEquipSignalScopePrompt = __instance._centerSignalscopePrompt;

        ApplyHasSignalscopeFlag(hasSignalscope);
    }

    public static void ApplyHasSignalscopeFlag(bool hasSignalscope)
    {
        if (equipSignalscopePrompt == null || centerEquipSignalScopePrompt == null) return;

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

    public static HashSet<SignalFrequency> usableFrequencies = new();
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
    [HarmonyPrefix, HarmonyPatch(typeof(PlayerData), nameof(PlayerData.KnowsFrequency))]

    public static bool PlayerData_KnowsFrequency_Prefix(SignalFrequency frequency, ref bool __result)
    {
        if (!ItemNames.frequencyToItem.ContainsKey(frequency))
            return true; // not a frequency we've turned into an AP item & location, let the vanilla implementation handle it

        __result = usableFrequencies.Contains(frequency); // override return value
        return false; // skip vanilla implementation
    }
    [HarmonyPrefix, HarmonyPatch(typeof(PlayerData), nameof(PlayerData.KnowsMultipleFrequencies))]
    public static bool PlayerData_KnowsMultipleFrequencies_Prefix(ref bool __result)
    {
        // The SignalFrequency enum has 8 values. 3 of them are AP items/locations, 1 (Radio / Deep Space Radio) is not yet but will be
        // when we support the EotE DLC, 1 (Traveler / Outer Wilds Ventures) the player always has, and the last 3 are never used.
        // Therefore, the player "knows" at least two frequencies if they have either acquired one of the AP frequency items, making it "usable",
        // or if they've scanned the Deep Space radio frequency.

        __result = usableFrequencies.Count > 0 || PlayerData.KnowsFrequency(SignalFrequency.Radio); // override return value
        return false; // skip vanilla implementation
    }
    [HarmonyPrefix, HarmonyPatch(typeof(PlayerData), nameof(PlayerData.KnowsSignal))]
    public static bool PlayerData_KnowsSignal_Prefix(SignalName signalName, ref bool __result)
    {
        if (!ItemNames.signalToItem.ContainsKey(signalName))
            return true; // not a signal we've turned into an AP item & location, let the vanilla implementation handle it

        // if we let the game think the signal's known, then you won't be able to scan it,
        // so we have to wait for *both* the item to be acquired and the location checked
        // before we can let the in-game signalscope fully recognize this signal
        var location = LocationNames.signalToLocation[signalName];
        var isKnown = APRandomizer.SaveData.locationsChecked[location] && usableSignals.Contains(signalName);

        __result = isKnown; // override return value
        return false; // skip vanilla implementation
    }
    // In vanilla, this is used to forget the Hide & Seek frequency
    // after each loop. But we never want to forget anything.
    [HarmonyPrefix, HarmonyPatch(typeof(PlayerData), nameof(PlayerData.ForgetFrequency))]
    public static bool PlayerData_ForgetFrequency_Prefix(SignalFrequency frequency)
    {
        APRandomizer.OWMLModConsole.WriteLine($"preventing PlayerData.ForgetFrequency({frequency})");
        return false; // skip vanilla implementation, never forget a frequency
    }

    // Next, these are the patches to actually check locations when the player scans a frequency and/or signal.

    [HarmonyPrefix, HarmonyPatch(typeof(PlayerData), nameof(PlayerData.LearnFrequency))]
    public static void PlayerData_LearnFrequency_Prefix(SignalFrequency frequency)
    {
        if (LocationNames.frequencyToLocation.ContainsKey(frequency))
        {
            var locationName = LocationNames.frequencyToLocation[frequency];
            LocationTriggers.CheckLocation(locationName);
        }
    }
    [HarmonyPrefix, HarmonyPatch(typeof(PlayerData), nameof(PlayerData.LearnSignal))]
    public static void PlayerData_LearnSignal_Prefix(SignalName signalName)
    {
        if (LocationNames.signalToLocation.ContainsKey(signalName))
        {
            var locationName = LocationNames.signalToLocation[signalName];
            LocationTriggers.CheckLocation(locationName);
        }
    }

    // The above patches are enough to separate signalscope scans from signalscope knowledge,
    // but we can do better.

    // The first UX problem I want to solve is that scanning a signal source puts the game
    // in an infinite loop of relearning it, complete with annoying eternal beeping.
    // That's what these next two patches are for:

    // If the player has scanned this frequency/signal before, and the vanilla code is calling
    // these methods only because they haven't received the AP item for that frequency/signal yet,
    // then don't "relearn" it.
    [HarmonyPrefix, HarmonyPatch(typeof(AudioSignal), nameof(AudioSignal.IdentifyFrequency))]
    public static bool AudioSignal_IdentifyFrequency_Prefix(AudioSignal __instance)
    {
        if (!LocationNames.frequencyToLocation.TryGetValue(__instance.GetFrequency(), out Location location))
            return true;

        if (APRandomizer.SaveData.locationsChecked[location])
            return false; // skip vanilla implementation

        return true;
    }
    [HarmonyPrefix, HarmonyPatch(typeof(AudioSignal), nameof(AudioSignal.IdentifySignal))]
    public static bool AudioSignal_IdentifySignal_Prefix(AudioSignal __instance)
    {
        if (!LocationNames.signalToLocation.TryGetValue(__instance.GetName(), out Location location))
            return true;

        if (APRandomizer.SaveData.locationsChecked[location])
            return false; // skip vanilla implementation

        // If you have the frequency *item* already, the game won't Identify/LearnFrequency(),
        // because we do want a frequency to be "usable" with the item and not the location,
        // so in this specific case we need to check the frequency *location* manually.
        if (ItemNames.frequencyToItem.TryGetValue(__instance.GetFrequency(), out Item item))
            if (APRandomizer.SaveData.itemsAcquired[item] > 0)
                if (LocationNames.frequencyToLocation.TryGetValue(__instance.GetFrequency(), out Location frequencyLocation))
                    if (!APRandomizer.SaveData.locationsChecked[frequencyLocation])
                        LocationTriggers.CheckLocation(frequencyLocation);

        return true;
    }

    // The second UX problem I want to solve is the "Unidentified Signal Nearby" notifications for
    // signal sources the player has already scanned.
    // And the third is that in vanilla, you cannot scan signals of unknown frequency without wearing
    // the suit, which in rando feels like a bug if you happen to get Signalscope early on TH.

    [HarmonyPrefix, HarmonyPatch(typeof(AudioSignalDetectionTrigger), nameof(AudioSignalDetectionTrigger.Update))]
    public static bool AudioSignalDetectionTrigger_Update_Prefix(AudioSignalDetectionTrigger __instance)
    {
        var signalName = __instance._signal.GetName();
        if (!LocationNames.signalToLocation.ContainsKey(signalName))
            return true; // not a signal we've turned into an AP item & location, let the vanilla implementation handle it

        // isDetecting=false means this Update() is deciding whether to show Unidentified Signal Nearby,
        // which we don't want to show on a scanned signal (even if it's not usable yet).
        // but isDetecting=true means this Update() is deciding whether to *hide* that message,
        // which we do want hidden in all the vanilla cases.
        var mightDisplayUnidentifiedSignalMessage = !__instance._isDetecting;

        var location = LocationNames.signalToLocation[signalName];
        if (APRandomizer.SaveData.locationsChecked[location] && mightDisplayUnidentifiedSignalMessage) {
            return false; // skip vanilla implementation
        }

        // copy-pasted and tweaked from vanilla implementation
        bool flag = PlayerData.KnowsSignal(__instance._signal.GetName());
        bool flag2 = __instance._signal.IsActive() && !flag && __instance._isPlayerInside && !PlayerState.IsInsideShip() &&
            // remove only the Locator.GetPlayerSuit().IsWearingHelmet() && part
            (__instance._allowUnderwater || !PlayerState.IsCameraUnderwater()) && __instance._inDarkZone == PlayerState.InDarkZone();

        // if the only reason we "don't detect" an unidentified signal nearby is that the player is not wearing the helmet,
        // then skip vanilla implementation and force it to be detected anyway so the player can scan it
        bool wouldBeDetecting = flag2;
        if (wouldBeDetecting && !Locator.GetPlayerSuit().IsWearingHelmet())
        {
            if (mightDisplayUnidentifiedSignalMessage)
            {
                APRandomizer.OWMLModConsole.WriteLine($"AudioSignalDetectionTrigger_Update_Prefix forcing detection of {__instance._signal.GetName()} despite player not wearing the helmet");

                // copy-pasted and tweaked from vanilla implementation
                __instance._isDetecting = true;
                Locator.GetToolModeSwapper().GetSignalScope().OnEnterSignalDetectionTrigger(__instance._signal);
                Locator.GetToolModeSwapper().GetSignalScope().SelectFrequency(__instance._signal.GetFrequency());
                NotificationManager.SharedInstance.PostNotification(__instance._nearbySignalMessage, true);
            }

            return false; // skip vanilla implementation
        }

        return true;
    }
}
