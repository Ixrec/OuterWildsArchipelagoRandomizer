using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class StrangerDoorCodes
{
    private static bool _hasBreachOverrideCodes = false;
    public static bool hasBreachOverrideCodes
    {
        get => _hasBreachOverrideCodes;
        set
        {
            if (_hasBreachOverrideCodes != value)
            {
                _hasBreachOverrideCodes = value;
                // TODO: UpdateLabDoorsIRState();
            }
        }
    }

    private static bool _hasRLPaintingCode = false;
    public static bool hasRLPaintingCode
    {
        get => _hasRLPaintingCode;
        set
        {
            if (_hasRLPaintingCode != value)
            {
                _hasRLPaintingCode = value;
                UpdateRLPaintingIRState();
            }
        }
    }

    private static bool _hasCIPaintingCode = false;
    public static bool hasCIPaintingCode
    {
        get => _hasCIPaintingCode;
        set
        {
            if (_hasCIPaintingCode != value)
            {
                _hasCIPaintingCode = value;
                UpdateCIPaintingIRState();
            }
        }
    }

    private static bool _hasHGPaintingCode = false;
    public static bool hasHGPaintingCode
    {
        get => _hasHGPaintingCode;
        set
        {
            if (_hasHGPaintingCode != value)
            {
                _hasHGPaintingCode = value;
                UpdateHGPaintingIRState();
            }
        }
    }

    // Prevent the painting doors from reacting to lighting changes
    [HarmonyPrefix, HarmonyPatch(typeof(LightDarkDoorController), nameof(LightDarkDoorController.OnDetectDarkness))]
    public static bool LightDarkDoorController_OnDetectDarkness(LightDarkDoorController __instance) => false;
    [HarmonyPrefix, HarmonyPatch(typeof(LightDarkDoorController), nameof(LightDarkDoorController.OnDetectLight))]
    public static bool LightDarkDoorController_OnDetectLight(LightDarkDoorController __instance) => false;

    private static SlidingDoor rlPaintingDoor = null;
    private static SlidingDoor ciPaintingDoor = null;
    private static SlidingDoor hgPaintingDoor = null;

    private static InteractReceiver rlPaintingIR = null;
    private static InteractReceiver ciPaintingIR = null;
    private static InteractReceiver hgPaintingIR = null;

    [HarmonyPostfix, HarmonyPatch(typeof(RingWorldController), nameof(RingWorldController.Start))]
    public static void RingWorldController_Start()
    {
        rlPaintingDoor = GameObject.Find("RingWorld_Body/Sector_RingInterior/Sector_Zone1/Sector_DreamFireHouse_Zone1/Interactables_DreamFireHouse_Zone1/VisibleFromFar_Interactables_DreamFireHouse_Zone1/SecretPassage_1/")
            .GetComponent<SlidingDoor>();

        GameObject rlPaintingIRAnchor = new GameObject("APRandomizer_RL_PaintingDoor_InteractReceiver");
        rlPaintingIRAnchor.transform.SetParent(rlPaintingDoor.transform, false);
        var rlBox = rlPaintingIRAnchor.AddComponent<BoxCollider>();
        rlBox.isTrigger = true;
        rlBox.size = new Vector3(7, 10, 6);
        rlPaintingIR = rlPaintingIRAnchor.AddComponent<InteractReceiver>();

        UpdateRLPaintingIRState();

        rlPaintingIR.OnPressInteract += () =>
        {
            if (!hasRLPaintingCode) return;
            if (rlPaintingDoor == null) return;
            if (rlPaintingDoor.IsOpen()) return;

            APRandomizer.OWMLModConsole.WriteLine($"APRandomizer_RL_PaintingDoor_InteractReceiver OnPressInteract opening RL painting door");
            rlPaintingDoor.Open();
            rlPaintingIR.DisableInteraction();
        };

        ciPaintingDoor = GameObject.Find("RingWorld_Body/Sector_RingInterior/Sector_Zone2/Sector_DreamFireLighthouse_Zone2_AnimRoot/Interactibles_DreamFireLighthouse_Zone2/VisibleFromFar_Interactables_DreamFireLighthouse_Zone2/SecretPassage_2/")
            .GetComponent<SlidingDoor>();

        GameObject ciPaintingIRAnchor = new GameObject("APRandomizer_CI_PaintingDoor_InteractReceiver");
        ciPaintingIRAnchor.transform.SetParent(ciPaintingDoor.transform, false);
        var ciBox = ciPaintingIRAnchor.AddComponent<BoxCollider>();
        ciBox.isTrigger = true;
        ciBox.size = new Vector3(7, 10, 6);
        ciPaintingIR = ciPaintingIRAnchor.AddComponent<InteractReceiver>();

        UpdateCIPaintingIRState();

        ciPaintingIR.OnPressInteract += () =>
        {
            if (!hasCIPaintingCode) return;
            if (ciPaintingDoor == null) return;
            if (ciPaintingDoor.IsOpen()) return;

            APRandomizer.OWMLModConsole.WriteLine($"APRandomizer_CI_PaintingDoor_InteractReceiver OnPressInteract opening CI painting door");
            ciPaintingDoor.Open();
            ciPaintingIR.DisableInteraction();
        };

        hgPaintingDoor = GameObject.Find("RingWorld_Body/Sector_RingInterior/Sector_Zone3/Sector_HiddenGorge/Sector_DreamFireHouse_Zone3/Interactables_DreamFireHouse_Zone3/VisibleFromFar_Interactables_DreamFireHouse_Zone3/SecretPassage_DFH_Zone3")
            .GetComponent<SlidingDoor>();

        GameObject hgPaintingIRAnchor = new GameObject("APRandomizer_HG_PaintingDoor_InteractReceiver");
        hgPaintingIRAnchor.transform.SetParent(hgPaintingDoor.transform, false);
        var hgBox = hgPaintingIRAnchor.AddComponent<BoxCollider>();
        hgBox.isTrigger = true;
        hgBox.size = new Vector3(7, 10, 6);
        hgPaintingIR = hgPaintingIRAnchor.AddComponent<InteractReceiver>();

        UpdateHGPaintingIRState();

        hgPaintingIR.OnPressInteract += () =>
        {
            if (!hasHGPaintingCode) return;
            if (hgPaintingDoor == null) return;
            if (hgPaintingDoor.IsOpen()) return;

            APRandomizer.OWMLModConsole.WriteLine($"APRandomizer_HG_PaintingDoor_InteractReceiver OnPressInteract opening HG painting door");
            hgPaintingDoor.Open();
            hgPaintingIR.DisableInteraction();
        };
    }

    private static void UpdateRLPaintingIRState()
    {
        if (rlPaintingIR == null || rlPaintingDoor == null) return;
        if (rlPaintingDoor.IsOpen()) return;

        if (hasRLPaintingCode)
        {
            rlPaintingIR.ChangePrompt("Open Painting");
            rlPaintingIR.SetKeyCommandVisible(true);
        }
        else
        {
            rlPaintingIR.ChangePrompt("Requires River Lowlands Painting Code");
            rlPaintingIR.SetKeyCommandVisible(false);
        }
    }

    private static void UpdateCIPaintingIRState()
    {
        if (ciPaintingIR == null || ciPaintingDoor == null) return;
        if (ciPaintingDoor.IsOpen()) return;

        if (hasCIPaintingCode)
        {
            ciPaintingIR.ChangePrompt("Open Painting");
            ciPaintingIR.SetKeyCommandVisible(true);
        }
        else
        {
            ciPaintingIR.ChangePrompt("Requires Cinder Isles Painting Code");
            ciPaintingIR.SetKeyCommandVisible(false);
        }
    }

    private static void UpdateHGPaintingIRState()
    {
        if (hgPaintingIR == null || hgPaintingDoor == null) return;
        if (hgPaintingDoor.IsOpen()) return;

        if (hasHGPaintingCode)
        {
            hgPaintingIR.ChangePrompt("Open Painting");
            hgPaintingIR.SetKeyCommandVisible(true);
        }
        else
        {
            hgPaintingIR.ChangePrompt("Requires Hidden Gorge Painting Code");
            hgPaintingIR.SetKeyCommandVisible(false);
        }
    }
}
