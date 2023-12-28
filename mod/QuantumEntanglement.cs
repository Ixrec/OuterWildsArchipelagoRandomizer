using HarmonyLib;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class QuantumEntanglement
{
    private static bool _hasEntanglementKnowledge = false;

    public static bool hasEntanglementKnowledge
    {
        get => _hasEntanglementKnowledge;
        set
        {
            if (_hasEntanglementKnowledge != value)
            {
                _hasEntanglementKnowledge = value;

                if (_hasEntanglementKnowledge)
                {
                    var nd = new NotificationData(NotificationTarget.Player, "RECONFIGURING SPACESUIT TO DISABLE ALL LIGHTS ON QUANTUM OBJECTS", 10);
                    NotificationManager.SharedInstance.PostNotification(nd, false);
                }
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(QuantumObject), nameof(QuantumObject.IsPlayerEntangled))]
    public static void QuantumObject_IsPlayerEntangled_Postfix(ref bool __result)
    {
        if (!hasEntanglementKnowledge)
            __result = false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(QuantumMoon), nameof(QuantumMoon.IsPlayerEntangled))]
    public static void QuantumMoon_IsPlayerEntangled_Postfix(ref bool __result)
    {
        if (!hasEntanglementKnowledge)
            __result = false;
    }

    static bool collidingWithQuantumObject = false;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.CastForGrounded))]
    public static void PlayerCharacterController_CastForGrounded_Postfix(PlayerCharacterController __instance)
    {
        collidingWithQuantumObject = (
            __instance._collidingQuantumObject is not null &&
            // for some reason the spaceship has a (disabled) SocketedQuantumObject component,
            // so we have to manually exclude that case here
            !__instance._collidingQuantumObject.CompareTag("Ship")
        );
    }

    static ScreenPrompt suitLightsDisabledPrompt = new("Suit Lights: Disabled", 0);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.LateInitialize))]
    public static void ToolModeUI_LateInitialize_Postfix()
    {
        Locator.GetPromptManager().AddScreenPrompt(suitLightsDisabledPrompt, PromptPosition.UpperRight, false);
    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.Update))]
    public static void ToolModeUI_Update_Postfix()
    {
        suitLightsDisabledPrompt.SetVisibility(
            hasEntanglementKnowledge && OWInput.IsInputMode(InputMode.Character) && collidingWithQuantumObject
        );
    }
}
