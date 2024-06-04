using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ArchipelagoRandomizer;

internal class Spawn
{
    // somehow the meaning of this changed while I was testing other stuff...
    private static Vector3 shipSpawnOffsetForChertsCamp = new Vector3(-24, -7, 5);

    private static bool spawnedInSuit = false;

    [HarmonyPrefix, HarmonyPatch(typeof(PlayerSpawner), nameof(PlayerSpawner.Update))]
    public static void PlayerSpawner_Update(PlayerSpacesuit __instance)
    {
        if (!spawnedInSuit)
        {
            APRandomizer.OWMLModConsole.WriteLine($"instant SuitUp()");
            Locator.GetPlayerSuit().SuitUp(isTrainingSuit: false, instantSuitUp: true, putOnHelmet: true);
            spawnedInSuit = true;
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(AlignPlayerWithForce), nameof(AlignPlayerWithForce.OnSuitUp))]
    public static bool AlignPlayerWithForce_OnSuitUp(AlignPlayerWithForce __instance)
    {
        if (!spawnedInSuit)
        {
            APRandomizer.OWMLModConsole.WriteLine($"skipping AlignPlayerWithForce_OnSuitUp");
            return false;
        }
        return true;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(PlayerSpawner), nameof(PlayerSpawner.SpawnPlayer))]
    public static void PlayerSpawner_SpawnPlayer(PlayerSpawner __instance)
    {
        APRandomizer.OWMLModConsole.WriteLine($"PlayerSpawner_SpawnPlayer");

        var sp = Locator.GetAstroObject(AstroObject.Name.CaveTwin).gameObject.transform.Find("SPAWNS/Spawn_ChertsCamp").GetComponent<SpawnPoint>();
        //var sp = __instance.GetSpawnPoint(SpawnLocation.HourglassTwin_1);
        __instance._initialSpawnPoint = sp;
        APRandomizer.OWMLModConsole.WriteLine($"PlayerSpawner_SpawnPlayer set player spawn {sp.transform.position}");

        //APRandomizer.OWMLModConsole.WriteLine($"_spawnList: {string.Join("\n",
        //    __instance._spawnList.Select(sp => $"{sp?.transform.parent.name}/{sp?.name}|{sp?._spawnLocation}|{sp?._isShipSpawn}|{sp?._triggerVolumes?.Count()}|{sp?._attachedBody?.name}|{sp?.transform.position}"))}");

        OWRigidbody owrigidbody = Locator.GetShipBody();
        var shipPos = sp.transform.position + shipSpawnOffsetForChertsCamp;
        owrigidbody.WarpToPositionRotation(shipPos, sp.transform.rotation);
        owrigidbody.SetVelocity(sp.GetPointVelocity());
        owrigidbody.GetRequiredComponent<MatchInitialMotion>().SetBodyToMatch(sp?._attachedBody);
        APRandomizer.OWMLModConsole.WriteLine($"PlayerSpawner_SpawnPlayer set ship spawn {shipSpawnOffsetForChertsCamp} / {shipPos} / {owrigidbody.transform.position}");
    }

    /*[HarmonyPrefix, HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.Update))]
    public static void ToolModeUI_Update_Prefix()
    {
        if (OWInput.SharedInputManager.IsNewlyPressed(InputLibrary.left2))
            shipSpawnOffsetForChertsCamp.x -= 1;
        if (OWInput.SharedInputManager.IsNewlyPressed(InputLibrary.right2))
            shipSpawnOffsetForChertsCamp.x += 1;
        if (OWInput.SharedInputManager.IsNewlyPressed(InputLibrary.flashlight))
            shipSpawnOffsetForChertsCamp.y -= 1;
        if (OWInput.SharedInputManager.IsNewlyPressed(InputLibrary.lockOn))
            shipSpawnOffsetForChertsCamp.y += 1;
        if (OWInput.SharedInputManager.IsNewlyPressed(InputLibrary.up2))
            shipSpawnOffsetForChertsCamp.z -= 1;
        if (OWInput.SharedInputManager.IsNewlyPressed(InputLibrary.down2))
            shipSpawnOffsetForChertsCamp.z += 1;
    }*/
}
