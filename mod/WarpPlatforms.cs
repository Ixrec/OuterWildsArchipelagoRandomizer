using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class WarpPlatforms
{
    private static bool _hasNomaiWarpCodes = false;

    public static bool hasNomaiWarpCodes
    {
        get => _hasNomaiWarpCodes;
        set
        {
            if (_hasNomaiWarpCodes != value)
            {
                _hasNomaiWarpCodes = value;
                foreach (var ir in interactReceivers)
                    ApplyHasCodesFlagToIR(_hasNomaiWarpCodes, ir);
            }
        }
    }

    // we don't care about the Vessel frequency used at the end of the game
    readonly static NomaiWarpPlatform.Frequency[] frequenciesOfInterest = {
        NomaiWarpPlatform.Frequency.SunStation,
        NomaiWarpPlatform.Frequency.TimeLoop, // Ash Twin to inside Ash Twin Project
        NomaiWarpPlatform.Frequency.HourglassTwin, // Ash Twin to Ember Twin
        NomaiWarpPlatform.Frequency.TimberHearth,
        NomaiWarpPlatform.Frequency.BrittleHollowPolar, // Ash Twin to Brittle Hollow surface near north pole
        NomaiWarpPlatform.Frequency.BrittleHollowForge, // White Hole Station to Brittle Hollow underside of crust, just outside Black Hole Forge
        NomaiWarpPlatform.Frequency.GiantsDeep,
    };

    static Dictionary<NomaiWarpPlatform.Frequency, NomaiWarpPlatform> warpTransmitters = new();

    public static void Setup()
    {
        LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
        {
            if (loadScene != OWScene.SolarSystem) return;

            var at = Locator.GetAstroObject(AstroObject.Name.TowerTwin); // Ash Twin
            var whs = GameObject.Find("WhiteholeStation_Body");

            warpTransmitters.Clear();
            warpTransmitters.Add(
                NomaiWarpPlatform.Frequency.GiantsDeep,
                at.transform.Find("Sector_TowerTwin/Sector_Tower_GD").GetComponentInChildren<NomaiWarpTransmitter>()
            );
            warpTransmitters.Add(
                NomaiWarpPlatform.Frequency.BrittleHollowForge,
                at.transform.Find("Sector_TowerTwin/Sector_Tower_BH").GetComponentInChildren<NomaiWarpTransmitter>()
            );
            warpTransmitters.Add(
                NomaiWarpPlatform.Frequency.BrittleHollowPolar,
                whs.transform.GetComponentInChildren<NomaiWarpTransmitter>()
            );
            warpTransmitters.Add(
                NomaiWarpPlatform.Frequency.TimberHearth,
                at.transform.Find("Sector_TowerTwin/Sector_Tower_TH").GetComponentInChildren<NomaiWarpTransmitter>()
            );
            warpTransmitters.Add(
                NomaiWarpPlatform.Frequency.HourglassTwin,
                at.transform.Find("Sector_TowerTwin/Sector_Tower_HGT/Interactables_Tower_HGT/Interactables_Tower_CT").GetComponentInChildren<NomaiWarpTransmitter>()
            );
            warpTransmitters.Add(
                NomaiWarpPlatform.Frequency.TimeLoop,
                at.transform.Find("Sector_TowerTwin/Sector_Tower_HGT/Interactables_Tower_HGT/Interactables_Tower_TT").GetComponentInChildren<NomaiWarpTransmitter>()
            );
            warpTransmitters.Add(
                NomaiWarpPlatform.Frequency.SunStation,
                at.transform.Find("Sector_TowerTwin/Sector_Tower_SS").GetComponentInChildren<NomaiWarpTransmitter>()
            );
        };
    }

    static List<InteractReceiver> interactReceivers = new();

    private static void ApplyHasCodesFlagToIR(bool hasNomaiWarpCodes, InteractReceiver ir)
    {
        if (hasNomaiWarpCodes)
        {
            ir.ChangePrompt("Activate Nomai Warp Platform");
            ir.SetKeyCommandVisible(true);
        }
        else
        {
            ir.ChangePrompt("Requires Nomai Warp Codes");
            ir.SetKeyCommandVisible(false);
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(NomaiWarpPlatform), nameof(NomaiWarpPlatform.Start))]
    public static void NomaiWarpPlatform_Start_Prefix(NomaiWarpPlatform __instance)
    {
        if (!frequenciesOfInterest.Contains(__instance.GetFrequency())) return;

        GameObject warpPlatformInteract = new GameObject("APRandomizer_WarpPlatformInteract");
        warpPlatformInteract.transform.SetParent(__instance.transform, false);
        
        var box = warpPlatformInteract.AddComponent<BoxCollider>();
        // We just want to detect the player, not make an invisible wall
        box.isTrigger = true;
        // Change the size to something easy to interact with
        box.size = new Vector3(6, 3, 6);

        // We'll also add a capsule collider to make this composite, making it easier to interact with
        var capsule = warpPlatformInteract.AddComponent<CapsuleCollider>();
        capsule.isTrigger = true;
        capsule.radius = 3;
        capsule.height = 7;

        var ir = warpPlatformInteract.AddComponent<InteractReceiver>();
        ApplyHasCodesFlagToIR(hasNomaiWarpCodes, ir);
        interactReceivers.Add(ir);

        ir.OnPressInteract += () =>
        {
            if (!hasNomaiWarpCodes) return;

            var isTransmitter = (__instance is NomaiWarpTransmitter);
            var destination = isTransmitter ?
                Locator.GetWarpReceiver(__instance.GetFrequency()) :
                warpTransmitters[__instance.GetFrequency()];

            APRandomizer.OWMLModConsole.WriteLine($"APRandomizer_WarpPlatformInteract OnPressInteract opening black hole {isTransmitter} / {__instance?.name} {destination?.name} / {__instance?.transform?.parent?.name} {destination?.transform?.parent?.name}");
            __instance.OpenBlackHole(destination, false);
        };
    }

    // In the vanilla game, NomaiWarpTransmitter.FixedUpdate is the method that decides to
    // open a black hole and a white hole when a pair of warp platforms line up.

    // Unfortunately that's a derived class' override of a virtual method, which explicitly
    // invokes the base class method NomaiWarpPlatform.FixedUpdate.
    // That base class method is essential to making the black hole actually warp you to the white hole.
    // So we can't just skip the method completely.

    // Instead we take advantage of the fact that NomaiWarpPlatform.FixedUpdate is essentially:
    //      this.UpdateEnabled();
    //      if (this.IsBlackHoleOpen()) {
    //          ...
    //      }
    // So if the black hole is already open, we let both methods be called normally,
    // but if it's not open, we emulate what the base method would've done on its own.
    [HarmonyPrefix, HarmonyPatch(typeof(NomaiWarpTransmitter), nameof(NomaiWarpTransmitter.FixedUpdate))]
    public static bool NomaiWarpTransmitter_FixedUpdate_Prefix(NomaiWarpTransmitter __instance)
    {
        if (__instance.IsBlackHoleOpen())
        {
            return true;
        }
        else
        {
            __instance.UpdateEnabled();
            return false;
        }
    }
}
