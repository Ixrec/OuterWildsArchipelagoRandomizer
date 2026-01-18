using HarmonyLib;

namespace ArchipelagoRandomizer.ItemImpls.FCProgression
{
    class QuantumProbability
    {
        private static bool _hasProbabilityKnowledge = false;

        public static bool hasProbabilityKnowledge
        {
            get => _hasProbabilityKnowledge;
            set
            {
                if (_hasProbabilityKnowledge != value)
                {
                    _hasProbabilityKnowledge = value;
                }
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(QuantumObject), nameof(QuantumObject.IsPlayerEntangled))]
        public static void QuantumObject_IsPlayerEntangled_Postfix(ref bool __result)
        {
            if (APRandomizer.NewHorizonsAPI == null) return;
            if (APRandomizer.NewHorizonsAPI.GetCurrentStarSystem() != "DeepBramble") return;
            if (!_hasProbabilityKnowledge)
                __result = false;
        }
    }
}
