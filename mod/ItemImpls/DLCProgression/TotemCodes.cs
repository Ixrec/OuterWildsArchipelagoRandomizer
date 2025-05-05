using HarmonyLib;
using OWML.Common;
using UnityEngine;
using System.Security.Cryptography;
using System.Text;
using System;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class TotemCodes
{
    private static bool RandomizeCodes = false;
    private static Sprite templeCodeSprite = null;
    private static int[] vaultCode1 = [5, 0, 2, 6, 5];
    private static int[] vaultCode2 = [5, 4, 3, 7, 2];
    private static int[] vaultCode3 = [1, 2, 3, 2, 1];
    private static int[] jammerCode = [4, 6, 7, 6, 7];
    private static int[] templeCode = [0, 3, 0, 5, 5];

    private static int[] nullCode = [0, 0, 0, 0, 0];

    internal static AssetBundle CodeAssets; //[!] Ideally, this would be a part of the main assetbundle. I don't have the ability to edit that, so this temporary assetbundle exists instead

    public static void OnCompleteSceneLoad(OWScene scene, OWScene loadScene)
    {
        if (loadScene != OWScene.SolarSystem) return;

        if (RandomizeCodes)
        {
            MD5 hasher = MD5.Create(); // The room seed is a string, so we hash it to get our seed
            System.Random codeRng = new System.Random(BitConverter.ToInt32(hasher.ComputeHash(Encoding.UTF8.GetBytes(APRandomizer.APSession.RoomState.Seed)), 0));

            for (int i = 0; i < 5; i++)
            {
                vaultCode1[i] = codeRng.Next(8);
                vaultCode2[i] = codeRng.Next(8);
                vaultCode3[i] = codeRng.Next(8);
                jammerCode[i] = codeRng.Next(8);
                templeCode[i] = codeRng.Next(8);
            }

            // If any of the codes would be all 0 (the default input when a player finds a code totem), reset the code to the default
            jammerCode = jammerCode == nullCode ? [4, 6, 7, 6, 7] : jammerCode;
            templeCode = templeCode == nullCode ? [0, 3, 0, 5, 5] : templeCode;
            vaultCode1 = vaultCode1 == nullCode ? [5, 0, 2, 6, 5] : vaultCode1;
            vaultCode2 = vaultCode2 == nullCode ? [5, 4, 3, 7, 2] : vaultCode2;
            vaultCode3 = vaultCode3 == nullCode ? [1, 2, 3, 2, 1] : vaultCode3;
        }
    }

    public static void ModSettingsChanged(IModConfig config)
    {
        RandomizeCodes = config.GetSettingsValue<bool>("Randomize Stranger Codes");
    }

    [HarmonyPrefix, HarmonyPatch(typeof(RingWorldController), nameof(RingWorldController.OnEnterInsideVolume))]
    public static void RingWorldController_OnEnterInsideVolume(RingWorldController __instance)
    {
        // If we edit the visible codes too early, they get stuck on a low resolution texture
        if (RandomizeCodes)
        {
            APRandomizer.OWMLModConsole.WriteLine($"RingWorldController_OnEnterInsideVolume altering totem codes");
            // Change the code paper for the temple code in the code room
            Transform templeCodeDisplay = GameObject.Find("RingWorld_Body/Sector_RingInterior/Sector_Zone2/Sector_DreamFireLighthouse_Zone2_AnimRoot/Interactibles_DreamFireLighthouse_Zone2/Pivot_CodeRoom").transform.GetChild(2).GetChild(1); // GetChild() is used because multiple objects here have the same name
            // All the symbols are in the same texture, but our origin is based on the "correct" symbol, meaning each symbol needs to be handled slightly differently
            templeCodeDisplay.GetChild(0).GetComponent<MeshRenderer>().material.mainTextureOffset = new Vector2(templeCode[0] % 4 * 0.25f, templeCode[0] / 4 * -0.25f);
            templeCodeDisplay.GetChild(1).GetComponent<MeshRenderer>().material.mainTextureOffset = new Vector2(-0.75f + (templeCode[1] % 4 * 0.25f), templeCode[1] / 4 * -0.25f);
            templeCodeDisplay.GetChild(2).GetComponent<MeshRenderer>().material.mainTextureOffset = new Vector2(templeCode[2] % 4 * 0.25f, templeCode[2] / 4 * -0.25f);
            templeCodeDisplay.GetChild(3).GetComponent<MeshRenderer>().material.mainTextureOffset = new Vector2(-0.25f + (templeCode[3] % 4 * 0.25f), 0.25f - (templeCode[3] / 4 * 0.25f));
            templeCodeDisplay.GetChild(4).GetComponent<MeshRenderer>().material.mainTextureOffset = new Vector2(-0.25f + (templeCode[4] % 4 * 0.25f), 0.25f - (templeCode[4] / 4 * 0.25f));

            // Change the code paper for the jammer code in the code room
            Transform jammerCodeDisplayTower = GameObject.Find("RingWorld_Body/Sector_RingInterior/Sector_Zone2/Sector_DreamFireLighthouse_Zone2_AnimRoot/Interactibles_DreamFireLighthouse_Zone2/Pivot_CodeRoom").transform.GetChild(6).GetChild(2); // GetChild() is used because multiple objects here have the same name
            jammerCodeDisplayTower.transform.GetChild(0).GetComponent<MeshRenderer>().material.mainTextureOffset = new Vector2(jammerCode[0] % 4 * 0.25f, 0.25f - (jammerCode[0] / 4 * 0.25f));
            jammerCodeDisplayTower.transform.GetChild(1).GetComponent<MeshRenderer>().material.mainTextureOffset = new Vector2(-0.5f + (jammerCode[1] % 4 * 0.25f), 0.25f - (jammerCode[1] / 4 * 0.25f));
            jammerCodeDisplayTower.transform.GetChild(2).GetComponent<MeshRenderer>().material.mainTextureOffset = new Vector2(-0.75f + (jammerCode[2] % 4 * 0.25f), 0.25f - (jammerCode[2] / 4 * 0.25f));
            jammerCodeDisplayTower.transform.GetChild(3).GetComponent<MeshRenderer>().material.mainTextureOffset = new Vector2(-0.5f + (jammerCode[3] % 4 * 0.25f), 0.25f - (jammerCode[3] / 4 * 0.25f));
            jammerCodeDisplayTower.transform.GetChild(4).GetComponent<MeshRenderer>().material.mainTextureOffset = new Vector2(-0.75f + (jammerCode[4] % 4 * 0.25f), 0.25f - (jammerCode[4] / 4 * 0.25f));

            // Change the code paper for the jammer code in the jammer room
            GameObject jammerCodeDisplayLocal = GameObject.Find("RingWorld_Body/Sector_RingInterior/Sector_Zone4/Sector_BlightedShore/Sector_JammingControlRoom_Zone4/Interactables_JammingControlRoom_Zone4/Prefab_IP_PictureFrame_Door/CodeDecals_Zone4");
            jammerCodeDisplayLocal.transform.GetChild(0).GetComponent<MeshRenderer>().material.mainTextureOffset = new Vector2(jammerCode[0] % 4 * 0.25f, 0.25f - (jammerCode[0] / 4 * 0.25f));
            jammerCodeDisplayLocal.transform.GetChild(1).GetComponent<MeshRenderer>().material.mainTextureOffset = new Vector2(-0.5f + (jammerCode[1] % 4 * 0.25f), 0.25f - (jammerCode[1] / 4 * 0.25f));
            jammerCodeDisplayLocal.transform.GetChild(2).GetComponent<MeshRenderer>().material.mainTextureOffset = new Vector2(-0.75f + (jammerCode[2] % 4 * 0.25f), 0.25f - (jammerCode[2] / 4 * 0.25f));
            jammerCodeDisplayLocal.transform.GetChild(3).GetComponent<MeshRenderer>().material.mainTextureOffset = new Vector2(-0.5f + (jammerCode[3] % 4 * 0.25f), 0.25f - (jammerCode[3] / 4 * 0.25f));
            jammerCodeDisplayLocal.transform.GetChild(4).GetComponent<MeshRenderer>().material.mainTextureOffset = new Vector2(-0.75f + (jammerCode[4] % 4 * 0.25f), 0.25f - (jammerCode[4] / 4 * 0.25f));

            // Assign the new codes to the code totems
            GameObject.Find("RingWorld_Body/Sector_RingInterior/Sector_Zone4/Sector_BlightedShore/Sector_JammingControlRoom_Zone4/Interactables_JammingControlRoom_Zone4/VisibleFromFar_Interactables_JammingControlRoom_Zone4/Prefab_IP_CodeTotem").GetComponent<EclipseCodeController4>()._code = jammerCode;
            GameObject.Find("RingWorld_Body/Sector_RingInterior/Sector_Zone3/Sector_CanyonObvious/Structures_CanyonObvious/AbandonedTemple_Zone3/Interactables_AbandonedTemple_Zone3/SecretTempleDoor/Prefab_IP_CodeTotem").GetComponent<EclipseCodeController4>()._code = templeCode;
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(DreamWorldController), nameof(DreamWorldController.EnterDreamWorld))]
    public static void DreamWorldController_EnterDreamWorld(DreamWorldController __instance)
    {
        if (RandomizeCodes)
        {
            // Set the Dreamworld vault codes
            APRandomizer.OWMLModConsole.WriteLine($"DreamWorldController_EnterDreamWorld altering vault codes");
            GameObject.Find("DreamWorld_Body/Sector_DreamWorld/Sector_Underground/IslandsRoot/IslandPivot_A/Island_A/Interactibles_Island_A/InvisibleBridge/InvisibleBridgeSegment_1").GetComponent<InvisibleBridgeController>()._codeIndex = vaultCode1[0];
            GameObject.Find("DreamWorld_Body/Sector_DreamWorld/Sector_Underground/IslandsRoot/IslandPivot_A/Island_A/Interactibles_Island_A/InvisibleBridge/InvisibleBridgeSegment_2").GetComponent<InvisibleBridgeController>()._codeIndex = vaultCode1[1];
            GameObject.Find("DreamWorld_Body/Sector_DreamWorld/Sector_Underground/IslandsRoot/IslandPivot_A/Island_A/Interactibles_Island_A/InvisibleBridge/InvisibleBridgeSegment_3").GetComponent<InvisibleBridgeController>()._codeIndex = vaultCode1[2];
            GameObject.Find("DreamWorld_Body/Sector_DreamWorld/Sector_Underground/IslandsRoot/IslandPivot_A/Island_A/Interactibles_Island_A/InvisibleBridge/InvisibleBridgeSegment_4").GetComponent<InvisibleBridgeController>()._codeIndex = vaultCode1[3];
            GameObject.Find("DreamWorld_Body/Sector_DreamWorld/Sector_Underground/IslandsRoot/IslandPivot_A/Island_A/Interactibles_Island_A/InvisibleBridge/InvisibleBridgeSegment_5").GetComponent<InvisibleBridgeController>()._codeIndex = vaultCode1[4];
            GameObject.Find("DreamWorld_Body/Sector_DreamWorld/Sector_Underground/IslandsRoot/IslandPivot_B/Island_B/Interactibles_Island_B/Prefab_IP_DW_CodeTotem").GetComponent<EclipseCodeController4>()._code = vaultCode2;
            GameObject.Find("DreamWorld_Body/Sector_DreamWorld/Sector_Underground/IslandsRoot/IslandPivot_C/Island_C/Interactibles_Island_C/Prefab_IP_DW_CodeTotem").GetComponent<EclipseCodeController4>()._code = vaultCode3;
        }
    }

    private static ShipLogManager logManager = null;

    [HarmonyPrefix, HarmonyPatch(typeof(ShipLogManager), nameof(ShipLogManager.Awake))]
    public static void ShipLogManager_Awake_Prefix(ShipLogManager __instance)
    {
        logManager = __instance;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(ShipLogController), nameof(ShipLogController.EnterShipComputer))]
    public static void ShipLogController_EnterShipComputer_Prefix(ShipLogController __instance)
    {
        EnsureCodeSpriteCreated();
    }

    public static void EnsureCodeSpriteCreated()
    {
        if (RandomizeCodes == false) return;

        if (logManager != null)
        {
            ShipLogEntry ptmGeneratedEntry = null;
            var generatedEntryList = logManager.GetEntryList();
            if (generatedEntryList != null)
                ptmGeneratedEntry = generatedEntryList.Find(entry => entry.GetID() == "IP_ZONE_2_CODE");

            var tex = CodeAssets.LoadAsset<Texture2D>("CodeBackground"); // [!] Change to load from the main assetbundle
            var icons = CodeAssets.LoadAsset<Texture2D>("CodeIcons");
            for (int i = 0; i < 5; i++)
                for (int x = 0; x < 75; x++)
                    for (int y = 0; y < 75; y++)
                        tex.SetPixel(x + 380, y + 63 + (i * 75), icons.GetPixel(x + (75 * (templeCode[4 - i] % 4)), y + (75 * (templeCode[4 - i] / 4))));
            tex.Apply();
            templeCodeSprite = Sprite.Create(tex, new Rect(0, 0, 512, 512), new Vector2(0.5f, 0.5f));

            ptmGeneratedEntry?.SetAltSprite(templeCodeSprite);
        }
    }
}
