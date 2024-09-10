using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class StrangerLightModulator
{
    private static bool _hasLightModulator = false;

    public static bool hasLightModulator
    {
        get => _hasLightModulator;
        set
        {
            _hasLightModulator = value;

            if (_hasLightModulator)
            {
                var nd = new NotificationData(NotificationTarget.Player, "FLASHLIGHT AND SCOUT LIGHTS UPGRADED TO CONTROL DEVICES INSIDE THE STRANGER USING THE INHABITANTS' EYESHINE WAVELENGTH.", 10);
                NotificationManager.SharedInstance.PostNotification(nd, false);
            }
        }
    }


    static ScreenPrompt noLightModulatorPrompt = null;
    private static ScreenPrompt getNoLightModulatorPrompt()
    {
        if (noLightModulatorPrompt == null)
        {
            noLightModulatorPrompt = new ScreenPrompt("Requires Stranger Light Modulator", 0);
            Locator.GetPromptManager().AddScreenPrompt(noLightModulatorPrompt, PromptPosition.Center, false);
        }
        return noLightModulatorPrompt;
    }
    private static void showNoLightModulatorPrompt()
    {
        var prompt = getNoLightModulatorPrompt();
        if (!prompt.IsVisible())
        {
            APRandomizer.OWMLModConsole.WriteLine($"showing light modulator prompt");
            prompt.SetVisibility(true);

            Task.Run(async () =>
            {
                await Task.Delay(3000);
                APRandomizer.OWMLModConsole.WriteLine($"hiding light modulator prompt");
                noLightModulatorPrompt?.SetVisibility(false);
            });
        }
    }

    // This does include dream sensors, because those are easier to exclude with a runtime check in the sensor patch below.
    // This does NOT include the airlock light sensors, which we want to work whether or not you have the item.
    // This does NOT include the lab door sensors, which we want to ignore light entirely in favor of the Breach Override Codes item.
    private static List<SingleLightSensor> sensorsNeedingModulator = new();

    // This is for the lab door sensors, which we want to ignore light entirely in favor of the Breach Override Codes item.
    private static List<SingleLightSensor> disabledSensors = new();

    [HarmonyPrefix, HarmonyPatch(typeof(EclipseElevatorController), nameof(EclipseElevatorController.Awake))]
    public static void EclipseElevatorController_Awake(EclipseElevatorController __instance) =>
        sensorsNeedingModulator.AddRange(__instance._lightSensors);

    [HarmonyPrefix, HarmonyPatch(typeof(EclipseDoorController), nameof(EclipseDoorController.Awake))]
    public static void EclipseDoorController_Awake(EclipseDoorController __instance)
    {
        if (__instance.transform.parent.name == "HullBreachController" || __instance.transform.parent.name == "Interactibles_SecretEntrance")
            disabledSensors.AddRange( __instance._lightSensors);
        else
            sensorsNeedingModulator.AddRange(__instance._lightSensors);
    }

    [HarmonyPrefix, HarmonyPatch(typeof(RaftController), nameof(RaftController.Awake))]
    public static void RaftController_Awake(RaftController __instance)
    {
        // RaftController._lightSensors' type is the abstract LightSensor class, but in practice it's also only using SingleLightSensors
        foreach (var ls in __instance._lightSensors)
            sensorsNeedingModulator.Add(ls.GetComponent<SingleLightSensor>());
    }

    [HarmonyPrefix, HarmonyPatch(typeof(SingleLightSensor), nameof(SingleLightSensor.UpdateIllumination))]
    public static bool SingleLightSensor_UpdateIllumination_Prefix(SingleLightSensor __instance)
    {
        if (disabledSensors.Contains(__instance))
        {
            __instance._illuminated = false;
            return false; // don't bother computing whether it's illuminated or not
        }
        return true;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(SingleLightSensor), nameof(SingleLightSensor.UpdateIllumination))]
    public static void SingleLightSensor_UpdateIllumination_Postfix(SingleLightSensor __instance) {
        if (!_hasLightModulator && (!Locator.GetDreamWorldController()?.IsInDream() ?? false) && sensorsNeedingModulator.Contains(__instance))
        {
            if (__instance._illuminated)
            {
                __instance._illuminated = false;
                showNoLightModulatorPrompt();
            }
        }
    }
}
