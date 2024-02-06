﻿using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class VelocityMatcher
{
    private static bool _hasVelocityMatcher = false;

    public static bool hasVelocityMatcher
    {
        get => _hasVelocityMatcher;
        set
        {
            if (_hasVelocityMatcher != value)
            {
                _hasVelocityMatcher = value;
                ApplyHasVelocityMatcherFlag(_hasVelocityMatcher);
                if (_hasVelocityMatcher)
                {
                    var nd = new NotificationData(NotificationTarget.All, "MATCH VELOCITY FEATURE ADDED TO SUIT AND SHIP", 10);
                    NotificationManager.SharedInstance.PostNotification(nd, false);
                }
            }
        }
    }

    private static ScreenPrompt ShipMVPrompt = null;
    private static ScreenPrompt JetpackMVPrompt = null;
    private static ScreenPrompt LockOnMVPrompt = null;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipPromptController), nameof(ShipPromptController.LateInitialize))]
    public static void ShipPromptController_LateInitialize_Postfix(ShipPromptController __instance)
    {
        ShipMVPrompt = __instance._matchVelocityPrompt;
        ApplyHasVelocityMatcherFlag(_hasVelocityMatcher);
    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(JetpackPromptController), nameof(JetpackPromptController.LateInitialize))]
    public static void JetpackPromptController_LateInitialize_Postfix(JetpackPromptController __instance)
    {
        JetpackMVPrompt = __instance._matchVelocityPrompt;
        ApplyHasVelocityMatcherFlag(_hasVelocityMatcher);
    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(LockOnReticule), nameof(LockOnReticule.Init))]
    public static void LockOnReticule_Init_Postfix(LockOnReticule __instance)
    {
        LockOnMVPrompt = __instance._matchVelocityPrompt;
        ApplyHasVelocityMatcherFlag(_hasVelocityMatcher);
    }

    public static void ApplyHasVelocityMatcherFlag(bool hasVelocityMatcher)
    {
        if (ShipMVPrompt is not null)
        {
            if (hasVelocityMatcher)
            {
                ShipMVPrompt._commandIdList = new List<InputConsts.InputCommandType> { InputLibrary.matchVelocity.CommandType };
                // copy-pasted from the body of ShipPromptController.Awake()
                ShipMVPrompt.SetText("<CMD>" + UITextLibrary.GetString(UITextType.HoldPrompt) + "   " + UITextLibrary.GetString(UITextType.MatchVelocityPrompt));
            }
            else
            {
                ShipMVPrompt._commandIdList = new();
                ShipMVPrompt.SetText("Velocity Matcher Not Available");
            }
        }

        if (JetpackMVPrompt is not null)
        {
            if (hasVelocityMatcher)
            {
                JetpackMVPrompt._commandIdList = new List<InputConsts.InputCommandType> { InputLibrary.matchVelocity.CommandType };
                // copy-pasted from the body of JetpackPromptController.Awake()
                JetpackMVPrompt.SetText(UITextLibrary.GetString(UITextType.MatchVelocityPrompt) + " <CMD>" + UITextLibrary.GetString(UITextType.HoldPrompt) + "  ");
            }
            else
            {
                JetpackMVPrompt._commandIdList = new();
                JetpackMVPrompt.SetText("Velocity Matcher Not Available");
            }
        }

        if (LockOnMVPrompt is not null)
        {
            if (hasVelocityMatcher)
            {
                LockOnMVPrompt._commandIdList = new List<InputConsts.InputCommandType> { InputLibrary.matchVelocity.CommandType };
                // copy-pasted from the body of LockOnReticule.Init()
                LockOnMVPrompt.SetText("<CMD>" + UITextLibrary.GetString(UITextType.HoldPrompt) + "   " + UITextLibrary.GetString(UITextType.MatchVelocityPrompt));
            }
            else
            {
                LockOnMVPrompt._commandIdList = new();
                LockOnMVPrompt.SetText("Velocity Matcher Not Available");
            }
        }
    }

    // Since players probably will not go looking for the "(A) (Hold) Match Velocity" prompt before holding A,
    // we need a more unmissable prompt for when they do try holding A and it "doesn't work".
    static ScreenPrompt velocityMatcherNotAvailableCenterPrompt = new ScreenPrompt("Velocity Matcher Not Available", 0);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.LateInitialize))]
    public static void ToolModeUI_LateInitialize_Postfix()
    {
        Locator.GetPromptManager().AddScreenPrompt(velocityMatcherNotAvailableCenterPrompt, PromptPosition.Center, false);
    }

    // Confusingly, the game's "(A) (Hold) Match Velocity" feature is implemented by a class named Autopilot,
    // which also implements the game's "Autopilot" feature.
    // Additionally, the Autopilot feature's 1st phase is matching the target velocity, so
    // "Match Velocity" in code often refers to Autopilot phase 1.

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Autopilot), nameof(Autopilot.StartMatchVelocity))]
    public static bool Autopilot_StartMatchVelocity_Prefix(Autopilot __instance)
    {
        if (!_hasVelocityMatcher)
        {
            Randomizer.OWMLModConsole.WriteLine($"Autopilot_StartMatchVelocity_Prefix blocking attempt to use the Match Velocity feature");

            velocityMatcherNotAvailableCenterPrompt.SetVisibility(true);

            // unfortunately the prompt manager has no delay features, so this is the only simple solution
            Task.Run(async () => {
                await Task.Delay(3000);
                velocityMatcherNotAvailableCenterPrompt.SetVisibility(false);
            });
        }

        return _hasVelocityMatcher; // if we have the AP item, allow the base game code to run, otherwise skip it
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ThrusterFlameColorSwapper), nameof(ThrusterFlameColorSwapper.Awake))]
    public static void ThrusterFlameColorSwapper_Awake(ThrusterFlameColorSwapper __instance)
    {
        var c = new Color(0, 0, 1);
        Randomizer.OWMLModConsole.WriteLine($"ThrusterFlameColorSwapper_Awake {__instance._baseLightColor} [{__instance._thrusterLights.Length}]{string.Join("|", __instance._thrusterLights.Select(l => l.color))}");
        __instance._baseLightColor = c;
        foreach (var item in __instance._thrusterLights)
            item.color = c;
        Randomizer.OWMLModConsole.WriteLine($"ThrusterFlameColorSwapper_Awake {__instance._baseLightColor} [{__instance._thrusterLights.Length}]{string.Join("|", __instance._thrusterLights.Select(l => l.color))}");
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ThrusterFlameColorSwapper), nameof(ThrusterFlameColorSwapper.SetFlameColor))]
    public static void ThrusterFlameColorSwapper_SetFlameColor(ThrusterFlameColorSwapper __instance)
    {
        Randomizer.OWMLModConsole.WriteLine($"ThrusterFlameColorSwapper_SetFlameColor {__instance._baseLightColor} [{__instance._thrusterLights.Length}]{string.Join("|", __instance._thrusterLights.Select(l => l.color))}");
    }

}
