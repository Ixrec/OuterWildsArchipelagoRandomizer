using HarmonyLib;
using UnityEngine;

namespace ArchipelagoRandomizer
{
    [HarmonyPatch]
    internal class NomaiTextQoL
    {
        [HarmonyPostfix, HarmonyPatch(typeof(NomaiWallText), nameof(NomaiWallText.LateInitialize))]
        public static void NomaiWallText_LateInitialize_Postfix(NomaiWallText __instance)
        {
            APRandomizer.OWMLModConsole.WriteLine($"Expanding all text in {__instance.gameObject.name}", OWML.Common.MessageType.Info);
            foreach (NomaiTextLine child in __instance.GetComponentsInChildren<NomaiTextLine>())
            {
                child._state = NomaiTextLine.VisualState.UNREAD;
            }

            // Ignore scrolls if they aren't socketed
            bool isScroll = __instance.transform.GetComponentInParent<ScrollItem>() != null;
            bool isSocketed = __instance.transform.GetComponentInParent<ScrollSocket>() != null;

            if (!isScroll || isSocketed)
            {
                __instance.ShowImmediate();
            }
        }
    }
}
