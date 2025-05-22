using HarmonyLib;

namespace ArchipelagoRandomizer
{
    [HarmonyPatch]
    internal class MapDisableTrap
    {
        private static uint _mapDisableTraps;

        public static uint mapDisableTraps
        {
            get => _mapDisableTraps;
            set
            {
                if (value > _mapDisableTraps)
                {
                    DisableMap();
                }
                _mapDisableTraps = value;
            }
        }

        static void DisableMap()
        {
            if (LoadManager.GetCurrentScene() != OWScene.SolarSystem && LoadManager.GetCurrentScene() != OWScene.EyeOfTheUniverse)
                return;

            GlobalMessenger.FireEvent("BrokeMapSatellite");
        }
    }
}
