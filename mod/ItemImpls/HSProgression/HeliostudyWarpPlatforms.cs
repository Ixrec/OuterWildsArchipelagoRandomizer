using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XGamingRuntime;

namespace ArchipelagoRandomizer.ItemImpls.HSProgression
{
    [HarmonyPatch]
    internal class HeliostudyWarpPlatforms
    {
        static string[] _heliostudySectorNames = new string[]
        {
            "Walker_Jam5_Station",
            "Walker_Jam5_Planet1",
            "Walker_Jam5_Planet2",
            "Walker_Jam5_Planet3",
            "Walker_Jam5_Planet4",
        };

        // We want to disable the Heliostudy warp platforms if you do not have the AP item, but otherwise default begavior is acceptable,
        // as the player has complete control over the system and the manual warps would introduce strange behaviors.
        [HarmonyPrefix, HarmonyPatch(typeof(NomaiWarpTransmitter), nameof(NomaiWarpTransmitter.FixedUpdate))]
        public static bool NomaiWarpTransmitter_FixedUpdate_Prefix(NomaiWarpTransmitter __instance)
        {
            if (PlayerSectors._sectorList.Where(sector => _heliostudySectorNames.Contains(sector._idString)).Any()) return WarpPlatforms.hasNomaiWarpCodes;
            return true;
        }

        static PlayerSectorDetector PlayerSectors = null;

        [HarmonyPrefix, HarmonyPatch(typeof(PlayerSectorDetector), nameof(PlayerSectorDetector.Awake))]
        public static void PlayerSectorDetector_Awake(PlayerSectorDetector __instance) => PlayerSectors = __instance;
    }
}