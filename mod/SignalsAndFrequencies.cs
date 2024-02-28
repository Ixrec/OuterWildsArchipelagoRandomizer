using HarmonyLib;
using System.Collections.Generic;

namespace ArchipelagoRandomizer;

// SignalscopeManager.cs deals with the player having and equipping the Signalscope itself.
// This file is about individual signals and frequencies, and how the Signalscope's UI is
// changed to support signal and frequency items for AP.

// In the vanilla game, "have I scanned X with the Signalscope?" and
// "do I know X's frequency + signal?" are always both true or both false.
// To randomize frequencies and signals, we need to separate those two concepts.

// We use the term "scanned" for frequencies and signals that the player has pointed the
// Signalscope at and are stored in the vanilla save file.
// We use the term "usable" for frequencies and signals that the player has received the
// Archipelago items for, and thus our mod wants to allow the use of.

[HarmonyPatch]
internal class SignalsAndFrequencies
{
    public static Dictionary<SignalName, SignalFrequency> signalToFrequency = new Dictionary<SignalName, SignalFrequency>{
        { SignalName.Traveler_Chert, SignalFrequency.Traveler },
        { SignalName.Traveler_Esker, SignalFrequency.Traveler },
        { SignalName.Traveler_Riebeck, SignalFrequency.Traveler },
        { SignalName.Traveler_Gabbro, SignalFrequency.Traveler },
        { SignalName.Traveler_Feldspar, SignalFrequency.Traveler },
        { SignalName.Quantum_TH_MuseumShard, SignalFrequency.Quantum },
        { SignalName.Quantum_TH_GroveShard, SignalFrequency.Quantum },
        { SignalName.Quantum_CT_Shard, SignalFrequency.Quantum },
        { SignalName.Quantum_BH_Shard, SignalFrequency.Quantum },
        { SignalName.Quantum_GD_Shard, SignalFrequency.Quantum },
        { SignalName.Quantum_QM, SignalFrequency.Quantum },
        { SignalName.EscapePod_CT, SignalFrequency.EscapePod },
        { SignalName.EscapePod_BH, SignalFrequency.EscapePod },
        { SignalName.EscapePod_DB, SignalFrequency.EscapePod },
        { SignalName.HideAndSeek_Galena, SignalFrequency.HideAndSeek },
        { SignalName.HideAndSeek_Tephra, SignalFrequency.HideAndSeek },
        // DLC will add: SignalFrequency.Radio
        // left out Default, WarpCore and Statue frequencies because I don't believe they get used
        // left out Default, HideAndSeek_Arkose and all the White Hole signals because I don't believe they're used
        // left out Nomai and Prisoner because I believe those are only available during the finale
    };
 
    public static Dictionary<SignalFrequency, HashSet<SignalName>> frequencyToSignals = new Dictionary<SignalFrequency, HashSet<SignalName>>
    {
        { SignalFrequency.Traveler, new HashSet<SignalName>{
            SignalName.Traveler_Chert,
            SignalName.Traveler_Esker,
            SignalName.Traveler_Riebeck,
            SignalName.Traveler_Gabbro,
            SignalName.Traveler_Feldspar,
        } },
        { SignalFrequency.Quantum, new HashSet<SignalName>{
            SignalName.Quantum_TH_MuseumShard,
            SignalName.Quantum_TH_GroveShard,
            SignalName.Quantum_CT_Shard,
            SignalName.Quantum_BH_Shard,
            SignalName.Quantum_GD_Shard,
            SignalName.Quantum_QM,
        } },
        { SignalFrequency.EscapePod, new HashSet<SignalName>{
            SignalName.EscapePod_CT,
            SignalName.EscapePod_CT,
            SignalName.EscapePod_DB,
        } },
        { SignalFrequency.HideAndSeek, new HashSet<SignalName>{
            SignalName.HideAndSeek_Galena,
            SignalName.HideAndSeek_Tephra,
        } },
    };

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
        var signal = __instance.GetName();
        if (!LocationNames.signalToLocation.TryGetValue(signal, out Location signalLocation))
            return true;

