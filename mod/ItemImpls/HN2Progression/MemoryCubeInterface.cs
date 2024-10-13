using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class MemoryCubeInterface
{
    private static bool _hasMemoryCubeInterface = false;

    public static bool hasMemoryCubeInterface
    {
        get => _hasMemoryCubeInterface;
        set
        {
            if (_hasMemoryCubeInterface != value)
            {
                _hasMemoryCubeInterface = value;
                foreach (var ir in MemoryCubeIRs)
                    ApplyMCIFlagToIR(_hasMemoryCubeInterface, ir);
            }
        }
    }

    private static List<InteractReceiver> MemoryCubeIRs = null;
    private static List<GameObject> MemoryCubeInteractableGOs = null;

    [HarmonyPostfix, HarmonyPatch(typeof(PlayerSectorDetector), nameof(PlayerSectorDetector.OnAddSector))]
    public static void PlayerSectorDetector_OnAddSector(PlayerSectorDetector __instance) {
        // we only need to do this once
        if (MemoryCubeIRs != null) return;

        // and only if we're in the NH2 system
        if (APRandomizer.NewHorizonsAPI == null) return;
        if (APRandomizer.NewHorizonsAPI.GetCurrentStarSystem() != "Jam3") return;

        var memoryCubeIRs = GameObject.Find("MAGISTARIUM_Body")
            ?.GetComponentsInChildren<InteractReceiver>()
            ?.Where(ir => ir._screenPrompt._text == "<CMD> Talk to Memory Cube")
            ?.ToList();

        if (memoryCubeIRs.Count == 0)
        {
            APRandomizer.OWMLModConsole.WriteLine($"MCI::OnAddSector didn't find any memory cubes yet, waiting for next sector change");
            return;
        }

        // wait to set the static variable to non-null until we're sure we found the cubes
        // fortunately the HN2 sectoring doesn't appear to split the cubes into groups, they're just all slow to initialize their text prompts
        MemoryCubeIRs = memoryCubeIRs;
        MemoryCubeInteractableGOs = MemoryCubeIRs.Select(ir => ir.gameObject).ToList();

        APRandomizer.OWMLModConsole.WriteLine($"MCI::OnAddSector updating {MemoryCubeIRs.Count} memory cube IRs");
        foreach (var ir in MemoryCubeIRs)
            ApplyMCIFlagToIR(_hasMemoryCubeInterface, ir);
    }

    [HarmonyPrefix, HarmonyPatch(typeof(CharacterDialogueTree), nameof(CharacterDialogueTree.OnPressInteract))]
    public static bool CharacterDialogueTree_OnPressInteract(CharacterDialogueTree __instance)
    {
        // make sure this patch can't break any CDTs except the HN2 memory cubes
        if (MemoryCubeInteractableGOs == null) return true;
        if (!MemoryCubeInteractableGOs.Contains(__instance.gameObject)) return true;

        if (!hasMemoryCubeInterface)
            APRandomizer.OWMLModConsole.WriteLine($"CharacterDialogueTree_OnPressInteract preventing interaction with CDT of {__instance?.transform?.parent?.name}/{__instance?.name}");
        return hasMemoryCubeInterface;
    }

    private static void ApplyMCIFlagToIR(bool hasMCI, InteractReceiver ir)
    {
        if (hasMCI)
        {
            ir.ChangePrompt("Talk to Memory Cube");
            ir.SetKeyCommandVisible(true);
        }
        else
        {
            ir.ChangePrompt("Requires Memory Cube Interface");
            ir.SetKeyCommandVisible(false);
        }
    }
}
