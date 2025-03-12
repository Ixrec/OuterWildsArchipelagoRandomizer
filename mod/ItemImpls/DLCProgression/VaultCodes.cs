using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class VaultCodes
{
    [HarmonyPrefix, HarmonyPatch(typeof(DreamWorldController), nameof(DreamWorldController.Start))]
    public static void DreamWorldController_Start(DreamWorldController __instance)
    {
        APRandomizer.OWMLModConsole.WriteLine($"DreamWorldController_Start disabling vault codes");
        GameObject.Find("DreamWorld_Body/Sector_DreamWorld/Sector_Underground/IslandsRoot/IslandPivot_C/Island_C/Interactibles_Island_C/Prefab_IP_DW_CodeTotem").GetComponent<EclipseCodeController4>()._code[0] = 8;
        GameObject.Find("DreamWorld_Body/Sector_DreamWorld/Sector_Underground/IslandsRoot/IslandPivot_B/Island_B/Interactibles_Island_B/Prefab_IP_DW_CodeTotem").GetComponent<EclipseCodeController4>()._code[0] = 8;
    }
}
