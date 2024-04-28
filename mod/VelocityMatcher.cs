using HarmonyLib;
using System.Collections.Generic;
using System.Threading.Tasks;

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

    [HarmonyPostfix, HarmonyPatch(typeof(ShipPromptController), nameof(ShipPromptController.LateInitialize))]
    public static void ShipPromptController_LateInitialize_Postfix(ShipPromptController __instance)
    {
        ShipMVPrompt = __instance._matchVelocityPrompt;
        ApplyHasVelocityMatcherFlag(_hasVelocityMatcher);
    }
    [HarmonyPostfix, HarmonyPatch(typeof(JetpackPromptController), nameof(JetpackPromptController.LateInitialize))]
    public static void JetpackPromptController_LateInitialize_Postfix(JetpackPromptController __instance)
    {
        JetpackMVPrompt = __instance._matchVelocityPrompt;
        ApplyHasVelocityMatcherFlag(_hasVelocityMatcher);
    }
    [HarmonyPostfix, HarmonyPatch(typeof(LockOnReticule), nameof(LockOnReticule.Init))]
    public static void LockOnReticule_Init_Postfix(LockOnReticule __instance)
    {
        LockOnMVPrompt = __instance._matchVelocityPrompt;
        ApplyHasVelocityMatcherFlag(_hasVelocityMatcher);
    }

    public static void ApplyHasVelocityMatcherFlag(bool hasVelocityMatcher)
    {
        if (ShipMVPrompt != null)
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

        if (JetpackMVPrompt != null)
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

        if (LockOnMVPrompt != null)
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
            // This is the "(Up) Autopilot" feature entering its thir and final stage,
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
