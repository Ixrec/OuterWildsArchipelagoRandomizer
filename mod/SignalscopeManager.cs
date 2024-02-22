using HarmonyLib;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class SignalscopeManager
{
    private static bool _hasSignalscope = false;

    public static bool hasSignalscope
    {
        get => _hasSignalscope;
        set
        {
            if (_hasSignalscope != value)
            {
                _hasSignalscope = value;
                ApplyHasSignalscopeFlag(_hasSignalscope);
            }
        }
    }

    // The signalscope is the only tool in vanilla which you can equip without even wearing the suit, and thus
    // by far the easiest tool to try equipping without any "[Y] Equip Signalscope" prompt for me to edit.

    // So this "duplicate" prompt is for when the player presses Y and I know there won't be an existing prompt about it.
    static ScreenPrompt signalscopeNotAvailablePrompt = new ScreenPrompt("Signalscope Not Available", 0);

    [HarmonyPostfix, HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.LateInitialize))]
    public static void ToolModeUI_LateInitialize_Postfix()
    {
        Locator.GetPromptManager().AddScreenPrompt(signalscopeNotAvailablePrompt, PromptPosition.Center, false);
    }

    [HarmonyPrefix, HarmonyPatch(typeof(ToolModeSwapper), nameof(ToolModeSwapper.EquipToolMode))]
    public static bool ToolModeSwapper_EquipToolMode_Prefix(ToolMode mode)
    {
        if (mode == ToolMode.SignalScope && !hasSignalscope)
        {
            APRandomizer.OWMLModConsole.WriteLine($"blocked attempt to equip Signalscope");

            if (!Locator.GetPlayerSuit().IsWearingSuit() && !OWInput.IsInputMode(InputMode.ShipCockpit))
            {
                signalscopeNotAvailablePrompt.SetVisibility(true);

                // not the most robust delay code, but this is already a corner case and
                // the prompt manager has no delay features, so not worth investing in this
                Task.Run(async () => {
                    await Task.Delay(3000);
                    signalscopeNotAvailablePrompt.SetVisibility(false);
                });
            }

            return false;
        }

        return true;
    }

    // These are the Signalscope prompts which do exist in the vanilla game for me to edit.

    static ScreenPrompt equipSignalscopePrompt = null;
    static ScreenPrompt centerEquipSignalScopePrompt = null; // only shown in specific places, e.g. hide & seek

    [HarmonyPostfix, HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.Start))]
    public static void ToolModeUI_Start_Postfix(ToolModeUI __instance)
    {
        equipSignalscopePrompt = __instance._signalscopePrompt;
        centerEquipSignalScopePrompt = __instance._centerSignalscopePrompt;

        ApplyHasSignalscopeFlag(hasSignalscope);
    }

    public static void ApplyHasSignalscopeFlag(bool hasSignalscope)
    {
        if (equipSignalscopePrompt == null || centerEquipSignalScopePrompt == null) return;

        if (hasSignalscope)
        {
            equipSignalscopePrompt._commandIdList = new List<InputConsts.InputCommandType> { InputLibrary.signalscope.CommandType };
            centerEquipSignalScopePrompt._commandIdList = new List<InputConsts.InputCommandType> { InputLibrary.signalscope.CommandType };
            // copy-pasted from the body of ToolModeUI.Start()
            equipSignalscopePrompt.SetText(UITextLibrary.GetString(UITextType.SignalscopePrompt) + "   <CMD>");
            centerEquipSignalScopePrompt.SetText(UITextLibrary.GetString(UITextType.SignalscopePrompt) + "   <CMD>");
        }
        else
        {
            equipSignalscopePrompt._commandIdList = new();
            centerEquipSignalScopePrompt._commandIdList = new();
            equipSignalscopePrompt.SetText("Signalscope Not Available");
            centerEquipSignalScopePrompt.SetText("Signalscope Not Available");
        }
    }
}
