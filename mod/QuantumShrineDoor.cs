using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class QuantumShrineDoor
{
    public static bool hasQuantumShrineCodes = false;

    private static InteractReceiver doorIR = null;
    private static NomaiGateway gateway = null;

    public static void Setup()
    {
        LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
        {
            if (loadScene != OWScene.SolarSystem) return;

            Randomizer.Instance.ModHelper.Console.WriteLine($"QuantumShrineDoor.Setup deleting door orb");

            var shrineTransform = Locator.GetQuantumMoon().transform.Find("Sector_QuantumMoon/QuantumShrine");
            var orbs = shrineTransform.GetComponent<QuantumShrine>()._childOrbs;

            orbs[0].gameObject.SetActive(false); // door switch
            // orbs[1] is the light switch inside, leave that active
        };
    }

    public static void SetHasQuantumShrineCodes(bool hasQuantumShrineCodes)
    {
        if (QuantumShrineDoor.hasQuantumShrineCodes != hasQuantumShrineCodes)
        {
            QuantumShrineDoor.hasQuantumShrineCodes = hasQuantumShrineCodes;            
            UpdateIRState();
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(QuantumShrine), nameof(QuantumShrine.Start))]
    public static void QuantumShrine_Start_Prefix(QuantumShrine __instance)
    {
        Randomizer.Instance.ModHelper.Console.WriteLine($"QuantumShrine.Start adding prompt to shrine door");

        var shrineGatewayTransform = __instance.transform.Find("Prefab_NOM_Gateway");

        gateway = shrineGatewayTransform.gameObject.GetComponent<NomaiGateway>();

        GameObject shrineDoorInteract = new GameObject("APRandomizer_ShrineDoorInteract");
        shrineDoorInteract.transform.SetParent(shrineGatewayTransform, false);

        var box = shrineDoorInteract.AddComponent<BoxCollider>();
        box.isTrigger = true; // We just want to detect the player, not make an invisible wall
        box.size = new Vector3(10, 10, 5);

        doorIR = shrineDoorInteract.AddComponent<InteractReceiver>();
        UpdateIRState();
        doorIR.OnPressInteract += () =>
        {
            if (!hasQuantumShrineCodes) return;

            Randomizer.Instance.ModHelper.Console.WriteLine($"APRandomizer_ShrineDoorInteract OnPressInteract opening gateway");
            if (gateway is null)
            {
                Randomizer.Instance.ModHelper.Console.WriteLine($"APRandomizer_ShrineDoorInteract OnPressInteract failed to locate NomaiGateway component on shrineGatewayTransform", OWML.Common.MessageType.Error);
                return;
            }

            // OpenGate()'s implementation never uses its slot argument, but _openSlot/_closeSlot are what it would normally be set to
            if (gateway._open)
                gateway.CloseGate(gateway._closeSlot);
            else
                gateway.OpenGate(gateway._openSlot);

            UpdateIRState();
        };
    }

    private static void UpdateIRState()
    {
        if (doorIR is null || gateway is null) return;

        if (hasQuantumShrineCodes)
        {
            doorIR.ChangePrompt(gateway._open ? "Close Quantum Shrine" : "Open Quantum Shrine");
            doorIR.SetKeyCommandVisible(true);
        }
        else
        {
            doorIR.ChangePrompt("Requires Quantum Shrine Codes");
            doorIR.SetKeyCommandVisible(false);
        }
    }
}
