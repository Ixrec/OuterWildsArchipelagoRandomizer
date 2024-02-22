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
            {
                _hasTranslator = value;
                ApplyHasTranslatorFlag(_hasTranslator);
            }
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

    [HarmonyPostfix, HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.Start))]
    public static void ToolModeUI_Start_Postfix(ToolModeUI __instance)
    {
        translatePrompt = __instance._centerTranslatePrompt;

        ApplyHasTranslatorFlag(hasTranslator);
    }

    // Because of the auto-equip translator setting, the enabling/disabling of the translate
    // prompt is a little more complex than usual and gets evaluated in Update().
    [HarmonyPostfix, HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.Update))]
    public static void ToolModeUI_Update_Postfix(ToolModeUI __instance)
    {
        // This is mostly a copy-paste of the body of ToolModeSwapper.IsTranslatorEquipPromptAllowed(),
        // but with our hasTranslator flag inserted in the right place.
        if (
            __instance._toolSwapper.IsNomaiTextInFocus() &&
            __instance._toolSwapper._currentToolMode != ToolMode.Translator && 
            (!hasTranslator || !__instance._toolSwapper.GetAutoEquipTranslator() || __instance._toolSwapper._waitForLoseNomaiTextFocus) &&
            OWInput.IsInputMode(InputMode.Character)
        ) {
            translatePrompt.SetVisibility(true);
        }
    }

    public static void ApplyHasTranslatorFlag(bool hasTranslator)
    {
        if (translatePrompt == null) return;

        if (hasTranslator)
        {
            translatePrompt._commandIdList = new List<InputConsts.InputCommandType> { InputLibrary.interact.CommandType };
            // copy-pasted from the body of ToolModeUI.Start()
            translatePrompt.SetText("<CMD>   " + UITextLibrary.GetString(UITextType.TranslatorPrompt));
        }
        else
        {
            translatePrompt._commandIdList = new();
            translatePrompt.SetText("Translator Not Available");
        }
    }
}
