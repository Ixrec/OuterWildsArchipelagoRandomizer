using HarmonyLib;

namespace ArchipelagoRandomizer.ItemImpls.FCProgression
{
    static class DeepBrambleCoordinates
    {
        public static void ChangeEnterWarp()
        {
            APRandomizer.OWMLModConsole.WriteLine("ChangeEnterWarp() changing Deep Bramble warp requirements");
            APRandomizer.NewHorizonsAPI.DefineStarSystem("DeepBramble", "{ \"factRequiredForWarp\": \"TH_VILLAGE_X1\"}", APRandomizer.Instance);
        }
    }
}
