using HarmonyLib;
using System;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class Anglerfish
{
    private static bool _hasAnglerfishKnowledge = false;

    public static bool hasAnglerfishKnowledge
    {
        get => _hasAnglerfishKnowledge;
        set
        {
            if (_hasAnglerfishKnowledge != value)
            {
                _hasAnglerfishKnowledge = value;

                if (_hasAnglerfishKnowledge)
                {
                    var nd = new NotificationData(NotificationTarget.All, "RECONFIGURING SPACESHIP FOR SILENT RUNNING MODE", 10);
                    NotificationManager.SharedInstance.PostNotification(nd, false);
                }
            }
        }
    }

    static bool shipMakingNoise = false;
    static bool playerMakingNoise = false;

    // In the vanilla game, _noiseRadius is always between 0 and 400.
    [HarmonyPostfix, HarmonyPatch(typeof(ShipNoiseMaker), nameof(ShipNoiseMaker.Update))]
    public static void ShipNoiseMaker_Update_Postfix(ref ShipNoiseMaker __instance)
    {
        if (!hasAnglerfishKnowledge)
            __instance._noiseRadius = Math.Max(__instance._noiseRadius, 1000);

        var newShipNoise = __instance._noiseRadius > 0;
        if (newShipNoise != shipMakingNoise)
        {
            shipMakingNoise = newShipNoise;
            UpdatePromptText();
        }
    }
    [HarmonyPostfix, HarmonyPatch(typeof(PlayerNoiseMaker), nameof(PlayerNoiseMaker.Update))]
    public static void PlayerNoiseMaker_Update_Postfix(ref PlayerNoiseMaker __instance)
    {
        if (!hasAnglerfishKnowledge)
            __instance._noiseRadius = Math.Max(__instance._noiseRadius, 400);

        var newPlayerNoise = __instance._noiseRadius > 0;
        if (newPlayerNoise != playerMakingNoise)
        {
            playerMakingNoise = newPlayerNoise;
            UpdatePromptText();
        }
    }

    static string activeText = "Silent Running Mode: <color=green>Active</color>";
    static string inactiveText = "Silent Running Mode: <color=red>Inactive</color>";
    static ScreenPrompt silentRunningPrompt = new(activeText, 0);

    public static void UpdatePromptText()
    {
        if (shipMakingNoise || playerMakingNoise)
            silentRunningPrompt.SetText(inactiveText);
        else
            silentRunningPrompt.SetText(activeText);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.LateInitialize))]
    public static void ToolModeUI_LateInitialize_Postfix()
    {
        Locator.GetPromptManager().AddScreenPrompt(silentRunningPrompt, PromptPosition.UpperRight, false);
    }
    [HarmonyPostfix, HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.Update))]
    public static void ToolModeUI_Update_Postfix()
    {
        silentRunningPrompt.SetVisibility(
            hasAnglerfishKnowledge &&
            (OWInput.IsInputMode(InputMode.Character) || OWInput.IsInputMode(InputMode.ShipCockpit)) &&
            (
                Locator.GetPlayerSectorDetector().IsWithinSector(Sector.Name.DarkBramble) ||
                Locator.GetPlayerSectorDetector().IsWithinSector(Sector.Name.BrambleDimension)
            )
        );
    }
}
