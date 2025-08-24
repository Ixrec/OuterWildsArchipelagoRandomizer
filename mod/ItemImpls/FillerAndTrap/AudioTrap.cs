﻿using HarmonyLib;
using System;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class AudioTrap
{
    private static uint _audioTraps = 0;

    public static uint audioTraps
    {
        get => _audioTraps;
        set
        {
            if (value > _audioTraps)
                PlayDisruptiveAudio();
            _audioTraps = value;
        }
    }

    private static GlobalMusicController globalMusicController = null;
    [HarmonyPrefix, HarmonyPatch(typeof(GlobalMusicController), nameof(GlobalMusicController.Awake))]
    public static void GlobalMusicController_Awake_Prefix(GlobalMusicController __instance) => globalMusicController = __instance;

    private static Random prng = new Random();

    internal static void PlayDisruptiveAudio()
    {
        // We're still on the main menu, being told how many Audio Traps were received in previous sessions,
        // so do nothing, not even scheduling future trap execution.
        if (Locator.GetPlayerAudioController() == null || globalMusicController == null) return;

        var playerAudioSource = Locator.GetPlayerAudioController()._oneShotSource;
        var selection = prng.Next(0, APRandomizer.SlotEnabledEotEDLC() ? 4 : 3); // don't use owlk sounds if DLC is off
        switch (selection)
        {
            case 0:
                APRandomizer.InGameAPConsole.AddText($"Audio Trap has randomly selected: Anglerfish Initiating Chase", skipGameplayConsole: true);
                playerAudioSource.PlayOneShot(global::AudioType.DBAnglerfishDetectTarget, 1f);
                break;
            case 1:
                APRandomizer.InGameAPConsole.AddText($"Audio Trap has randomly selected: Instant Player Death", skipGameplayConsole: true);
                playerAudioSource.PlayOneShot(global::AudioType.Death_Instant, 1f);
                break;
            case 2:
                // In playtesting this often fails, but I can't seem to reproduce the failures when testing,
                // so for now I'm guessing that using endTimesSource instead of playerAudioSource will help.
                APRandomizer.InGameAPConsole.AddText($"Audio Trap has randomly selected: End Times Music", skipGameplayConsole: true);
                var endTimesSource = globalMusicController._endTimesSource;
                endTimesSource.AssignAudioLibraryClip(global::AudioType.EndOfTime);
                endTimesSource.FadeInToLibraryVolume(2f, false, false);
                break;
            case 3:
                APRandomizer.InGameAPConsole.AddText($"Audio Trap has randomly selected: Owlk Scream", skipGameplayConsole: true);
                // I couldn't pick just one of the owlk sounds
                AudioType[] owlkScreams = [
                    global::AudioType.Ghost_CallForHelp,
                    global::AudioType.Ghost_IntruderConfirmed,
                    global::AudioType.Ghost_SomeoneIsInHereHowl,
                ];
                Locator.GetPlayerAudioController()._oneShotSource.PlayOneShot(owlkScreams[prng.Next(0, 3)], 1f);
                break;
            default: APRandomizer.OWMLModConsole.WriteLine($"Invalid audio selection: {selection}", OWML.Common.MessageType.Error); break;
        }
    }
}
