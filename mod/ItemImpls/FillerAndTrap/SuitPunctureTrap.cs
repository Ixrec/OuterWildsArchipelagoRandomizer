using HarmonyLib;

namespace ArchipelagoRandomizer
{
    [HarmonyPatch]
    internal class SuitPunctureTrap
    {
        public static uint _suitPunctureTraps;

        public static uint suitPunctureTraps
        {
            get => _suitPunctureTraps;
            set
            {
                if (value > _suitPunctureTraps)
                {
                    PunctureSuit();
                }
                _suitPunctureTraps = value;
            }
        }

        static void PunctureSuit()
        {
            //we're not in gameplay, ignore
            if (LoadManager.GetCurrentScene() != OWScene.SolarSystem && LoadManager.GetCurrentScene() != OWScene.EyeOfTheUniverse)
                return;

            playerResources.ApplySuitPuncture();

            //if the player is not wearing the suit (rare unless playing suitless) the oxygen leak sound
            //will still play. In order for it to not get annoying, we disable it
            if (!PlayerState.IsWearingSuit())
            {
                playerResources._playerAudioController.UpdateSuitPunctures(0, PlayerResources._maxSuitPunctures);
            }
        }

        static PlayerResources playerResources = null;

        [HarmonyPrefix, HarmonyPatch(typeof(PlayerResources), nameof(PlayerResources.Awake))]
        public static void PlayerResources_Awake(PlayerResources __instance) => playerResources = __instance;
    }
}
