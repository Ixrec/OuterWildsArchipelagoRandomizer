using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ArchipelagoRandomizer.ItemImpls.FCProgression
{
    class FixRecursiveNode
    {
        [HarmonyPostfix, HarmonyPatch(typeof(PlayerSectorDetector), nameof(PlayerSectorDetector.OnAddSector))]
        public static void PlayerSectorDetector_OnAddSector(PlayerSectorDetector __instance, Sector sector)
        {
            if (sector._idString != "Briar's Hollow") return;
            // For an unknown reason, the recursive node in Briar's Hollow is sometimes disabled. We forcibly re-enable it here
            GameObject loopNode = GameObject.Find("BriarsHollow_Body/Sector/Loop Node");
            if (!loopNode.activeSelf)
            {
                APRandomizer.OWMLModConsole.WriteLine($"PlayerSectorDetector_OnAddSector() Recursive mode disabled. Re-enabling", OWML.Common.MessageType.Warning);
                loopNode.SetActive(true);
            }
        }
    }
}
