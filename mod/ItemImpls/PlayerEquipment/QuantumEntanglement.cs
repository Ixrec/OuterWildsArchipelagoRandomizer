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

    [HarmonyPostfix, HarmonyPatch(typeof(QuantumObject), nameof(QuantumObject.IsPlayerEntangled))]
    public static void QuantumObject_IsPlayerEntangled_Postfix(ref bool __result)
    {
        if (!hasEntanglementKnowledge)
            __result = false;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(QuantumMoon), nameof(QuantumMoon.IsPlayerEntangled))]
    public static void QuantumMoon_IsPlayerEntangled_Postfix(ref bool __result)
    {
        if (!hasEntanglementKnowledge)
            __result = false;
    }
    [HarmonyPostfix, HarmonyPatch(typeof(QuantumMoon), nameof(QuantumMoon.GetRandomStateIndex))]
    public static void QuantumMoon_GetRandomStateIndex_Postfix(QuantumMoon __instance, ref int __result)
    {
        //APRandomizer.OWMLModConsole.WriteLine($"QuantumMoon_GetRandomStateIndex_Postfix {__result} / {hasEntanglementKnowledge} / {__instance.IsPlayerInside()}");
        if (!hasEntanglementKnowledge && __instance.IsPlayerInside() && __result == 5)
        {
            // When the player is on the QM, without Entanglement Rule, and uses the Quantum Shrine to put themselves in darkness,
            // usually the QM "leaves the player behind" as intended. But if the QM chooses to go to the 6th location,
            // for some reason the player gets warped there and then trapped in the vortex above the south pole.
            // The vortex infinite loops because of an early return condition in ChangeQuantumState() confused by my IsPlayerEntangled() patch.
            // But I've been unable to find any trace of what is warping the player into the vortex in the first place.
            // So in lieu of a direct fix, we're going to forbid the QM from choosing the 6th location in this state.

            __result = UnityEngine.Random.Range(0, 4); // pick anything except 5 / the 6th location
            APRandomizer.OWMLModConsole.WriteLine($"QuantumMoon_GetRandomStateIndex_Postfix worked around the infinite vortex bug by moving the Quantum Moon to state {__result} instead of 5");
        }
    }

    static bool collidingWithQuantumObject = false;

    [HarmonyPostfix, HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.CastForGrounded))]
    public static void PlayerCharacterController_CastForGrounded_Postfix(PlayerCharacterController __instance)
    {
        collidingWithQuantumObject = (
            __instance._collidingQuantumObject != null &&
            // for some reason the spaceship has a (disabled) SocketedQuantumObject component,
            // so we have to manually exclude that case here
            !__instance._collidingQuantumObject.CompareTag("Ship")
        );
    }

    static ScreenPrompt suitLightsDisabledPrompt = new("Suit Lights: Disabled", 0);

    [HarmonyPostfix, HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.LateInitialize))]
    public static void ToolModeUI_LateInitialize_Postfix()
    {
        Locator.GetPromptManager().AddScreenPrompt(suitLightsDisabledPrompt, PromptPosition.UpperRight, false);
    }
    [HarmonyPostfix, HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.Update))]
    public static void ToolModeUI_Update_Postfix()
    {
        suitLightsDisabledPrompt.SetVisibility(
            hasEntanglementKnowledge && OWInput.IsInputMode(InputMode.Character) && collidingWithQuantumObject
        );
    }
}
