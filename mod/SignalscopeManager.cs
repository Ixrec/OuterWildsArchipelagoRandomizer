using HarmonyLib;
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
            Randomizer.OWMLModConsole.WriteLine($"blocked attempt to equip Signalscope");

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

    // These are the two Signalscope prompts which do exist in the vanilla game for me to edit,
    // along with two replacements for them created by us.
    // Turns out the safe way to remove a button from a UI prompt is to switch between two prompts.

    static ScreenPrompt equipSignalscopePrompt = null;
    static ScreenPrompt cannotEquipSignalscopePrompt = null;
    // only shown in specific places, e.g. hide & seek
    static ScreenPrompt centerEquipSignalScopePrompt = null;
    static ScreenPrompt centerCannotEquipSignalScopePrompt = null;

    [HarmonyPostfix, HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.Start))]
    public static void ToolModeUI_Start_Postfix(ToolModeUI __instance)
    {
        equipSignalscopePrompt = __instance._signalscopePrompt;
        centerEquipSignalScopePrompt = __instance._centerSignalscopePrompt;

        cannotEquipSignalscopePrompt = new ScreenPrompt("Signalscope Not Available", 0);
        Locator.GetPromptManager().AddScreenPrompt(cannotEquipSignalscopePrompt, PromptPosition.UpperRight, false);
        centerCannotEquipSignalScopePrompt = new ScreenPrompt("Signalscope Not Available", 0);
        Locator.GetPromptManager().AddScreenPrompt(centerCannotEquipSignalScopePrompt, PromptPosition.Center, false);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.Update))]
    public static void ToolModeUI_Update_Postfix(ToolModeUI __instance)
    {
        cannotEquipSignalscopePrompt.SetVisibility(false);
        if (equipSignalscopePrompt.IsVisible() && !_hasSignalscope)
        {
            equipSignalscopePrompt.SetVisibility(false);
            cannotEquipSignalscopePrompt.SetVisibility(true);
        }

        centerCannotEquipSignalScopePrompt.SetVisibility(false);
        if (centerEquipSignalScopePrompt.IsVisible() && !_hasSignalscope)
        {
            centerEquipSignalScopePrompt.SetVisibility(false);
            centerCannotEquipSignalScopePrompt.SetVisibility(true);
        }
    }
}
