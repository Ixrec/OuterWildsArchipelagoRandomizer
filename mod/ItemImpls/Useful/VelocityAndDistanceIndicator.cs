using System.Collections.Generic;
using HarmonyLib;
using OWML.ModHelper;
using UnityEngine;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class VelocityAndDistanceIndicator
{
    private static bool _hasVelocityIndicator = false;

    public static bool hasVelocityIndicator
    {
        get => _hasVelocityIndicator;
        set
        {
            if (_hasVelocityIndicator != value)
            {
                _hasVelocityIndicator = value;
                if (_hasVelocityIndicator)
                {
                    var nd = new NotificationData(NotificationTarget.All, "VELOCITY INDICATOR INSTALLED", 10);
                    NotificationManager.SharedInstance.PostNotification(nd, false);
                }
            }
        }
    }

    private static bool _hasDistanceIndicator = false;

    public static bool hasDistanceIndicator
    {
        get => _hasDistanceIndicator;
        set
        {
            if (_hasDistanceIndicator != value)
            {
                _hasDistanceIndicator = value;
                if (_hasDistanceIndicator)
                {
                    var nd = new NotificationData(NotificationTarget.All, "DISTANCE INDICATOR INSTALLED", 10);
                    NotificationManager.SharedInstance.PostNotification(nd, false);
                }
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(LockOnReticule), nameof(LockOnReticule.EnableMotionLines))]
    public static void LockOnReticule_EnableMotionLines_Prefix(ref bool value)
    {
        if (!hasVelocityIndicator)
            value = false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(OffScreenIndicator), nameof(OffScreenIndicator.SetText))]
    public static void OffScreenIndicator_SetText_Prefix(ref string text)
    {
        text = ModifyIndicatorText(text);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(LockOnReticule), nameof(LockOnReticule.SetReadoutText))]
    public static void LockOnReticule_SetReadoutText_Prefix(ref string s)
    {
        s = ModifyIndicatorText(s);
    }

    private static string ModifyIndicatorText(string text)
    {
        if (hasVelocityIndicator && hasDistanceIndicator)
            return text;
        var lines = new List<string>(text.Split('\n'));
        var lineCount = lines.Count;
        if (lineCount < 2)
            return text;
        if (!hasVelocityIndicator)
            lines.RemoveAt(lineCount - 1);
        if (!hasDistanceIndicator)
            lines.RemoveAt(lineCount - 2);
        return lines.Join(null, "\n");
    }

    private static ScreenPrompt ShipMVPrompt = null;
    private static ScreenPrompt noVelocityIndicatorPrompt = null;
    private static ScreenPrompt noDistanceIndicatorPrompt = null;

    [HarmonyPostfix, HarmonyPatch(typeof(ShipPromptController), nameof(ShipPromptController.LateInitialize))]
    public static void ShipPromptController_LateInitialize_Postfix(ShipPromptController __instance)
    {
        ShipMVPrompt = __instance._matchVelocityPrompt;

        noVelocityIndicatorPrompt = new ScreenPrompt("Velocity Indicator Not Available", 0);
        Locator.GetPromptManager().AddScreenPrompt(noVelocityIndicatorPrompt, PromptPosition.UpperLeft, false);
        noDistanceIndicatorPrompt = new ScreenPrompt("Distance Indicator Not Available", 0);
        Locator.GetPromptManager().AddScreenPrompt(noDistanceIndicatorPrompt, PromptPosition.UpperLeft, false);
    }
    [HarmonyPostfix, HarmonyPatch(typeof(ShipPromptController), nameof(ShipPromptController.Update))]
    public static void ShipPromptController_Update_Postfix(ShipPromptController __instance)
    {
        noVelocityIndicatorPrompt.SetVisibility(!_hasVelocityIndicator && ShipMVPrompt.IsVisible());
        noDistanceIndicatorPrompt.SetVisibility(!_hasDistanceIndicator && ShipMVPrompt.IsVisible());
    }
    [HarmonyPostfix, HarmonyPatch(typeof(ShipPromptController), nameof(ShipPromptController.HideAllPrompts))]
    public static void ShipPromptController_HideAllPrompts(ShipPromptController __instance)
    {
        noVelocityIndicatorPrompt.SetVisibility(false);
        noDistanceIndicatorPrompt.SetVisibility(false);
    }
}
