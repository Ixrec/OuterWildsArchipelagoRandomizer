using HarmonyLib;
using System.Collections.Generic;
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

            ApplyModulatorFlag(_hasLightModulator);

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
    private static void showPromptIfPlayerClose(Vector3 lightSensorPosition)
    {
        var prompt = getNoLightModulatorPrompt();
        if (!prompt.IsVisible())
        {
            var distanceFromPlayer = (Locator.GetPlayerBody().transform.position - lightSensorPosition).magnitude;
            if (distanceFromPlayer > 10) return;

            APRandomizer.OWMLModConsole.WriteLine($"showing light modulator prompt");
            prompt.SetVisibility(true);

            Task.Run(async () =>
            {
                await Task.Delay(3000);
                APRandomizer.OWMLModConsole.WriteLine($"hiding light modulator prompt");
                prompt?.SetVisibility(false);
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
                showPromptIfPlayerClose(__instance.transform.position);
            }
        }
    }

    private static List<InteractReceiver> raftPushIRs = new();

    public static void ApplyModulatorFlagToIR(bool hasModulator, InteractReceiver ir)
    {
        if (hasModulator)
        {
            ir.ChangePrompt(UITextType.PushRaftPrompt);
            ir.SetKeyCommandVisible(true);
        }
        else
        {
            ir.ChangePrompt("Requires Stranger Light Modulator");
            ir.SetKeyCommandVisible(false);
        }
    }

    private static void ApplyModulatorFlag(bool hasModulator)
    {
        //APRandomizer.OWMLModConsole.WriteLine($"ApplyModulatorFlag {hasModulator} for {raftPushIRs.Count} IRs");
        foreach (var ir in raftPushIRs)
            ApplyModulatorFlagToIR(hasModulator, ir);
    }

    // RaftController.Start() -> Initialize() calls ir.SetPromptText(), so we have to edit it after that
    [HarmonyPostfix, HarmonyPatch(typeof(RaftController), nameof(RaftController.Start))]
    public static void RaftController_Start_Postfix(RaftController __instance)
    {
        // RaftController._lightSensors' type is the abstract LightSensor class, but in practice it's also only using SingleLightSensors
        foreach (var ls in __instance._lightSensors)
            sensorsNeedingModulator.Add(ls.GetComponent<SingleLightSensor>());

        var ir = __instance._interactReceiver;
        if (ir)
        {
            raftPushIRs.Add(ir);
            ApplyModulatorFlagToIR(_hasLightModulator, ir);
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(RaftController), nameof(RaftController.OnPressInteract))]
    public static bool RaftController_OnPressInteract(RaftController __instance)
    {
        if (!_hasLightModulator)
            return false; // skip vanilla impl so the raft does not get pushed

        return true;
    }
}
