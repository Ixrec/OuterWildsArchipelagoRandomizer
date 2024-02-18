using HarmonyLib;
using System.Threading.Tasks;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class NapTrap
{
    private static uint _napTraps = 0;

    public static uint napTraps
    {
        get => _napTraps;
        set
        {
            if (value > _napTraps)
            {
                _napTraps = value;
                if (napTrapInProgress)
                {
                    APRandomizer.InGameAPConsole.AddText($"Ignoring Nap Trap because another Nap Trap is still in progress");
                    return;
                }
                ForceNap();
            }
        }
    }

    // SleepTimerUI is the class which draws "00:12" in the middle of the screen during normal campfire napping,
    // as well as generates the campfire ember effects on the otherwise black screen.

    static SleepTimerUI sleepTimerUI = null;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SleepTimerUI), nameof(SleepTimerUI.Awake))]
    public static void SleepTimerUI_Awake(SleepTimerUI __instance) => sleepTimerUI = __instance;

    // SleepTimerUI is the only caller of IsSleepingAtDreamCampfire, and it uses it to decide whether to render
    // "dream embers" instead of regular campfire embers. For nap traps, we'd like to use dream embers
    // if we get meditated anywhere inside the Stranger or the dream world.
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerState), nameof(PlayerState.IsSleepingAtDreamCampfire))]
    public static void PlayerState_IsSleepingAtDreamCampfire_Awake(PlayerState __instance, ref bool __result)
    {
        __result = __result // if the vanilla value is already true, don't risk changing it
            || Locator.GetPlayerSectorDetector().IsWithinSector(Sector.Name.InvisiblePlanet)
            || Locator.GetPlayerSectorDetector().IsWithinSector(Sector.Name.DreamWorld);
    }

    // when the game is unpaused, if there's a nap trap that's been waiting to execute, start executing it
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PauseMenuManager), nameof(PauseMenuManager.OnDeactivatePauseMenu))]
    public static void PauseMenuManager_OnDeactivatePauseMenu(PauseMenuManager __instance)
    {
        if (napTrapInProgress)
        {
            APRandomizer.OWMLModConsole.WriteLine($"resuming deferred Nap Trap now that the game has been unpaused");
            ForceNap();
        }
    }

    private static bool napTrapInProgress = false;

    private static void ForceNap()
    {
        // We're still on the main menu, being told how many Nap Traps were received in previous sessions,
        // so do nothing, not even scheduling future trap execution.
        if (Locator.GetPauseCommandListener() == null) return;

        napTrapInProgress = true;

        // if the game is currently paused, we need to wait until it's unpaused
        if (Locator.GetPauseCommandListener()._pauseMenu.IsOpen())
        {
            APRandomizer.OWMLModConsole.WriteLine($"deferring Nap Trap because the game is currently paused");
            return;
        }

        Task.Run(async () => {
            var warningSeconds = 3;
            APRandomizer.OWMLModConsole.WriteLine($"Nap Trap accepted, beginning in-game console countdown to forced nap");

            APRandomizer.InGameAPConsole.AddText($"An unskippable one-minute nap will begin in");
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
            APRandomizer.OWMLModConsole.WriteLine($"beginning forced nap for {inUniverseSecondsAsleep} in-universe seconds at {fastForwardFactor}x speed");

            ScreenPrompt thisIsANapTrapPrompt = new("Unskippable One-Minute Nap In Progress", 0);
            Locator.GetPromptManager().AddScreenPrompt(thisIsANapTrapPrompt, PromptPosition.Center, false);
            thisIsANapTrapPrompt.SetVisibility(true);

            var oldInputMode = OWInput.GetInputMode();
            OWInput.ChangeInputMode(InputMode.None);

            Locator.GetPlayerCamera().GetComponent<PlayerCameraEffectController>().CloseEyes(3f);
            Locator.GetPlayerAudioController().OnStartSleepingAtCampfire(useGreenFireSounds);
            OWTime.SetTimeScale(fastForwardFactor);
            sleepTimerUI.OnStartFastForward();

            await Task.Run(async () => {
                await Task.Delay(inUniverseSecondsAsleep / fastForwardFactor * 1000);
                APRandomizer.OWMLModConsole.WriteLine($"ending forced nap");

                Locator.GetPromptManager().RemoveScreenPrompt(thisIsANapTrapPrompt);

                OWInput.ChangeInputMode(oldInputMode);

                Locator.GetPlayerCamera().GetComponent<PlayerCameraEffectController>().OpenEyes(1f, false);
                Locator.GetPlayerAudioController().OnStopSleepingAtCampfire(false, false); // no gasping sound
                OWTime.SetTimeScale(1);
                sleepTimerUI.OnEndFastForward();

                napTrapInProgress = false;
            });
        });
    }
}
