using HarmonyLib;
using System.Collections.Generic;
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
                if (_hasVelocityMatcher)
                {
                    var nd = new NotificationData(NotificationTarget.All, "MATCH VELOCITY FEATURE ADDED TO SUIT AND SHIP", 10);
                    NotificationManager.SharedInstance.PostNotification(nd, false);
                }
            }
        }
    }

    private static ScreenPrompt ShipMVPrompt = null;
    private static ScreenPrompt ShipCannotMVPrompt = null;

    [HarmonyPostfix, HarmonyPatch(typeof(ShipPromptController), nameof(ShipPromptController.LateInitialize))]
    public static void ShipPromptController_LateInitialize_Postfix(ShipPromptController __instance)
    {
        ShipMVPrompt = __instance._matchVelocityPrompt;

        ShipCannotMVPrompt = new ScreenPrompt("Velocity Matcher Not Available", 0);
        Locator.GetPromptManager().AddScreenPrompt(ShipCannotMVPrompt, PromptPosition.UpperLeft, false);
    }
    [HarmonyPostfix, HarmonyPatch(typeof(ShipPromptController), nameof(ShipPromptController.Update))]
    public static void ShipPromptController_Update_Postfix(ShipPromptController __instance)
    {
        ShipCannotMVPrompt.SetVisibility(false);
        if (ShipMVPrompt.IsVisible() && !_hasVelocityMatcher)
        {
            ShipMVPrompt.SetVisibility(false);
            ShipCannotMVPrompt.SetVisibility(true);
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(ShipPromptController), nameof(ShipPromptController.HideAllPrompts))]
    public static void ShipPromptController_HideAllPrompts(ShipPromptController __instance)
    {
        ShipCannotMVPrompt.SetVisibility(false);
    }

    private static ScreenPrompt JetpackMVPrompt = null;
    private static ScreenPrompt JetpackCannotMVPrompt = null;

    [HarmonyPostfix, HarmonyPatch(typeof(JetpackPromptController), nameof(JetpackPromptController.LateInitialize))]
    public static void JetpackPromptController_LateInitialize_Postfix(JetpackPromptController __instance)
    {
        JetpackMVPrompt = __instance._matchVelocityPrompt;

        JetpackCannotMVPrompt = new ScreenPrompt("Velocity Matcher Not Available", 0);
        Locator.GetPromptManager().AddScreenPrompt(JetpackCannotMVPrompt, PromptPosition.UpperRight, false);
    }
    [HarmonyPostfix, HarmonyPatch(typeof(JetpackPromptController), nameof(JetpackPromptController.Update))]
    public static void JetpackPromptController_Update_Postfix(JetpackPromptController __instance)
    {
        JetpackCannotMVPrompt.SetVisibility(false);
        if (JetpackMVPrompt.IsVisible() && !_hasVelocityMatcher)
        {
            JetpackMVPrompt.SetVisibility(false);
            JetpackCannotMVPrompt.SetVisibility(true);
        }
    }

    private static ScreenPrompt LockOnMVPrompt = null;
    private static ScreenPrompt LockOnCannotMVPrompt = null;

    [HarmonyPostfix, HarmonyPatch(typeof(LockOnReticule), nameof(LockOnReticule.Init))]
    public static void LockOnReticule_Init_Postfix(LockOnReticule __instance)
    {
        LockOnMVPrompt = __instance._matchVelocityPrompt;

        LockOnCannotMVPrompt = new ScreenPrompt("Velocity Matcher Not Available", 0);
        Locator.GetPromptManager().AddScreenPrompt(LockOnCannotMVPrompt, __instance._promptListBlock, TextAnchor.MiddleLeft, -1, false);
    }
    [HarmonyPostfix, HarmonyPatch(typeof(LockOnReticule), nameof(LockOnReticule.UpdateScreenPrompts))]
    public static void LockOnReticule_UpdateScreenPrompts_Postfix(LockOnReticule __instance)
    {
        LockOnCannotMVPrompt.SetVisibility(false);
        if (LockOnMVPrompt.IsVisible() && !_hasVelocityMatcher)
        {
            LockOnMVPrompt.SetVisibility(false);
            LockOnCannotMVPrompt.SetVisibility(true);
        }
    }

    // Since players probably will not go looking for the "(A) (Hold) Match Velocity" prompt before holding A,
    // we need a more unmissable prompt for when they do try holding A and it "doesn't work".
    static ScreenPrompt velocityMatcherNotAvailableCenterPrompt = new ScreenPrompt("Velocity Matcher Not Available", 0);

    [HarmonyPostfix, HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.LateInitialize))]
    public static void ToolModeUI_LateInitialize_Postfix()
    {
        Locator.GetPromptManager().AddScreenPrompt(velocityMatcherNotAvailableCenterPrompt, PromptPosition.Center, false);
    }

    // In the base game code, "Autopilot" and "Match Velocity" are used very confusingly.
    // First, the game's Autopilot class implements both the "(Up) Autopilot" feature and the
    // "(A) (Hold) Match Velocity" feature. This file is only interested in the latter.

    // The autopilot feature works in three stages. In-game, these are described by the ship console as:
    // - Stage 1: Aligning Flight Trajectory
    // - Stage 2: Accelerating Toward Destination
    // - Stage 3: Firing Retro-Rockets

    // In the Autopilot class' code, these three stages correspond to:
    // - IsLiningUpDestination() being true
    // - IsApproachingDestination() being true
    // - IsMatchingVelocity() being true, and firing OnFireRetroRockets and OnInitMatchVelocity

    // So while the term "match velocity" is conceptually the best fit for stage 1, in code only stage 3
    // is described this way, and Autopilot stage 3 is what goes through the same StartMatchVelocity()
    // method as the Match Velocity feature.

    [HarmonyPrefix, HarmonyPatch(typeof(Autopilot), nameof(Autopilot.StartMatchVelocity))]
    public static bool Autopilot_StartMatchVelocity_Prefix(Autopilot __instance)
    {
        if (__instance._isShipAutopilot && __instance._isFlyingToDestination)
            // This is the "(Up) Autopilot" feature entering its third and final stage,
            // not the "(A) (Hold) Match Velocity feature we want to block here.
            // So we immediately return control to the base game code.
            return true;

        if (!_hasVelocityMatcher)
        {
            velocityMatcherNotAvailableCenterPrompt.SetVisibility(true);

            // unfortunately the prompt manager has no delay features, so this is the only simple solution
            Task.Run(async () => {
                await Task.Delay(3000);
                velocityMatcherNotAvailableCenterPrompt.SetVisibility(false);
            });
        }

        return _hasVelocityMatcher; // if we have the AP item, allow the base game code to run, otherwise skip it
    }

}
