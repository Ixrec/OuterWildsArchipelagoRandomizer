using HarmonyLib;
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
            {
                _audioTraps = value;
                PlayDisruptiveAudio();
            }
        }
    }

    private static Random prng = new Random();

    private static void PlayDisruptiveAudio()
    {
        // We're still on the main menu, being told how many Audio Traps were received in previous sessions,
        // so do nothing, not even scheduling future trap execution.
        if (Locator.GetPlayerAudioController() == null) return;

        var playerAudioSource = Locator.GetPlayerAudioController()._oneShotSource;
        var selection = prng.Next(0, 3);
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
                APRandomizer.InGameAPConsole.AddText($"Audio Trap has randomly selected: End Times Music", skipGameplayConsole: true);
                playerAudioSource.AssignAudioLibraryClip(global::AudioType.EndOfTime);
                playerAudioSource.FadeInToLibraryVolume(2f, false, false);
                break;
            default: APRandomizer.OWMLModConsole.WriteLine($"Invalid audio selection: {selection}"); break;
        }
    }
}
