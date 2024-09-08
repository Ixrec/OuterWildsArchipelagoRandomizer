using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class SimulationDocks
{
    private static bool _hasDocksPatch = false;

    public static bool hasDocksPatch
    {
        get => _hasDocksPatch;
        set
        {
            _hasDocksPatch = value;

            if (_hasDocksPatch)
            {
                var nd = new NotificationData(NotificationTarget.Player, "SIMULATION HACK SUCCESSFUL. APPLIED HOTFIXES TO MULTIPLE AREAS ENABLING DIRECT ACCESS FROM SIMULATION RAFTS.", 10);
                NotificationManager.SharedInstance.PostNotification(nd, false);

                ApplyDockPatches();
            }
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(DreamWorldController), nameof(DreamWorldController.EnterDreamWorld))]
    public static void DreamWorldController_EnterDreamWorld(DreamWorldController __instance, DreamCampfire dreamCampfire, DreamArrivalPoint arrivalPoint, RelativeLocationData relativeLocation)
    {
        if (_hasDocksPatch)
        {
            APRandomizer.OWMLModConsole.WriteLine($"DreamWorldController_EnterDreamWorld calling ApplyDockPatches()");
            ApplyDockPatches();
        }
    }

    // for testing
    /*[HarmonyPrefix, HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.Update))]
    public static void ToolModeUI_Update_Prefix()
    {
        if (OWInput.SharedInputManager.IsNewlyPressed(InputLibrary.down2))
        {
            ApplyDockPatches();
        }
    }*/

    private static void ApplyDockPatches()
    {
        APRandomizer.OWMLModConsole.WriteLine($"ApplyDockPatches() called");

        var swDockDoorProjector = GameObject.Find("DreamWorld_Body/Sector_DreamWorld/Sector_DreamZone_1/Interactibles_DreamZone_1/Tunnel/Prefab_IP_DreamObjectProjector (2)");
        swDockDoorProjector.GetComponent<DreamObjectProjector>().SetLit(false);

        var scDockProjector = GameObject.Find("DreamWorld_Body/Sector_DreamWorld/Sector_DreamZone_2/Structure_DreamZone_2/LowerLevel/RaftDockProjector/Prefab_IP_DreamObjectProjector");
        scDockProjector.GetComponent<DreamObjectProjector>().SetLit(true);

        var ecDockElevator = GameObject.Find("DreamWorld_Body/Sector_DreamWorld/Sector_DreamZone_3/Interactibles_DreamZone_3/Elevator_Raft/Prefab_IP_DW_CageElevator");
        ecDockElevator.GetComponent<CageElevator>().GoDown();
    }
}
