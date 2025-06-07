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
            //only applicable in the solar system
            if (LoadManager.GetCurrentScene() != OWScene.SolarSystem)
                return;

            //simulate the "crashing into the Deep Space Satellite" event by just firing it
            GlobalMessenger.FireEvent("BrokeMapSatellite");
        }
    }
}
