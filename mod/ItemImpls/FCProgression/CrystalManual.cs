using HarmonyLib;

namespace ArchipelagoRandomizer.ItemImpls.FCProgression;

[HarmonyPatch]
internal class CrystalManual
{
    private static bool _hasCrystalManual = false;

    public static bool hasCrystalManual
    {
        get => _hasCrystalManual;
        set
        {
            if (_hasCrystalManual != value)
                _hasCrystalManual = value;
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(ItemTool), nameof(ItemTool.SocketItem))]
    private static bool SocketItem(ItemTool __instance, OWItemSocket socket)
    {
        if (APRandomizer.NewHorizonsAPI == null) return true;
        if (APRandomizer.NewHorizonsAPI.GetCurrentStarSystem() != "DeepBramble") return true;
        if (__instance._heldItem.name.Contains("crystal") && !hasCrystalManual)
        {
            APRandomizer.OWMLModConsole.WriteLine($"blocking attempt to insert FC gravity crystal into a socket");
            return false;
        }
        return true;
    }
    [HarmonyPrefix, HarmonyPatch(typeof(ItemTool), nameof(ItemTool.StartUnsocketItem))]
    private static bool StartUnsocketItem(ItemTool __instance, OWItemSocket socket)
    {
        if (APRandomizer.NewHorizonsAPI == null) return true;
        if (APRandomizer.NewHorizonsAPI.GetCurrentStarSystem() != "DeepBramble") return true;
        if (socket.GetSocketedItem().name.Contains("crystal") && !hasCrystalManual)
        {
            APRandomizer.OWMLModConsole.WriteLine($"blocking attempt to remove FC gravity crystal from its socket");
            return false;
        }
        return true;
    }
    [HarmonyPrefix, HarmonyPatch(typeof(ItemTool), nameof(ItemTool.UpdateInteract))]
    private static bool UpdateInteract(ItemTool __instance, FirstPersonManipulator firstPersonManipulator, bool inputBlocked)
    {
        if (APRandomizer.NewHorizonsAPI == null) return true;
        if (APRandomizer.NewHorizonsAPI.GetCurrentStarSystem() != "DeepBramble") return true;
        if (OWInput.IsNewlyPressed(InputLibrary.interact, InputMode.All) && !hasCrystalManual)
        {
            var item = firstPersonManipulator.GetFocusedOWItem();
            if (item?.name != null)
                if (item.name.Contains("crystal"))
                {
                    APRandomizer.OWMLModConsole.WriteLine($"blocking attempt to interact with a FC gravity crystal");
                    return false;
                }
        }
        return true;
    }

    private static FirstPersonManipulator firstPersonManipulator = null;

    [HarmonyPostfix, HarmonyPatch(typeof(ToolModeSwapper), nameof(ToolModeSwapper.Start))]
    public static void ToolModeSwapper_Start_Postfix(ToolModeSwapper __instance)
    {
        firstPersonManipulator = __instance._firstPersonManipulator;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(ItemTool), nameof(ItemTool.UpdateState))]
    public static void ItemTool_UpdateState_Postfix(ItemTool __instance, ItemTool.PromptState newState, string itemName)
    {
        if (APRandomizer.NewHorizonsAPI == null) return;
        if (APRandomizer.NewHorizonsAPI.GetCurrentStarSystem() != "DeepBramble") return;
        if ((newState == ItemTool.PromptState.SOCKET || newState == ItemTool.PromptState.UNSOCKET ||
            newState == ItemTool.PromptState.PICK_UP) && !hasCrystalManual)
        {
            OWItem item = null;
            if (newState == ItemTool.PromptState.SOCKET)
                item = __instance._heldItem;
            else if (newState == ItemTool.PromptState.UNSOCKET)
                item = firstPersonManipulator.GetFocusedItemSocket().GetSocketedItem();
            else if (newState == ItemTool.PromptState.PICK_UP)
                item = firstPersonManipulator.GetFocusedOWItem();

            if (item.name.Contains("crystal"))
            {
                __instance._interactButtonPrompt.SetVisibility(false);

                __instance._messageOnlyPrompt.SetText("Requires Crystal Repair Manual");
                __instance._messageOnlyPrompt.SetVisibility(true);
                return;
            }
        }
    }
}
