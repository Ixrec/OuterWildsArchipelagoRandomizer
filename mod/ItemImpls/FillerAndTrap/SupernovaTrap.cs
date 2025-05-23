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
            if (LoadManager.GetCurrentScene() != OWScene.SolarSystem && LoadManager.GetCurrentScene() != OWScene.EyeOfTheUniverse)
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
