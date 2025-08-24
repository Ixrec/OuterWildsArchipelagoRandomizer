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

        internal static void TriggerSupernova()
        {
            //trigger ONLY in the main solar system scene. Supernova while reaching the eye is not a good idea
            if (LoadManager.GetCurrentScene() != OWScene.SolarSystem)
                return;

            //prevents triggering twice if a supernova is already in progress- whether by this trap of by the timeloop
            if (triggeredSupernovaInThisLoop || TimeLoop.GetSecondsRemaining() <= 0.0)
                return;

            triggeredSupernovaInThisLoop = true;
            GlobalMessenger.FireEvent("TriggerSupernova");
        }

        [HarmonyPrefix, HarmonyPatch(typeof(TimeLoop), nameof(TimeLoop.Awake))]
        private static void TimeLoop_Awake_Prefix() => triggeredSupernovaInThisLoop = false;
    }
}
