using HarmonyLib;

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
        public static void ShipLogManager_Start_Postfix() => CheckEnableWarp();

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

        public static void ChangeExitWarp()
        {
            APRandomizer.NewHorizonsAPI.DefineStarSystem("DeepBramble", "{ \"factRequiredToExitViaWarpDrive\": \"NOMAI_WARP_FACT_FC\"}", APRandomizer.Instance);
        }
    }
}
