using HarmonyLib;
using System.Collections.Generic;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class Translator
{
    private static bool _hasTranslator = false;

    public static bool hasTranslator
    {
        get => _hasTranslator;
        set
        {
            if (_hasTranslator != value)
                _hasTranslator = value;
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(ToolModeSwapper), nameof(ToolModeSwapper.EquipToolMode))]
    public static bool ToolModeSwapper_EquipToolMode_Prefix(ToolMode mode)
    {
        if (mode == ToolMode.Translator && !hasTranslator)
            return false;
        return true;
    }

    static ScreenPrompt translatePrompt = null;
    static ScreenPrompt cannotTranslatePrompt = null;

    [HarmonyPostfix, HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.Start))]
    public static void ToolModeUI_Start_Postfix(ToolModeUI __instance)
    {
        translatePrompt = __instance._centerTranslatePrompt;

        cannotTranslatePrompt = new ScreenPrompt("Translator Not Available", 0);
        Locator.GetPromptManager().AddScreenPrompt(cannotTranslatePrompt, PromptPosition.Center, false);
    }

    // Because of the auto-equip translator setting, the enabling/disabling of the translate
    // prompt is a little more complex than usual and gets evaluated in Update().
    [HarmonyPostfix, HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.Update))]
    public static void ToolModeUI_Update_Postfix(ToolModeUI __instance)
    {
        cannotTranslatePrompt.SetVisibility(false);

        // If the vanilla translate prompt is already being displayed, then this is like every other file
        // that needs to change a UI prompt: just swap to the other prompt if we don't have the item yet.
        if (translatePrompt.IsVisible())
        {
            if (!_hasTranslator)
            {
                translatePrompt.SetVisibility(false);
                cannotTranslatePrompt.SetVisibility(true);
            }
            return;
        }

        // But if the vanilla game would've auto-equipped the translator, then we need to prevent that
        // to show our "Translator Not Available" prompt instead.
        // This is mostly a copy-paste of the body of ToolModeSwapper.IsTranslatorEquipPromptAllowed(),
        // but with our hasTranslator flag inserted in the right place.
        if (
            __instance._toolSwapper.IsNomaiTextInFocus() &&
            __instance._toolSwapper._currentToolMode != ToolMode.Translator &&
            (!hasTranslator || !__instance._toolSwapper.GetAutoEquipTranslator() || __instance._toolSwapper._waitForLoseNomaiTextFocus) &&
            OWInput.IsInputMode(InputMode.Character)
        )
        {
            translatePrompt.SetVisibility(_hasTranslator);
            cannotTranslatePrompt.SetVisibility(!_hasTranslator);
        }
    }
}
