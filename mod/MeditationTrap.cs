using HarmonyLib;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class MeditationTrap
{
    private static uint _meditationTraps = 0;

    public static uint meditationTraps
    {
        get => _meditationTraps;
        set
        {
            if (value > _meditationTraps)
            {
                _meditationTraps = value;
                if (meditationTrapInProgress)
                {
                    APRandomizer.InGameAPConsole.AddText($"Ignoring Meditation Trap because another Meditation Trap is still in progress");
                    return;
                }
                ForceMeditation();
            }
        }
    }

    // SleepTimerUI is the class which draws "00:12" in the middle of the screen during normal campfire meditation,
    // as well as generates the campfire ember effects on the otherwise black screen.

    static SleepTimerUI sleepTimerUI = null;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SleepTimerUI), nameof(SleepTimerUI.Awake))]
    public static void SleepTimerUI_Awake(SleepTimerUI __instance) => sleepTimerUI = __instance;

    // SleepTimerUI is the only caller of IsSleepingAtDreamCampfire, and it uses it to decide whether to render
    // "dream embers" instead of regular campfire embers. For meditation traps, we'd like to use dream embers
    // if we get meditated anywhere inside the Stranger or the dream world.
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerState), nameof(PlayerState.IsSleepingAtDreamCampfire))]
    public static void PlayerState_IsSleepingAtDreamCampfire_Awake(PlayerState __instance, ref bool __result)
    {
        __result = __result // if the vanilla value is already true, don't risk changing it
            || Locator.GetPlayerSectorDetector().IsWithinSector(Sector.Name.InvisiblePlanet)
            || Locator.GetPlayerSectorDetector().IsWithinSector(Sector.Name.DreamWorld);
    }

    // when the game is unpaused, if there's a meditation trap that's been waiting to execute, start executing it
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PauseMenuManager), nameof(PauseMenuManager.OnDeactivatePauseMenu))]
    public static void PauseMenuManager_OnDeactivatePauseMenu(PauseMenuManager __instance)
    {
        if (meditationTrapInProgress)
        {
            APRandomizer.OWMLModConsole.WriteLine($"resuming deferred Meditation Trap now that the game has been unpaused");
            ForceMeditation();
        }
    }

    private static bool meditationTrapInProgress = false;

    private static void ForceMeditation()
    {
        meditationTrapInProgress = true;

        // if the game is currently paused, we need to wait until it's unpaused
        if (Locator.GetPauseCommandListener()._pauseMenu.IsOpen())
        {
            APRandomizer.OWMLModConsole.WriteLine($"deferring Meditation Trap because the game is currently paused");
            return;
        }

        Task.Run(async () => {
            var warningSeconds = 3;
            APRandomizer.OWMLModConsole.WriteLine($"Meditation Trap accepted, beginning in-game console countdown to forced meditation");

            APRandomizer.InGameAPConsole.AddText($"A Mandatory Meditation Minute will begin in");
            while (warningSeconds > 0)
            {
                APRandomizer.InGameAPConsole.AddText($"{warningSeconds}...");
                warningSeconds--;
                await Task.Delay(1000);
            }

            var useGreenFireSounds = Locator.GetPlayerSectorDetector().IsWithinSector(Sector.Name.InvisiblePlanet)
                || Locator.GetPlayerSectorDetector().IsWithinSector(Sector.Name.DreamWorld);
            var fastForwardFactor = 10;
            var inUniverseSecondsAsleep = 60;
            APRandomizer.OWMLModConsole.WriteLine($"beginning forced meditation for {inUniverseSecondsAsleep} in-universe seconds at {fastForwardFactor}x speed");

            ScreenPrompt meditationPrompt = new("Mandatory Meditation Minute In Progress", 0);
            Locator.GetPromptManager().AddScreenPrompt(meditationPrompt, PromptPosition.Center, false);
            meditationPrompt.SetVisibility(true);

            var oldInputMode = OWInput.GetInputMode();
            OWInput.ChangeInputMode(InputMode.None);

            Locator.GetPlayerCamera().GetComponent<PlayerCameraEffectController>().CloseEyes(3f);
            Locator.GetPlayerAudioController().OnStartSleepingAtCampfire(useGreenFireSounds);
            OWTime.SetTimeScale(fastForwardFactor);
            sleepTimerUI.OnStartFastForward();

            await Task.Run(async () => {
                await Task.Delay(inUniverseSecondsAsleep / fastForwardFactor * 1000);
                APRandomizer.OWMLModConsole.WriteLine($"ending forced meditation");

                Locator.GetPromptManager().RemoveScreenPrompt(meditationPrompt);

                OWInput.ChangeInputMode(oldInputMode);

                Locator.GetPlayerCamera().GetComponent<PlayerCameraEffectController>().OpenEyes(1f, false);
                Locator.GetPlayerAudioController().OnStopSleepingAtCampfire(false, false); // no gasping sound
                OWTime.SetTimeScale(1);
                sleepTimerUI.OnEndFastForward();

                meditationTrapInProgress = false;
            });
        });
    }
}