        // Because of all the different states you can be in with frequency item, frequency location,
        // signal item and signal location all being separate things you may or may not have,
        // there are corner cases where the vanilla "scanning a signal implies scanning the frequency"
        // doesn't work and we have to do a frequency check here instead of relying on IdentifyFrequency.
        if (signalToFrequency.TryGetValue(signal, out var frequency))
            if (LocationNames.frequencyToLocation.TryGetValue(frequency, out var frequencyLocation))
                if (!APRandomizer.SaveData.locationsChecked[frequencyLocation])
                {
                    APRandomizer.OWMLModConsole.WriteLine($"AudioSignal_IdentifySignal_Prefix checking corresponding frequency location: {frequencyLocation}");
                    LocationTriggers.CheckLocation(frequencyLocation);
                }

        if (APRandomizer.SaveData.locationsChecked[signalLocation])
            return false; // skip vanilla implementation

        return true;
    }

    // This next patch does multiple things:
    // - Prevent scanning a signal we don't have the frequency item for
    // - Prevent showing "Unidentified Signal Nearby" notifications for signal sources you've already scanned
    // - Allow scanning signals without the suit. This is how vanilla works, but in rando it feels like a bug
    // if you happen to get Signalscope early on TH.

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

        // If this signal corresponds to an AP frequency item that we don't have yet,
        // prevent us from "detecting" and thus scanning it until we get that item.
        if (signalToFrequency.TryGetValue(signalName, out var frequency))
            if (frequency != SignalFrequency.Traveler)
                if (!usableFrequencies.Contains(frequency) && mightDisplayUnidentifiedSignalMessage)
                    return false; // skip vanilla implementation

        // If the player has already scanned this signal, then don't display "Unidentified Signal Nearby"
        var location = LocationNames.signalToLocation[signalName];
        if (APRandomizer.SaveData.locationsChecked[location] && mightDisplayUnidentifiedSignalMessage)
            return false; // skip vanilla implementation

        // copy-pasted and tweaked from vanilla implementation
        bool flag = PlayerData.KnowsSignal(__instance._signal.GetName());
        bool flag2 = __instance._signal.IsActive() && !flag && __instance._isPlayerInside && !PlayerState.IsInsideShip() &&
            // remove only the `Locator.GetPlayerSuit().IsWearingHelmet() &&` part
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

    // When we get into an "Unidentified Signal Nearby" state, track what that signal is
    private static AudioSignal nearbyUnscannedSignal = null;

    [HarmonyPostfix, HarmonyPatch(typeof(Signalscope), nameof(Signalscope.OnEnterSignalDetectionTrigger))]
    public static void Signalscope_OnEnterSignalDetectionTrigger(AudioSignalDetectionTrigger __instance, AudioSignal signal)
    {
        APRandomizer.OWMLModConsole.WriteLine($"Signalscope_OnEnterSignalDetectionTrigger {signal.GetName()}");
        nearbyUnscannedSignal = signal;
    }
    [HarmonyPostfix, HarmonyPatch(typeof(Signalscope), nameof(Signalscope.OnExitSignalDetectionTrigger))]
    public static void Signalscope_OnExitSignalDetectionTrigger(AudioSignalDetectionTrigger __instance, AudioSignal signal)
    {
        APRandomizer.OWMLModConsole.WriteLine($"Signalscope_OnExitSignalDetectionTrigger {signal.GetName()}");
        nearbyUnscannedSignal = null;
    }

    // Last but not least, this patch ensures that any signal you have not yet received the AP item for
    // will not show up in the Signalscope's UI, and not make any sound when holding the Signalscope.

    [HarmonyPrefix, HarmonyPatch(typeof(AudioSignal), nameof(AudioSignal.UpdateSignalStrength))]
    public static bool AudioSignal_UpdateSignalStrength_Prefix(AudioSignal __instance, Signalscope scope, float distToClosestScopeObstruction)
    {
        if (usableSignals.Contains(__instance.GetName()))
            return true;

        if (__instance.GetName() == nearbyUnscannedSignal?.GetName())
            return true;

        // The Hide & Seek signals don't do the whole "Unidentified Signal Nearby" thing,
        // so we can't "hide them at long range" without breaking scanning them.
        if (signalToFrequency.TryGetValue(__instance.GetName(), out var f) && f == SignalFrequency.HideAndSeek)
            return true;

        // copy-pasted from several early returns in the vanilla code
        __instance._signalStrength = 0f;
        __instance._degreesFromScope = 180f;

        return false; // skip vanilla implementation
    }
}
