using HarmonyLib;
using System.Collections.Generic;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class Jellyfish
{
    public static bool hasJellyfishKnowledge = false;

    static HashSet<InsulatingVolume> jellyfishInsulatingVolumes = new();

    public static void Setup()
    {
        // we don't want to retain these references beyond a scene transition / loop reset
        jellyfishInsulatingVolumes.Clear();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(JellyfishController), nameof(JellyfishController.OnSectorOccupantsUpdated))]
    public static void JellyfishController_OnSectorOccupantsUpdated_Prefix(ref JellyfishController __instance)
    {
        var insulators = __instance.gameObject.transform.GetComponentsInChildren<InsulatingVolume>();
        Randomizer.Instance.ModHelper.Console.WriteLine($"JellyfishController.OnSectorOccupantsUpdated found {insulators.Length} insulators");
        jellyfishInsulatingVolumes.UnionWith(insulators);
        ApplyHasKnowledgeFlag();
    }

    public static void SetHasJellyfishKnowledge(bool hasJellyfishKnowledge)
    {
        if (Jellyfish.hasJellyfishKnowledge != hasJellyfishKnowledge)
        {
            Jellyfish.hasJellyfishKnowledge = hasJellyfishKnowledge;
            ApplyHasKnowledgeFlag();
            if (hasJellyfishKnowledge)
            {
                var nd = new NotificationData(NotificationTarget.All, "SPACESUIT ELECTRICAL INSULATION AUGMENTED WITH JELLYFISH MEMBRANES", 10);
                NotificationManager.SharedInstance.PostNotification(nd, false);
            }
        }
    }

    private static void ApplyHasKnowledgeFlag()
    {
        jellyfishInsulatingVolumes.Do(iv => iv.SetVolumeActivation(hasJellyfishKnowledge));
    }

    static ScreenPrompt insulationIntactPrompt = new("Jellyfish Insulation: Intact", 0);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.LateInitialize))]
    public static void ToolModeUI_LateInitialize_Postfix()
    {
        Locator.GetPromptManager().AddScreenPrompt(insulationIntactPrompt, PromptPosition.UpperRight, false);
    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.Update))]
    public static void ToolModeUI_Update_Postfix()
    {
        insulationIntactPrompt.SetVisibility(
            hasJellyfishKnowledge &&
            OWInput.IsInputMode(InputMode.Character) &&
            Locator.GetPlayerSectorDetector().IsWithinSector(Sector.Name.GiantsDeep) &&
            PlayerState.IsCameraUnderwater()
        );
    }
}
