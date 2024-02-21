using HarmonyLib;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class WarpCoreManual
{
    private static bool _hasWarpCoreManual = false;

    public static bool hasWarpCoreManual
    {
        get => _hasWarpCoreManual;
        set
        {
            if (_hasWarpCoreManual != value)
                _hasWarpCoreManual = value;
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(ItemTool), nameof(ItemTool.SocketItem))]
    private static bool SocketItem(ItemTool __instance, OWItemSocket socket)
    {
        if (__instance._heldItem is WarpCoreItem && !hasWarpCoreManual)
        {
            var type = (__instance._heldItem as WarpCoreItem).GetWarpCoreType();
            if (type == WarpCoreType.Vessel || type == WarpCoreType.VesselBroken)
            {
                APRandomizer.OWMLModConsole.WriteLine($"blocking attempt to insert Vessel/ATP warp core into a socket");
                return false;
            }
        }
        return true;
    }
    [HarmonyPrefix, HarmonyPatch(typeof(ItemTool), nameof(ItemTool.StartUnsocketItem))]
    private static bool StartUnsocketItem(ItemTool __instance, OWItemSocket socket)
    {
        if (socket.GetSocketedItem() is WarpCoreItem && !hasWarpCoreManual)
        {
            var type = (socket.GetSocketedItem() as WarpCoreItem).GetWarpCoreType();
            if (type == WarpCoreType.Vessel || type == WarpCoreType.VesselBroken)
            {
                APRandomizer.OWMLModConsole.WriteLine($"blocking attempt to remove Vessel/ATP warp core from its socket");
                return false;
            }
        }
        return true;
    }

    // ItemTool.UpdateState is the ideal hook for messing with the Insert/Remove Warp Core prompt except that in the Remove case,
    // it has no access to the socketed item we're about to Remove, even through privates. UpdateState()'s caller uses
    // FirstPersonManipulator to get at that item, but doesn't pass it to UpdateState(), so we capture a reference to it here.

    private static FirstPersonManipulator firstPersonManipulator = null;

    [HarmonyPostfix, HarmonyPatch(typeof(ToolModeSwapper), nameof(ToolModeSwapper.Start))]
    public static void ToolModeSwapper_Start_Postfix(ToolModeSwapper __instance)
    {
        firstPersonManipulator = __instance._firstPersonManipulator;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(ItemTool), nameof(ItemTool.UpdateState))]
    public static void ItemTool_UpdateState_Postfix(ItemTool __instance, ItemTool.PromptState newState, string itemName)
    {
        // We only need to edit the prompt if ItemTool was going to say "Insert/Remove Warp Core" and we don't have the codes yet.
        // ItemTool (re)sets all three of its prompts every Update, so we don't need to worry about any other state changes here.
        if ((newState == ItemTool.PromptState.SOCKET || newState == ItemTool.PromptState.UNSOCKET) && !hasWarpCoreManual)
        {
            OWItem item = (newState == ItemTool.PromptState.SOCKET) ?
                __instance._heldItem :
                firstPersonManipulator.GetFocusedItemSocket().GetSocketedItem();

            if (item is WarpCoreItem)
            {
                WarpCoreType type = (item as WarpCoreItem).GetWarpCoreType();
                if (type == WarpCoreType.Vessel || type == WarpCoreType.VesselBroken)
                {
                    __instance._interactButtonPrompt.SetVisibility(false);

                    __instance._messageOnlyPrompt.SetText("Requires Warp Core Installation Manual");
                    __instance._messageOnlyPrompt.SetVisibility(true);
                    return;
                }
            }
        }
    }
}
