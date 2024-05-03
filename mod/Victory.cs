using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using HarmonyLib;
using System;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class Victory
{
    public enum GoalSetting: long
    {
        SongOfFive = 0,
        SongOfSix = 1,
    }

    public static GoalSetting goalSetting = GoalSetting.SongOfFive;

    public static void SetGoal(long goal)
    {
        if (Enum.IsDefined(typeof(GoalSetting), goal))
            goalSetting = (GoalSetting)goal;
        else
            APRandomizer.OWMLModConsole.WriteLine($"{goal} is not a valid goal setting", OWML.Common.MessageType.Error);
    }
    public static void OnCompleteSceneLoad(OWScene _scene, OWScene loadScene)
    {
        if (loadScene != OWScene.EyeOfTheUniverse) return;

        var metSolanum = PlayerData.GetPersistentCondition("MET_SOLANUM");
        var metPrisoner = PlayerData.GetPersistentCondition("MET_PRISONER");

        APRandomizer.OWMLModConsole.WriteLine($"EyeOfTheUniverse scene loaded.\n" +
            $"MET_SOLANUM: {metSolanum}\n" +
            $"MET_PRISONER: {metPrisoner}\n" +
            $"Goal setting is: {goalSetting}");

        bool isVictory = false;
        if (goalSetting == GoalSetting.SongOfFive)
            isVictory = true;
        else // currently SongOfSix is the only other goal
        {
            if (metSolanum)
                isVictory = true;
            else
            {
                APRandomizer.OWMLModConsole.WriteLine($"Goal {goalSetting} is NOT completed. Notifying the player.", OWML.Common.MessageType.Info);
                APRandomizer.InGameAPConsole.AddText("<color=red>Goal NOT completed.</color> Your goal is Song of Six, but you haven't met Solanum yet. " +
                    "You can quickly return to the solar system without completing the Eye by pausing and selecting 'Quit and Reset to Solar System'.");
            }
        }

        if (isVictory)
        {
            APRandomizer.OWMLModConsole.WriteLine($"Goal {goalSetting} completed! Notifying AP server.", OWML.Common.MessageType.Success);

            var statusUpdatePacket = new StatusUpdatePacket();
            statusUpdatePacket.Status = ArchipelagoClientState.ClientGoal;
            APRandomizer.APSession.Socket.SendPacket(statusUpdatePacket);
        }
    }
}
