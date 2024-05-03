using HarmonyLib;
using System.Collections.Generic;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class Jellyfish
{
    static HashSet<InsulatingVolume> jellyfishInsulatingVolumes = new();

    public static void OnCompleteSceneLoad(OWScene _scene, OWScene _loadScene)
    {
        // we don't want to retain these references beyond a scene transition / loop reset, or else
        // they become invalid and lead to NullReferenceExceptions when we try using them later
        jellyfishInsulatingVolumes.Clear();
    }

    [HarmonyPrefix, HarmonyPatch(typeof(JellyfishController), nameof(JellyfishController.OnSectorOccupantsUpdated))]
    public static void JellyfishController_OnSectorOccupantsUpdated_Prefix(ref JellyfishController __instance)
    {
        var insulators = __instance.gameObject.transform.GetComponentsInChildren<InsulatingVolume>();
        jellyfishInsulatingVolumes.UnionWith(insulators);
        ApplyHasKnowledgeFlag();
    }

    private static bool _hasJellyfishKnowledge = false;

    public static bool hasJellyfishKnowledge
    {
        get => _hasJellyfishKnowledge;
        set
        {
            if (_hasJellyfishKnowledge != value)
            {
                _hasJellyfishKnowledge = value;
                ApplyHasKnowledgeFlag();
                if (_hasJellyfishKnowledge)
                {
                    var nd = new NotificationData(NotificationTarget.All, "SPACESUIT ELECTRICAL INSULATION AUGMENTED WITH JELLYFISH MEMBRANES", 10);
                    NotificationManager.SharedInstance.PostNotification(nd, false);
                }
            }
        }
    }

    private static void ApplyHasKnowledgeFlag()
    {
        jellyfishInsulatingVolumes.Do(iv => iv.SetVolumeActivation(hasJellyfishKnowledge));
    }

    static ScreenPrompt insulationIntactPrompt = new("Jellyfish Insulation: Intact", 0);

    [HarmonyPostfix, HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.LateInitialize))]
    public static void ToolModeUI_LateInitialize_Postfix()
    {
        Locator.GetPromptManager().AddScreenPrompt(insulationIntactPrompt, PromptPosition.UpperRight, false);
    }
    [HarmonyPostfix, HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.Update))]
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
