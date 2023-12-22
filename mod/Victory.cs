using HarmonyLib;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class Victory
{
    // todo: settings for alternate goals

    static bool prisonerJoined = false;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(QuantumCampsiteController), nameof(QuantumCampsiteController.OnPrisonerErased))]
    public static void QuantumCampsiteController_OnPrisonerErased_Prefix() => prisonerJoined = false;
    [HarmonyPrefix]
    [HarmonyPatch(typeof(QuantumCampsiteController), nameof(QuantumCampsiteController.OnPrisonerJoined))]
    public static void QuantumCampsiteController_OnPrisonerJoined_Prefix() => prisonerJoined = true;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(QuantumCampsiteController), nameof(QuantumCampsiteController.CheckTravelersGathered))]
    public static void QuantumCampsiteController_CheckTravelersGathered_Prefix(QuantumCampsiteController __instance)
    {
        Randomizer.OWMLModConsole.WriteLine($"QuantumCampsiteController.CheckTravelersGathered\n" +
            $"AreAllTravelersGathered(): {__instance.AreAllTravelersGathered()}\n" +
            $"MET_SOLANUM: {PlayerData.GetPersistentCondition("MET_SOLANUM")}\n" +
            $"MET_PRISONER: {PlayerData.GetPersistentCondition("MET_PRISONER")}\n" +
            $"Prisoner joined: {prisonerJoined}");

        if (__instance.AreAllTravelersGathered())
        {
            // todo: send AP victory message
            Randomizer.OWMLModConsole.WriteLine($"Goal completed!", OWML.Common.MessageType.Success);
        }
    }
}
