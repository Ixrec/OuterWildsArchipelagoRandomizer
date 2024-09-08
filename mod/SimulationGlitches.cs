using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class SimulationGlitches
{
    private static bool _hasLimboWarpPatch = false;

    public static bool hasLimboWarpPatch
    {
        get => _hasLimboWarpPatch;
        set
        {
            _hasLimboWarpPatch = value;

            if (_hasLimboWarpPatch)
            {
                var nd = new NotificationData(NotificationTarget.Player, "SIMULATION HACK SUCCESSFUL. APPLIED HOTFIX TO RE-ENABLE THE LIMBO WARP GLITCH.", 10);
                NotificationManager.SharedInstance.PostNotification(nd, false);
            }
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(DreamWarpVolume), nameof(DreamWarpVolume.FixedUpdate))]
    public static void DreamWarpVolume_FixedUpdate(DreamWarpVolume __instance)
    {
        if (
            !hasLimboWarpPatch && // we don't want to allow the warp
            __instance._playerFallingToUnderground && // the player is trying to trigger the warp by falling off the raft
            !Locator.GetDreamWorldController().IsExitingDream()) // and we haven't already started "killing" them/waking them up
        {
            APRandomizer.OWMLModConsole.WriteLine($"DreamWarpVolume_FixedUpdate 'killing' player because they attempted to use the limbo warp glitch without the AP item");
            Locator.GetDeathManager().KillPlayer(DeathType.Default);
        }
    }

    private static bool _hasProjectionRangePatch = false;

    public static bool hasProjectionRangePatch
    {
        get => _hasProjectionRangePatch;
        set
        {
            _hasProjectionRangePatch = value;

            if (_hasProjectionRangePatch)
            {
                var nd = new NotificationData(NotificationTarget.Player, "SIMULATION HACK SUCCESSFUL. APPLIED HOTFIX TO RE-ENABLE THE PROJECTION RANGE GLITCH.", 10);
                NotificationManager.SharedInstance.PostNotification(nd, false);
            }
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(DreamWorldController), nameof(DreamWorldController.ExitLanternBounds))]
    public static void DreamWorldController_ExitLanternBounds(DreamWorldController __instance)
    {
        if (_hasProjectionRangePatch) return;

        APRandomizer.OWMLModConsole.WriteLine($"DreamWorldController_ExitLanternBounds warping player back to dream lantern because they went outside its projection range");

        OWRigidbody playerRigidBody = Locator.GetPlayerBody();
        var lanternPosition = Locator.GetDreamWorldController().GetPlayerLantern().gameObject.transform.position;
        playerRigidBody.WarpToPositionRotation(lanternPosition + new UnityEngine.Vector3(0, 1, 0), playerRigidBody.GetRotation());
    }

    private static bool _hasAlarmBypassPatch = false;

    public static bool hasAlarmBypassPatch
    {
        get => _hasAlarmBypassPatch;
        set
        {
            _hasAlarmBypassPatch = value;

            if (_hasAlarmBypassPatch)
            {
                var nd = new NotificationData(NotificationTarget.Player, "SIMULATION HACK SUCCESSFUL. APPLIED HOTFIX TO RE-ENABLE THE ALARM BYPASS GLITCH.", 10);
                NotificationManager.SharedInstance.PostNotification(nd, false);
            }
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(DeathManager), nameof(DeathManager.CheckShouldWakeInDreamWorld))]
    public static bool DeathManager_CheckShouldWakeInDreamWorld(DeathManager __instance, ref bool __result)
    {
        if (!_hasAlarmBypassPatch)
        {
            APRandomizer.OWMLModConsole.WriteLine($"DeathManager_CheckShouldWakeInDreamWorld preventing DW entrance after death");
            __result = false;
            return false; // skip vanilla implementation
        }
        return true; // let vanilla implementation handle it
    }
}
