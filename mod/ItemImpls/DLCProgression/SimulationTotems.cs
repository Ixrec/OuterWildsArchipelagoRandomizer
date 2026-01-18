using HarmonyLib;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class SimulationTotems
{
    private static bool _hasTotemPatch = false;

    public static bool hasTotemPatch
    {
        get => _hasTotemPatch;
        set
        {
            _hasTotemPatch = value;

            ApplyTotemPatchFlag(_hasTotemPatch);

            if (_hasTotemPatch)
            {
                var nd = new NotificationData(NotificationTarget.Player, "SIMULATION HACK SUCCESSFUL. APPLIED HOTFIXES TO SIMULATION TOTEMS ENABLING AMPHIBIAN USE.", 10);
                NotificationManager.SharedInstance.PostNotification(nd, false);
            }
        }
    }

    static ScreenPrompt noTotemPatchPrompt = null;
    private static ScreenPrompt getNoTotemPatchPrompt()
    {
        if (noTotemPatchPrompt == null)
        {
            noTotemPatchPrompt = new ScreenPrompt("Requires Dream Totem Patch", 0);
            Locator.GetPromptManager().AddScreenPrompt(noTotemPatchPrompt, PromptPosition.Center, false);
        }
        return noTotemPatchPrompt;
    }
    private static void showNoTotemPatchPrompt()
    {
        var prompt = getNoTotemPatchPrompt();
        if (!prompt.IsVisible())
        {
            APRandomizer.OWMLModConsole.WriteLine($"showing totem patch prompt");
            prompt.SetVisibility(true);

            Task.Run(async () =>
            {
                await Task.Delay(3000);
                APRandomizer.OWMLModConsole.WriteLine($"hiding totem patch prompt");
                noTotemPatchPrompt?.SetVisibility(false);
            });
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(LanternZoomPoint), nameof(LanternZoomPoint.OnDetectLight))]
    public static bool LanternZoomPoint_OnDetectLight(LanternZoomPoint __instance)
    {
        if (!_hasTotemPatch)
        {
            APRandomizer.OWMLModConsole.WriteLine($"LanternZoomPoint_OnDetectLight blocking attempt to zoom");
            showNoTotemPatchPrompt();
            return false; // skip vanilla implementation
        }
        return true; // let vanilla implementation handle it
    }

    private static List<InteractReceiver> projectorIRs = new();

    public static void ApplyTotemPatchFlagToIR(bool hasTotemPatch, InteractReceiver ir)
    {
        if (hasTotemPatch)
        {
            ir.ChangePrompt(UITextType.RoastingExtinguishPrompt);
            ir.SetKeyCommandVisible(true);
        }
        else
        {
            ir.ChangePrompt("Requires Dream Totem Patch");
            ir.SetKeyCommandVisible(false);
        }
    }

    private static void ApplyTotemPatchFlag(bool hasTotemPatch)
    {
        //APRandomizer.OWMLModConsole.WriteLine($"ApplyTotemPatchFlag {hasTotemPatch} for {projectorIRs.Count} IRs");
        foreach (var ir in projectorIRs)
            ApplyTotemPatchFlagToIR(hasTotemPatch, ir);
    }

    [HarmonyPrefix, HarmonyPatch(typeof(DreamObjectProjector), nameof(DreamObjectProjector.Start))]
    public static void DreamObjectProjector_Start(DreamObjectProjector __instance)
    {
        projectorIRs.Add(__instance._interactReceiver);
        ApplyTotemPatchFlagToIR(hasTotemPatch, __instance._interactReceiver);
    }

    [HarmonyPrefix, HarmonyPatch(typeof(DreamObjectProjector), nameof(DreamObjectProjector.FixedUpdate))]
    public static bool DreamObjectProjector_FixedUpdate(DreamObjectProjector __instance)
    {
        if (!_hasTotemPatch) {
            // these lines are copy-pasted from the vanilla impl
            bool flag = __instance._lightSensor.IsIlluminated();
            if (!__instance._lit && flag && !__instance._wasSensorIlluminated)
            {
                APRandomizer.OWMLModConsole.WriteLine($"DreamObjectProjector_FixedUpdate blocked attempt to project a dream object");
                showNoTotemPatchPrompt();
                return false; // skip the vanilla code calling SetLit(true)
            }
        }
        return true; // let vanilla implementation handle it
    }
    // DreamRaftProjector is a subclass of DreamObjectProjector, but its FU() logic is just different enough it needs a unique patch
    [HarmonyPrefix, HarmonyPatch(typeof(DreamRaftProjector), nameof(DreamRaftProjector.FixedUpdate))]
    public static bool DreamRaftProjector_FixedUpdate(DreamRaftProjector __instance)
    {
        if (!_hasTotemPatch)
        {
            // this line is copy-pasted from the vanilla impl
            if (__instance._lightSensor.IsIlluminated())
            {
                if (!getNoTotemPatchPrompt().IsVisible())
                    APRandomizer.OWMLModConsole.WriteLine($"DreamRaftProjector_FixedUpdate blocked attempt to (re)spawn the dream raft");
                showNoTotemPatchPrompt();
                return false; // skip the vanilla code calling SetLit(true)
            }
        }
        return true; // let vanilla implementation handle it
    }

    [HarmonyPrefix, HarmonyPatch(typeof(DreamObjectProjector), nameof(DreamObjectProjector.OnPressInteract))]
    public static bool DreamObjectProjector_OnPressInteract(DreamObjectProjector __instance)
    {
        if (!_hasTotemPatch) {
            APRandomizer.OWMLModConsole.WriteLine($"DreamObjectProjector_OnPressInteract blocked attempt to extinguish a dream object");
            showNoTotemPatchPrompt();
            return false; // skip vanilla implementation
        }
        return true; // let vanilla implementation handle it
    }
}
