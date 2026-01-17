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
    public static Dictionary<string, string> signalToFrequency = new Dictionary<string, string>{
        { "Traveler_Chert", "Traveler" },
        { "Traveler_Esker", "Traveler" },
        { "Traveler_Riebeck", "Traveler" },
        { "Traveler_Gabbro", "Traveler" },
        { "Traveler_Feldspar", "Traveler" },
        { "Quantum_TH_MuseumShard", "Quantum" },
        { "Quantum_TH_GroveShard", "Quantum" },
        { "Quantum_CT_Shard", "Quantum" },
        { "Quantum_BH_Shard", "Quantum" },
        { "Quantum_GD_Shard", "Quantum" },
        { "Quantum_QM", "Quantum" },
        { "EscapePod_CT", "EscapePod" },
        { "EscapePod_BH", "EscapePod" },
        { "EscapePod_DB", "EscapePod" },
        { "HideAndSeek_Galena", "HideAndSeek" },
        { "HideAndSeek_Tephra", "HideAndSeek" },
        { "RadioTower", "Radio" },
        { "MapSatellite", "Radio" },
        // left out Default, WarpCore and Statue frequencies because I don't believe they get used
        // left out Default, HideAndSeek_Arkose and all the White Hole signals because I don't believe they're used
        // left out Nomai and Prisoner because I believe those are only available during the finale

        // Hearth's Neighbor
        { "DERELICT SHIP", "NEIGHBOR'S DISTRESS SIGNAL" },
        { "DEAD LAKE DISH", "NEIGHBOR'S DISTRESS SIGNAL" },
        { "ENTRANCE STATION", "Lava Core Signals" },
        { "LAVA SHRINE", "Lava Core Signals" },
        { "STRUCTURE BY CLIFF", "Lava Core Signals" },
        { "STRUCTURE WITH PILLARS", "Lava Core Signals" },
        { "OLD ABANDONED TOWN", "Lava Core Signals" },
        { "DERELICT SHIP COCKPIT", "GALACTIC COMMUNICATION" },
        { "SOLE SURVIVOR", "GALACTIC COMMUNICATION" },

        // The Outsider has no signals

        // Astral Codec
        { "Chime Transmitter", "Astral Codec" },
        { "Translation Probe Cinder", "Astral Codec" },
        { "Translation Probe Thicket", "Astral Codec" },

        // Fret's Quest
        { "Gneiss's Radio", "Hearthian Radio" },
        { "Rim's Radio", "Hearthian Radio" },
        { "Bridge's Radio", "Hearthian Radio" },
        { "Reson's Radio", "Hearthian Radio" },
        { "Reson's Second Radio", "Hearthian Radio" },
        { "Rim's Second Radio", "Hearthian Radio" },

        // Forgotten Castaways
        // left out Ditylum (Traveler) because it is only available during the finale
        { "First Marker", "Nomai Trailmarkers" },
        { "Camp Marker", "Nomai Trailmarkers" },
        { "Amplified Ambience", "Natural Phenomena" },
        { "Gravitational Anomaly", "Natural Phenomena" },
        { "Geothermal Activity", "Natural Phenomena" },
        { "Hot Shard", "Quantum" },
        { "Alien Echolocation", "Echolocation Tones" },
        { "Warped Echolocation", "Echolocation Tones" },
        { "Ditylum Echolocation", "Echolocation Tones" },
    };

    public static Dictionary<string, HashSet<string>> frequencyToSignals = new Dictionary<string, HashSet<string>>
    {
        { "Traveler", new HashSet<string>{
            "Traveler_Chert",
            "Traveler_Esker",
            "Traveler_Riebeck",
            "Traveler_Gabbro",
            "Traveler_Feldspar",
        } },
        { "Quantum", new HashSet<string>{
            "Quantum_TH_MuseumShard",
            "Quantum_TH_GroveShard",
            "Quantum_CT_Shard",
            "Quantum_BH_Shard",
            "Quantum_GD_Shard",
            "Quantum_QM",
            "Hot Shard", // Forgotten Castaways
        } },
        { "EscapePod", new HashSet<string>{
            "EscapePod_BH",
            "EscapePod_CT",
            "EscapePod_DB",
        } },
        { "HideAndSeek", new HashSet<string>{
            "HideAndSeek_Galena",
            "HideAndSeek_Tephra",
        } },
        { "Radio", new HashSet<string>{
            "RadioTower",
            "MapSatellite",
        } },

        // Hearth's Neighbor
        { "NEIGHBOR'S DISTRESS SIGNAL", new HashSet<string>{
            "DERELICT SHIP",
            "DEAD LAKE DISH",
        } },
        { "Lava Core Signals", new HashSet<string>{
            "ENTRANCE STATION",
            "LAVA SHRINE",
            "STRUCTURE BY CLIFF",
            "STRUCTURE WITH PILLARS",
            "OLD ABANDONED TOWN",
        } },
        { "GALACTIC COMMUNICATION", new HashSet<string>{
            "DERELICT SHIP COCKPIT",
            "SOLE SURVIVOR",
        } },

        // The Outsider has no signals

        // Astral Codec
        { "Astral Codec", new HashSet<string>{
            "Chime Transmitter",
            "Translation Probe Cinder",
            "Translation Probe Thicket",
        } },

        // Fret's Quest
        { "Hearthian Radio", new HashSet<string>{
            "Gneiss's Radio",
            "Rim's Radio",
            "Bridge's Radio",
            "Reson's Radio",
            "Reson's Second Radio",
            "Rim's Second Radio",
        } },

        // Forgotten Castaways
        { "Nomai Trailmarkers", new HashSet<string>{
            "First Marker",
            "Camp Marker",
        } },
        { "Natural Phenomena", new HashSet<string>{
            "Amplified Ambience",
            "Gravitational Anomaly",
            "Geothermal Activity",
        } },
        { "Echolocation Tones", new HashSet<string>{
            "Alien Echolocation",
            "Warped Echolocation",
            "Ditylum Echolocation",
        } },
    };

    public static HashSet<string> usableFrequencies = new();
    public static HashSet<string> usableSignals = new();
    public static void SetFrequencyUsable(string frequency, bool usable)
    {
        if (usable) usableFrequencies.Add(frequency); else usableFrequencies.Remove(frequency);
    }
    public static void SetSignalUsable(string signal, bool usable)
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
    [HarmonyPriority(Priority.Low)] // run this *after* the New Horizons patch for KnowsFrequency, so our __result overrides NH's
    public static bool PlayerData_KnowsFrequency_Prefix(SignalFrequency frequency, ref bool __result)
    {
        if (!ItemNames.frequencyToItem.ContainsKey(frequency.ToString()))
            return true; // not a frequency we've turned into an AP item & location, let the vanilla implementation handle it

        __result = usableFrequencies.Contains(frequency.ToString()); // override return value
        return false; // skip vanilla implementation
    }
    [HarmonyPrefix, HarmonyPatch(typeof(PlayerData), nameof(PlayerData.KnowsMultipleFrequencies))]
    [HarmonyPriority(Priority.Low)] // run this *after* the New Horizons patch for KnowsMultipleFrequencies, so our __result overrides NH's
    public static bool PlayerData_KnowsMultipleFrequencies_Prefix(ref bool __result)
    {
        // The Outer Wilds Ventures frequency the Signalscope starts with is the only frequency we haven't itemized,
        // so the player "knows" at least two frequencies if they have acquired any one of the AP frequency items.

        // If Forgotten Castaways is enabled, Natural Phenomena is an additional starting frequency.
        if (APRandomizer.SlotEnabledMod("enable_fc_mod"))
        {
            __result = true;
            return false;
        }

        __result = usableFrequencies.Count > 0; // override return value
        return false; // skip vanilla implementation
    }
    [HarmonyPrefix, HarmonyPatch(typeof(PlayerData), nameof(PlayerData.KnowsSignal))]
    [HarmonyPriority(Priority.Low)] // run this *after* the New Horizons patch for KnowsSignal, so our __result overrides NH's
    public static bool PlayerData_KnowsSignal_Prefix(SignalName signalName, ref bool __result)
    {
        if (!ItemNames.signalToItem.ContainsKey(signalName.ToString()))
            return true; // not a signal we've turned into an AP item & location, let the vanilla implementation handle it

        // if we let the game think the signal's known, then you won't be able to scan it,
        // so we have to wait for *both* the item to be acquired and the location checked
        // before we can let the in-game signalscope fully recognize this signal
        var location = LocationNames.signalToLocation[signalName.ToString()];
        if (!APRandomizer.SaveData.locationsChecked.ContainsKey(location))
        {
            APRandomizer.OWMLModConsole.WriteLine($"AudioSignal_IdentifySignal_Prefix discovered the save file is missing {location}", OWML.Common.MessageType.Error);
            return true;
        }
        var isKnown = APRandomizer.SaveData.locationsChecked[location] && usableSignals.Contains(signalName.ToString());

        __result = isKnown; // override return value
        return false; // skip vanilla implementation
    }
    // In vanilla, this is used to forget the Hide & Seek frequency
    // after each loop. But we never want to forget anything.
    [HarmonyPrefix, HarmonyPatch(typeof(PlayerData), nameof(PlayerData.ForgetFrequency))]
    public static bool PlayerData_ForgetFrequency_Prefix(SignalFrequency frequency)
    {
        return false; // skip vanilla implementation, never forget a frequency
    }

    // Next, these are the patches to actually check locations when the player scans a frequency and/or signal.

    [HarmonyPrefix, HarmonyPatch(typeof(PlayerData), nameof(PlayerData.LearnFrequency))]
    public static void PlayerData_LearnFrequency_Prefix(SignalFrequency frequency)
    {
        if (LocationNames.frequencyToLocation.ContainsKey(frequency.ToString()))
        {
            var locationName = LocationNames.frequencyToLocation[frequency.ToString()];
            LocationTriggers.CheckLocation(locationName);
        }
    }
    [HarmonyPrefix, HarmonyPatch(typeof(PlayerData), nameof(PlayerData.LearnSignal))]
    public static void PlayerData_LearnSignal_Prefix(SignalName signalName)
    {
        if (LocationNames.signalToLocation.ContainsKey(signalName.ToString()))
        {
            var locationName = LocationNames.signalToLocation[signalName.ToString()];
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
        if (!LocationNames.frequencyToLocation.TryGetValue(__instance.GetFrequency().ToString(), out Location location))
            return true;

        if (APRandomizer.SaveData.locationsChecked[location])
            return false; // skip vanilla implementation

        return true;
    }
    [HarmonyPrefix, HarmonyPatch(typeof(AudioSignal), nameof(AudioSignal.IdentifySignal))]
    public static bool AudioSignal_IdentifySignal_Prefix(AudioSignal __instance)
    {
        var signal = __instance.GetName();
        if (!LocationNames.signalToLocation.TryGetValue(signal.ToString(), out Location signalLocation))
            return true;

        // Because of all the different states you can be in with frequency item, frequency location,
        // signal item and signal location all being separate things you may or may not have,
        // there are corner cases where the vanilla "scanning a signal implies scanning the frequency"
        // doesn't work and we have to do a frequency check here instead of relying on IdentifyFrequency.
        if (signalToFrequency.TryGetValue(signal.ToString(), out var frequency))
            if (LocationNames.frequencyToLocation.TryGetValue(frequency.ToString(), out var frequencyLocation))
                if (!APRandomizer.SaveData.locationsChecked[frequencyLocation])
                    LocationTriggers.CheckLocation(frequencyLocation);

        // For similar reasons, we have to call IdentifyFrequency() ourselves or else the game ends up displaying
        // "Unidentified" in the UI forever. In practice this only seems to happen with New Horizons frequencies.
        var freqStr = AudioSignal.FrequencyToString(__instance.GetFrequency());
        if (freqStr == UITextLibrary.GetString(UITextType.SignalFreqUnidentified))
        {
            APRandomizer.OWMLModConsole.WriteLine($"AudioSignal_IdentifySignal_Prefix for {signal} calling " +
                $"IdentifyFrequency for {__instance.GetFrequency()} since OW/NH believe it to be unidentified");
            __instance.IdentifyFrequency();
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
        if (!LocationNames.signalToLocation.ContainsKey(signalName.ToString()))
            return true; // not a signal we've turned into an AP item & location, let the vanilla implementation handle it

        // isDetecting=false means this Update() is deciding whether to show Unidentified Signal Nearby,
        // which we don't want to show on a scanned signal (even if it's not usable yet).
        // but isDetecting=true means this Update() is deciding whether to *hide* that message,
        // which we do want hidden in all the vanilla cases.
        var mightDisplayUnidentifiedSignalMessage = !__instance._isDetecting;

        // If this signal corresponds to an AP frequency item that we don't have yet,
        // prevent us from "detecting" and thus scanning it until we get that item.
        if (signalToFrequency.TryGetValue(signalName.ToString(), out var frequency))
            if (frequency != "Traveler" && frequency != "Natural Phenomena") // FC: Natural Phenomena is a starting frequency
                if (!usableFrequencies.Contains(frequency) && mightDisplayUnidentifiedSignalMessage)
                    return false; // skip vanilla implementation

        // If the player has already scanned this signal, then don't display "Unidentified Signal Nearby"
        var location = LocationNames.signalToLocation[signalName.ToString()];
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
        // APRandomizer.OWMLModConsole.WriteLine($"Signalscope_OnEnterSignalDetectionTrigger {signal.GetName()}");
        nearbyUnscannedSignal = signal;
    }
    [HarmonyPostfix, HarmonyPatch(typeof(Signalscope), nameof(Signalscope.OnExitSignalDetectionTrigger))]
    public static void Signalscope_OnExitSignalDetectionTrigger(AudioSignalDetectionTrigger __instance, AudioSignal signal)
    {
        // APRandomizer.OWMLModConsole.WriteLine($"Signalscope_OnExitSignalDetectionTrigger {signal.GetName()}");
        nearbyUnscannedSignal = null;
    }

    // Last but not least, this patch ensures that any signal you have not yet received the AP item for
    // will not show up in the Signalscope's UI, and not make any sound when holding the Signalscope.

    [HarmonyPrefix, HarmonyPatch(typeof(AudioSignal), nameof(AudioSignal.UpdateSignalStrength))]
    [HarmonyPriority(Priority.Low)] // run this *after* the New Horizons patch for UpdateSignalStrength, because
                                    // otherwise NH's special code for custom QM signals unhides the vanilla QM signal
    public static bool AudioSignal_UpdateSignalStrength_Prefix(AudioSignal __instance, Signalscope scope, float distToClosestScopeObstruction)
    {
        if (usableSignals.Contains(__instance.GetName().ToString()))
            return true;

        // If this is the "Unidentified Signal Nearby" signal, then we do want it to be shown
        if (__instance.GetName() == nearbyUnscannedSignal?.GetName())
            return true;

        // During the Eye finale, all signals use SignalName.Default and are activated by the Eye's state
        // rather than signal detection triggers so they'll never get assigned to nearbyUnscannedSignal.
        //if (__instance.GetName() == SignalName.Default) also works, but it's safer to just do nothing at the Eye
        if (LoadManager.s_currentScene == OWScene.EyeOfTheUniverse)
            return true;

        // The Hide & Seek signals don't do the whole "Unidentified Signal Nearby" thing,
        // so we can't "hide them at long range" without breaking scanning them.
        if (signalToFrequency.TryGetValue(__instance.GetName().ToString(), out var f) && f == "HideAndSeek")
            return true;

        // We only want to block long-range detection of signals which have AP items unlocking that signal
        // and thus will be added to usableSignals later. Most story mod signals do not have items.
        if (!ItemNames.signalToItem.ContainsKey(__instance.GetName().ToString()))
            return true;

        // copy-pasted from several early returns in the vanilla code
        __instance._signalStrength = 0f;
        __instance._degreesFromScope = 180f;

        return false; // skip vanilla implementation
    }

    [HarmonyPrefix, HarmonyPatch(typeof(AudioSignal), nameof(AudioSignal.Start))]
    public static void IdentifyAmplifiedAmbience(AudioSignal __instance)
    {
        // Immediately identify the Natural Phenomena frequency for better UX
        if (__instance.GetFrequency().ToString() == "Natural Phenomena")
        {
            PlayerData.LearnFrequency(__instance.GetFrequency());
        }
    }

    // If you get the Signalscope item, then talk to Tephra before getting the Hide & Seek Frequency item, the
    // game will automatically switch you to that frequency after the conversation ends, bypassing the item.
    // So we need yet another patch to block that automatic frequency switch.
    [HarmonyPrefix, HarmonyPatch(typeof(Signalscope), nameof(Signalscope.SelectFrequency))]
    public static bool Signalscope_SelectFrequency_Prefix(SignalFrequency frequency)
    {
        if (frequency == SignalFrequency.HideAndSeek && !PlayerData.KnowsFrequency(frequency))
        {
            APRandomizer.OWMLModConsole.WriteLine($"Signalscope_SelectFrequency_Prefix blocking automatic switch to {frequency}");
            return false;
        }
        return true;
    }

/* these were useful for testing Signalscope issues in the Eye finale

    [HarmonyPostfix, HarmonyPatch(typeof(PlayerData), nameof(PlayerData.GetWarpedToTheEye))]
    public static void PlayerData_GetWarpedToTheEye(ref bool __result) => __result = true;

    [HarmonyPostfix, HarmonyPatch(typeof(EyeStateManager), nameof(EyeStateManager.Start))]
    public static void EyeStateManager_Start_Postfix(EyeStateManager __instance)
    {
        APRandomizer.OWMLModConsole.WriteLine($"EyeStateManager_Start_Postfix {__instance._initialState} {__instance._state}");
        APRandomizer.OWMLModConsole.WriteLine($"EyeStateManager_Start_Postfix calling SetState(EyeState.ForestIsDark)");
        __instance.SetState(EyeState.ForestOfGalaxies);
    }
    [HarmonyPrefix, HarmonyPatch(typeof(EyeStateManager), nameof(EyeStateManager.SetState))]
    public static void EyeStateManager_SetState(EyeStateManager __instance, EyeState state)
    {
        APRandomizer.OWMLModConsole.WriteLine($"EyeStateManager_SetState {state}");
    }
*/
}
