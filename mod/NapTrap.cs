using HarmonyLib;
using UnityEngine;

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
                ForceNap();
            _napTraps = value;
        }
    }

    // SleepTimerUI is the class which draws "00:12" in the middle of the screen during normal campfire napping,
    // as well as generates the campfire ember effects on the otherwise black screen.

    static SleepTimerUI sleepTimerUI = null;

    [HarmonyPostfix, HarmonyPatch(typeof(SleepTimerUI), nameof(SleepTimerUI.Awake))]
    public static void SleepTimerUI_Awake(SleepTimerUI __instance) => sleepTimerUI = __instance;

    // SleepTimerUI is the only caller of IsSleepingAtDreamCampfire, and it uses it to decide whether to render
    // "dream embers" instead of regular campfire embers. For nap traps, we'd like to use dream embers
    // if we get meditated anywhere inside the Stranger or the dream world.
    [HarmonyPostfix, HarmonyPatch(typeof(PlayerState), nameof(PlayerState.IsSleepingAtDreamCampfire))]
    public static void PlayerState_IsSleepingAtDreamCampfire_Awake(PlayerState __instance, ref bool __result)
    {
        __result = __result // if the vanilla value is already true, don't risk changing it
            || Locator.GetPlayerSectorDetector().IsWithinSector(Sector.Name.InvisiblePlanet)
            || Locator.GetPlayerSectorDetector().IsWithinSector(Sector.Name.DreamWorld);
    }

    private static NapTrapComponent napTrapComponent = null;
    private static ScreenPrompt thisIsANapTrapPrompt = null;

    // useful for testing
    /*[HarmonyPrefix, HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.Update))]
    public static void ToolModeUI_Update_Prefix()
    {
        if (OWInput.SharedInputManager.IsNewlyPressed(InputLibrary.left2))
        {
            ForceNap();
        }
    }*/

    private static void ForceNap()
    {
        // ignore Nap Traps if we're in menus or credits
        if (LoadManager.GetCurrentScene() != OWScene.SolarSystem && LoadManager.GetCurrentScene() != OWScene.EyeOfTheUniverse)
            return;

        if (napTrapComponent != null)
        {
            APRandomizer.OWMLModConsole.WriteLine($"Ignoring Nap Trap because another Nap Trap is still in progress");
            return;
        }

        if (thisIsANapTrapPrompt == null)
        {
            thisIsANapTrapPrompt = new("Unskippable One-Minute Nap In Progress", 0);
            Locator.GetPromptManager().AddScreenPrompt(thisIsANapTrapPrompt, PromptPosition.Center, false);
        }

        APRandomizer.InGameAPConsole.AddText($"An unskippable one-minute nap will begin in");

        napTrapComponent = Locator.GetPlayerTransform().gameObject.AddComponent<NapTrapComponent>();
    }

    class NapTrapComponent : MonoBehaviour
    {
        private float countdownSeconds = 3;
        private float napSeconds = 60;

        private float secondsSinceTrapActivated = 0;

        private int countdownLogsPrinted = 0;
        private bool isNapping = false;

        private void Update()
        {
            secondsSinceTrapActivated += Time.deltaTime;

            thisIsANapTrapPrompt.SetVisibility(false);

            if (secondsSinceTrapActivated < countdownSeconds)
            {
                // if the trap has not finished the countdown yet, make sure we've printed any countdown messages that are due
                if (countdownLogsPrinted == 0)
                {
                    APRandomizer.InGameAPConsole.AddText($"{countdownSeconds}...");
                    countdownLogsPrinted++;
                }
                else if (countdownLogsPrinted < countdownSeconds && secondsSinceTrapActivated >= countdownLogsPrinted)
                {
                    APRandomizer.InGameAPConsole.AddText($"{countdownSeconds - Mathf.Floor(secondsSinceTrapActivated)}...");
                    countdownLogsPrinted++;
                }
            }
            else if (secondsSinceTrapActivated < (countdownSeconds + napSeconds))
            {
                // if we've entered the napping phase of the nap trap, and aren't already napping, then put the player to sleep
                if (!isNapping)
                {
                    isNapping = true;

                    var useGreenFireSounds = Locator.GetPlayerSectorDetector().IsWithinSector(Sector.Name.InvisiblePlanet)
                        || Locator.GetPlayerSectorDetector().IsWithinSector(Sector.Name.DreamWorld);
                    var fastForwardFactor = 10;

                    APRandomizer.OWMLModConsole.WriteLine($"beginning forced nap for {napSeconds} in-universe seconds at {fastForwardFactor}x speed");

                    Locator.GetPlayerCamera().GetComponent<PlayerCameraEffectController>().CloseEyes(3f);
                    Locator.GetPlayerAudioController().OnStartSleepingAtCampfire(useGreenFireSounds);
                    OWTime.SetTimeScale(fastForwardFactor);
                    sleepTimerUI.OnStartFastForward();
                }

                // This text should be visible whenever we're in the napping phase and the game has not been paused
                if (!OWInput.IsInputMode(InputMode.Menu))
                    thisIsANapTrapPrompt.SetVisibility(true);
            }
            else
            {
                // if the trap has fully run its course, "wake up" the player and destroy this component
                Locator.GetPlayerCamera().GetComponent<PlayerCameraEffectController>().OpenEyes(1f, false);
                Locator.GetPlayerAudioController().OnStopSleepingAtCampfire(false, false); // no gasping sound
                OWTime.SetTimeScale(1);
                sleepTimerUI.OnEndFastForward();

                gameObject.DestroyAllComponents<NapTrapComponent>();
            }
        }
    }
}
