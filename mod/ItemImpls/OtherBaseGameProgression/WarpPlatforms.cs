using HarmonyLib;
using Newtonsoft.Json.Linq;
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

    private enum WarpPlatform
    {
        SunStation,
        SunTower,
        EmberTwin,
        EmberTwinTower,
        AshTwinProject,
        AshTwinTower,
        TimberHearth,
        TimberHearthTower,
        BrittleHollowNorthernGlacier,
        WhiteHoleStation,
        BlackHoleForge,
        BrittleHollowTower,
        GiantsDeep,
        GiantsDeepTower,
    }

    // In the base game, each warp platform is either a "transmitter" or a "receiver" on a certain "warp frequency".
    // In the randomizer, a warp can be initiated from any platform, at any time, and may go to any other platform.
    // So when patching the platform code, we need to be able to map from e.g. "transmitter on SunStation frequency" to "the Sun Tower platform".
    static Dictionary<NomaiWarpPlatform.Frequency, WarpPlatform> warpFrequencyToTransmitter = new Dictionary<NomaiWarpPlatform.Frequency, WarpPlatform> {
        { NomaiWarpPlatform.Frequency.SunStation, WarpPlatform.SunTower },
        { NomaiWarpPlatform.Frequency.TimeLoop, WarpPlatform.AshTwinTower },
        { NomaiWarpPlatform.Frequency.HourglassTwin, WarpPlatform.EmberTwinTower },
        { NomaiWarpPlatform.Frequency.TimberHearth, WarpPlatform.TimberHearthTower },
        { NomaiWarpPlatform.Frequency.BrittleHollowPolar, WarpPlatform.WhiteHoleStation },
        { NomaiWarpPlatform.Frequency.BrittleHollowForge, WarpPlatform.BrittleHollowTower },
        { NomaiWarpPlatform.Frequency.GiantsDeep, WarpPlatform.GiantsDeepTower },
    };
    static Dictionary<NomaiWarpPlatform.Frequency, WarpPlatform> warpFrequencyToReceiver = new Dictionary<NomaiWarpPlatform.Frequency, WarpPlatform>
    {
        { NomaiWarpPlatform.Frequency.SunStation, WarpPlatform.SunStation },
        { NomaiWarpPlatform.Frequency.TimeLoop, WarpPlatform.AshTwinProject },
        { NomaiWarpPlatform.Frequency.HourglassTwin, WarpPlatform.EmberTwin },
        { NomaiWarpPlatform.Frequency.TimberHearth, WarpPlatform.TimberHearth },
        { NomaiWarpPlatform.Frequency.BrittleHollowPolar, WarpPlatform.BrittleHollowNorthernGlacier },
        { NomaiWarpPlatform.Frequency.BrittleHollowForge, WarpPlatform.BlackHoleForge },
        { NomaiWarpPlatform.Frequency.GiantsDeep, WarpPlatform.GiantsDeep },
    };

    private static Dictionary<WarpPlatform, WarpPlatform> vanillaWarps = new Dictionary<WarpPlatform, WarpPlatform> {
        { WarpPlatform.SunStation, WarpPlatform.SunTower },
        { WarpPlatform.SunTower, WarpPlatform.SunStation },
        { WarpPlatform.EmberTwin, WarpPlatform.EmberTwinTower },
        { WarpPlatform.EmberTwinTower, WarpPlatform.EmberTwin },
        { WarpPlatform.AshTwinProject, WarpPlatform.AshTwinTower },
        { WarpPlatform.AshTwinTower, WarpPlatform.AshTwinProject },
        { WarpPlatform.TimberHearth, WarpPlatform.TimberHearthTower },
        { WarpPlatform.TimberHearthTower, WarpPlatform.TimberHearth },
        { WarpPlatform.BrittleHollowNorthernGlacier, WarpPlatform.WhiteHoleStation },
        { WarpPlatform.WhiteHoleStation, WarpPlatform.BrittleHollowNorthernGlacier },
        { WarpPlatform.BlackHoleForge, WarpPlatform.BrittleHollowTower },
        { WarpPlatform.BrittleHollowTower, WarpPlatform.BlackHoleForge },
        { WarpPlatform.GiantsDeep, WarpPlatform.GiantsDeepTower },
        { WarpPlatform.GiantsDeepTower, WarpPlatform.GiantsDeep },
    };

    private static Dictionary<WarpPlatform, WarpPlatform> warps = vanillaWarps;

    private static Dictionary<string, WarpPlatform> slotDataIdToWarpPlatform = new Dictionary<string, WarpPlatform>
    {
        { "SS", WarpPlatform.SunStation },
        { "ST", WarpPlatform.SunTower },
        { "ET", WarpPlatform.EmberTwin },
        { "ETT", WarpPlatform.EmberTwinTower },
        { "ATP", WarpPlatform.AshTwinProject },
        { "ATT", WarpPlatform.AshTwinTower },
        { "TH", WarpPlatform.TimberHearth },
        { "THT", WarpPlatform.TimberHearthTower },
        { "BHNG", WarpPlatform.BrittleHollowNorthernGlacier },
        { "WHS", WarpPlatform.WhiteHoleStation },
        { "BHF", WarpPlatform.BlackHoleForge },
        { "BHT", WarpPlatform.BrittleHollowTower },
        { "GD", WarpPlatform.GiantsDeep },
        { "GDT", WarpPlatform.GiantsDeepTower },
    };

    public static void ApplySlotData(object warpSlotData)
    {
        if (warpSlotData is string warpString && warpString == "vanilla")
        {
            warps = vanillaWarps;
            return;
        }

        if (warpSlotData is not JArray warpsArray)
        {
            APRandomizer.OWMLModConsole.WriteLine($"Leaving vanilla warps unchanged because slot_data['warps'] was invalid: {warpSlotData}", OWML.Common.MessageType.Error);
            return;
        }

        var warpsFromSlotData = new Dictionary<WarpPlatform, WarpPlatform>();
        foreach (JToken warpPair in warpsArray)
        {
            if (warpPair is not JArray warpPairArray)
            {
                APRandomizer.OWMLModConsole.WriteLine($"Leaving vanilla warps unchanged because slot_data['warps'] was invalid: {warpSlotData}", OWML.Common.MessageType.Error);
                return;
            }

            var w1 = (string)warpPairArray[0];
            var w2 = (string)warpPairArray[1];

            if (!slotDataIdToWarpPlatform.TryGetValue(w1, out WarpPlatform wp1))
            {
                APRandomizer.OWMLModConsole.WriteLine($"Leaving vanilla warps unchanged because slot_data['warps'] was invalid: {warpSlotData}", OWML.Common.MessageType.Error);
                return;
            }
            if (!slotDataIdToWarpPlatform.TryGetValue(w2, out WarpPlatform wp2))
            {
                APRandomizer.OWMLModConsole.WriteLine($"Leaving vanilla warps unchanged because slot_data['warps'] was invalid: {warpSlotData}", OWML.Common.MessageType.Error);
                return;
            }

            warpsFromSlotData.Add(wp1, wp2);
            warpsFromSlotData.Add(wp2, wp1);
        }

        warps = warpsFromSlotData;
    }

    private static Dictionary<WarpPlatform, NomaiWarpPlatform> warpEnumToInGamePlatform = new();
    private static HashSet<NomaiWarpPlatform> manualWarpPlatforms = new();

    public static void OnCompleteSceneLoad(OWScene _scene, OWScene loadScene)
    {
        if (loadScene != OWScene.SolarSystem) return;

        warpEnumToInGamePlatform.Clear();

        warpEnumToInGamePlatform.Add(WarpPlatform.SunStation, Locator.GetWarpReceiver(NomaiWarpPlatform.Frequency.SunStation));
        warpEnumToInGamePlatform.Add(WarpPlatform.AshTwinProject, Locator.GetWarpReceiver(NomaiWarpPlatform.Frequency.TimeLoop));
        warpEnumToInGamePlatform.Add(WarpPlatform.EmberTwin, Locator.GetWarpReceiver(NomaiWarpPlatform.Frequency.HourglassTwin));
        warpEnumToInGamePlatform.Add(WarpPlatform.TimberHearth, Locator.GetWarpReceiver(NomaiWarpPlatform.Frequency.TimberHearth));
        warpEnumToInGamePlatform.Add(WarpPlatform.BrittleHollowNorthernGlacier, Locator.GetWarpReceiver(NomaiWarpPlatform.Frequency.BrittleHollowPolar));
        warpEnumToInGamePlatform.Add(WarpPlatform.BlackHoleForge, Locator.GetWarpReceiver(NomaiWarpPlatform.Frequency.BrittleHollowForge));
        warpEnumToInGamePlatform.Add(WarpPlatform.GiantsDeep, Locator.GetWarpReceiver(NomaiWarpPlatform.Frequency.GiantsDeep));

        var at = Locator.GetAstroObject(AstroObject.Name.TowerTwin); // Ash Twin
        var whs = GameObject.Find("WhiteholeStation_Body");

        warpEnumToInGamePlatform.Add(
            WarpPlatform.GiantsDeepTower,
            at.transform.Find("Sector_TowerTwin/Sector_Tower_GD").GetComponentInChildren<NomaiWarpTransmitter>()
        );
        warpEnumToInGamePlatform.Add(
            WarpPlatform.BrittleHollowTower,
            at.transform.Find("Sector_TowerTwin/Sector_Tower_BH").GetComponentInChildren<NomaiWarpTransmitter>()
        );
        warpEnumToInGamePlatform.Add(
            WarpPlatform.WhiteHoleStation,
            whs.transform.GetComponentInChildren<NomaiWarpTransmitter>()
        );
        warpEnumToInGamePlatform.Add(
            WarpPlatform.TimberHearthTower,
            at.transform.Find("Sector_TowerTwin/Sector_Tower_TH").GetComponentInChildren<NomaiWarpTransmitter>()
        );
        warpEnumToInGamePlatform.Add(
            WarpPlatform.EmberTwinTower,
            at.transform.Find("Sector_TowerTwin/Sector_Tower_HGT/Interactables_Tower_HGT/Interactables_Tower_CT").GetComponentInChildren<NomaiWarpTransmitter>()
        );
        warpEnumToInGamePlatform.Add(
            WarpPlatform.AshTwinTower,
            at.transform.Find("Sector_TowerTwin/Sector_Tower_HGT/Interactables_Tower_HGT/Interactables_Tower_TT").GetComponentInChildren<NomaiWarpTransmitter>()
        );
        warpEnumToInGamePlatform.Add(
            WarpPlatform.SunTower,
            at.transform.Find("Sector_TowerTwin/Sector_Tower_SS").GetComponentInChildren<NomaiWarpTransmitter>()
        );

        manualWarpPlatforms = new HashSet<NomaiWarpPlatform>(warpEnumToInGamePlatform.Values);
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

    // we don't care about the Vessel frequency used at the end of the game
    readonly static NomaiWarpPlatform.Frequency[] frequenciesOfInterest = {
        NomaiWarpPlatform.Frequency.SunStation,
        NomaiWarpPlatform.Frequency.TimeLoop, // Ash Twin to inside Ash Twin Project
        NomaiWarpPlatform.Frequency.HourglassTwin, // Ash Twin to Ember Twin
        NomaiWarpPlatform.Frequency.TimberHearth,
        NomaiWarpPlatform.Frequency.BrittleHollowPolar, // White Hole Station to Brittle Hollow surface near north pole
        NomaiWarpPlatform.Frequency.BrittleHollowForge, // Ash Twin to Brittle Hollow underside of crust, just outside Black Hole Forge
        NomaiWarpPlatform.Frequency.GiantsDeep,
    };

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
            var frequency = __instance.GetFrequency();
            WarpPlatform source = isTransmitter ? warpFrequencyToTransmitter[frequency] : warpFrequencyToReceiver[frequency];

            WarpPlatform destination = warps[source];
            NomaiWarpPlatform destinationPlatform = warpEnumToInGamePlatform[destination];

            APRandomizer.OWMLModConsole.WriteLine($"APRandomizer_WarpPlatformInteract OnPressInteract opening black hole from {source} to {destination}");
            __instance.OpenBlackHole(destinationPlatform, false);
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
        // Also, we only want to disable auto-warp on alignment for the vanilla warp platforms this AP item is designed for.
        // To avoid breaking story mods (Astral Codec) that rely on warp platforms' normal behavior, we return early on any other platform.
        var isManualPlatform = manualWarpPlatforms.Contains(__instance);
        if (!isManualPlatform)
            return true;

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

    [HarmonyPrefix, HarmonyPatch(typeof(NomaiWarpPlatform), nameof(NomaiWarpPlatform.OnExit))]
    public static bool NomaiWarpPlatform_OnExit(NomaiWarpPlatform __instance, GameObject hitObj)
    {
        if (hitObj.GetComponentInParent<OWRigidbody>() == null)
        {
            APRandomizer.OWMLModConsole.WriteLine($"skipping NomaiWarpPlatform::OnExit() call to prevent a base game NRE when dying on a warp platform");
            return false;
        }

        return true;
    }
}
