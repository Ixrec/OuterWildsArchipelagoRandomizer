using HarmonyLib;

namespace ArchipelagoRandomizer;

// Both GhostMatter and QuantumImaging affect the content of this single prompt,
// so it makes more sense to give it its own tiny class than have one of those
// two pretend to fully own it.

[HarmonyPatch]
internal class CameraRangePrompt
{
    static ScreenPrompt cameraEMRangePrompt = new("", 0);

    [HarmonyPostfix, HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.LateInitialize))]
    public static void ToolModeUI_LateInitialize_Postfix()
    {
        Locator.GetPromptManager().AddScreenPrompt(cameraEMRangePrompt, PromptPosition.UpperRight, false);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.Update))]
    public static void ToolModeUI_Update_Postfix()
    {
        var text = "Camera EM Range: Visible";
        if (GhostMatterWavelength.hasGhostMatterKnowledge) text += " & Ghost Matter";
        if (QuantumImaging.hasImagingKnowledge) text += " & Quantum";
        cameraEMRangePrompt.SetText(text);

        cameraEMRangePrompt.SetVisibility(
            (GhostMatterWavelength.hasGhostMatterKnowledge || QuantumImaging.hasImagingKnowledge) &&
            (OWInput.IsInputMode(InputMode.Character) || OWInput.IsInputMode(InputMode.ShipCockpit)) &&
            Locator.GetToolModeSwapper().IsInToolMode(ToolMode.Probe)
        );
    }
}
