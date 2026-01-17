using HarmonyLib;

namespace ArchipelagoRandomizer.ItemImpls.FCProgression;

[HarmonyPatch]
internal class CrystalManual
{
    private static bool _hasCrystalManual = false;

    public static bool HasCrystalManual
    {
        get => _hasCrystalManual;
        set
        {
            if (_hasCrystalManual != value)
                _hasCrystalManual = value;
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(ItemTool), nameof(ItemTool.SocketItem))]
    private static bool SocketItem(ItemTool __instance)
    {
        if (APRandomizer.NewHorizonsAPI == null) return true;
        if (APRandomizer.NewHorizonsAPI.GetCurrentStarSystem() != "DeepBramble") return true;
        if (__instance._heldItem.name == "crystal" && !HasCrystalManual)
        {
            APRandomizer.OWMLModConsole.WriteLine("blocking attempt to insert FC gravity crystal into a socket");
            return false;
        }
        return true;
    }
    [HarmonyPrefix, HarmonyPatch(typeof(ItemTool), nameof(ItemTool.StartUnsocketItem))]
    private static bool StartUnsocketItem(OWItemSocket socket)
    {
        if (APRandomizer.NewHorizonsAPI == null) return true;
        if (APRandomizer.NewHorizonsAPI.GetCurrentStarSystem() != "DeepBramble") return true;
        if (socket.GetSocketedItem().name == "crystal" && !HasCrystalManual)
        {
            APRandomizer.OWMLModConsole.WriteLine("blocking attempt to remove FC gravity crystal from its socket");
            return false;
        }
        return true;
    }
    [HarmonyPrefix, HarmonyPatch(typeof(ItemTool), nameof(ItemTool.UpdateInteract))]
    private static bool UpdateInteract(FirstPersonManipulator firstPersonManipulator)
    {
        if (APRandomizer.NewHorizonsAPI == null) return true;
        if (APRandomizer.NewHorizonsAPI.GetCurrentStarSystem() != "DeepBramble") return true;
        if (OWInput.IsNewlyPressed(InputLibrary.interact, InputMode.All) && !HasCrystalManual)
        {
            var item = firstPersonManipulator.GetFocusedOWItem();
            if (item?.name == "crystal")
            {
                APRandomizer.OWMLModConsole.WriteLine("blocking attempt to interact with a FC gravity crystal");
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
    public static void ItemTool_UpdateState_Postfix(ItemTool __instance, ItemTool.PromptState newState)
    {
        if (APRandomizer.NewHorizonsAPI == null) return;
        if (APRandomizer.NewHorizonsAPI.GetCurrentStarSystem() != "DeepBramble") return;
        if ((newState == ItemTool.PromptState.SOCKET || newState == ItemTool.PromptState.UNSOCKET ||
            newState == ItemTool.PromptState.PICK_UP) && !HasCrystalManual)
        {
            OWItem item = null;
            if (newState == ItemTool.PromptState.SOCKET)
                item = __instance._heldItem;
            else if (newState == ItemTool.PromptState.UNSOCKET)
                item = firstPersonManipulator.GetFocusedItemSocket().GetSocketedItem();
            else if (newState == ItemTool.PromptState.PICK_UP)
                item = firstPersonManipulator.GetFocusedOWItem();

            if (item.name == "crystal")
            {
                __instance._interactButtonPrompt.SetVisibility(false);

                __instance._messageOnlyPrompt.SetText("Requires Crystal Repair Manual");
                __instance._messageOnlyPrompt.SetVisibility(true);
                return;
            }
        }
    }
}
