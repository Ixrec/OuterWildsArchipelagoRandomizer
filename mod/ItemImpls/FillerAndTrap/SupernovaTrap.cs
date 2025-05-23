using HarmonyLib;

namespace ArchipelagoRandomizer
{
    [HarmonyPatch]
    internal class SupernovaTrap
    {
        private static uint _supernovaTraps;

        public static uint supernovaTraps
        {
            get => _supernovaTraps;
            set
            {
                if (value > _supernovaTraps)
                {
                    TriggerSupernova();
                }
                _supernovaTraps = value;
            }
        }

        private static bool triggeredSupernovaInThisLoop = false;

        private static void TriggerSupernova()
        {
            //trigger ONLY in the main solar system scene. Supernova while reaching the eye is not a good idea
            if (LoadManager.GetCurrentScene() != OWScene.SolarSystem)
                return;

            if (triggeredSupernovaInThisLoop)
                return;

            triggeredSupernovaInThisLoop = true;
            GlobalMessenger.FireEvent("TriggerSupernova");
        }

        [HarmonyPrefix, HarmonyPatch(typeof(TimeLoop), nameof(TimeLoop.Start))]
        private static void TimeLoop_Start_Prefix() => triggeredSupernovaInThisLoop = false;
    }
}
