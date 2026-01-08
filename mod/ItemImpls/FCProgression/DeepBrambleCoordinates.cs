using HarmonyLib;
using System.Collections;
using UnityEngine;

namespace ArchipelagoRandomizer.ItemImpls.FCProgression
{
    [HarmonyPatch]
    static class DeepBrambleCoordinates
    {
        private static bool _hasDeepBrambleCoordinates = false;

        public static bool HasDeepBrambleCoordinates
        {
            get => _hasDeepBrambleCoordinates;
            set
            {
                _hasDeepBrambleCoordinates = value;
                CheckEnableWarp();
            }
        }

        private static void CheckEnableWarp() {
            if (!_hasDeepBrambleCoordinates) return;

            ShipLogManager slm = Locator.GetShipLogManager();
            if (slm is null) return;

            string system = APRandomizer.NewHorizonsAPI?.GetCurrentStarSystem();
            if (system == "SolarSystem")
            {
                if (!slm.IsFactRevealed("WARP_TO_DB_FACT"))
                    slm.RevealFact("WARP_TO_DB_FACT");
            }
            else if (system == "DeepBramble")
            {
                if (!slm.IsFactRevealed("NOMAI_WARP_FACT_FC"))
                    slm.RevealFact("NOMAI_WARP_FACT_FC");
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ShipLogManager), nameof(ShipLogManager.Start))]
        public static void ShipLogManager_Start_Postfix() => APRandomizer.Instance.StartCoroutine(EnableWarpDelayed());

        static IEnumerator EnableWarpDelayed() {
            // Need to delay the check so NH has time to do some setup
            // otherwise we get a NullReferenceException in ShipLogStarChartMode.AddSystemCard
            yield return new WaitForSeconds(1);
            CheckEnableWarp();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ShipLogManager), nameof(ShipLogManager.RevealFact))]
        public static bool RevealFactPatch(ShipLogManager __instance, string id)
        {
            // These log facts control your ability to warp to and from the Deep Bramble. These facts are items as a result.
            if (id == "WARP_TO_DB_FACT" || id == "NOMAI_WARP_FACT_FC")
            {
                if (!_hasDeepBrambleCoordinates)
                    return false;
            }
            return true;
        }

        public static void ExitWarpFix()
        {
            APRandomizer.NewHorizonsAPI.DefineStarSystem("DeepBramble", "{ \"factRequiredToExitViaWarpDrive\": \"NOMAI_WARP_FACT_FC\"}", APRandomizer.Instance);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(PlayerData), nameof(PlayerData.SetLastDeathType))]
        public static void PlayerData_SetLastDeathType() // FC sometimes changes the default system, so we reset the default system every time the player dies (for real).
        {
            if (APRandomizer.NewHorizonsAPI is null || !APRandomizer.SlotEnabledMod("enable_fc_mod"))
                return;

            if (Spawn.spawnChoice == Spawn.SpawnChoice.DeepBramble)
                APRandomizer.NewHorizonsAPI.SetDefaultSystem("DeepBramble");
            else
                APRandomizer.NewHorizonsAPI.SetDefaultSystem("SolarSystem");
        }
    }
}
