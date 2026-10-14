using HarmonyLib;

namespace ArchipelagoRandomizer.ItemImpls.FCProgression
{
    [HarmonyPatch]
    static class DeepBrambleCoordinates
    {
        public static void ChangeExitWarp()
        {
            APRandomizer.NewHorizonsAPI.DefineStarSystem("DeepBramble", "{ \"factRequiredToEnterViaWarpDrive\": \"\"}", APRandomizer.Instance);
        }
    }
}
