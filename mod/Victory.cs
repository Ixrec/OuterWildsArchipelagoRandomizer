using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using HarmonyLib;
using Newtonsoft.Json;
using System;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class Victory
{
    enum GoalSetting: long
    {
        SongOfFive = 0,
        SongOfSix = 1,
    }

    private static GoalSetting goalSetting = GoalSetting.SongOfFive;

    public static void SetGoal(long goal)
    {
        Randomizer.OWMLModConsole.WriteLine($"SetGoal() called with: {goal}");

        if (Enum.IsDefined(typeof(GoalSetting), goal))
            goalSetting = (GoalSetting)goal;
        else
            Randomizer.OWMLModConsole.WriteLine($"{goal} is not a valid goal setting", OWML.Common.MessageType.Error);
    }

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
        var metSolanum = PlayerData.GetPersistentCondition("MET_SOLANUM");

        Randomizer.OWMLModConsole.WriteLine($"QuantumCampsiteController.CheckTravelersGathered\n" +
            $"AreAllTravelersGathered(): {__instance.AreAllTravelersGathered()}\n" +
            $"MET_SOLANUM: {metSolanum}\n" +
            $"MET_PRISONER: {PlayerData.GetPersistentCondition("MET_PRISONER")}\n" +
            $"Prisoner joined: {prisonerJoined}\n" +
            $"Goal setting is: {goalSetting}");

        if (__instance.AreAllTravelersGathered())
        {
            bool isVictory = false;
            if (goalSetting == GoalSetting.SongOfFive)
                isVictory = true;
            else // currently SongOfSix is the only other goal
                isVictory = metSolanum;

            if (isVictory)
            {
                Randomizer.OWMLModConsole.WriteLine($"Goal {goalSetting} completed! Notifying AP server.", OWML.Common.MessageType.Success);

                var statusUpdatePacket = new StatusUpdatePacket();
                statusUpdatePacket.Status = ArchipelagoClientState.ClientGoal;
                Randomizer.APSession.Socket.SendPacket(statusUpdatePacket);
            }
        }
    }
}
