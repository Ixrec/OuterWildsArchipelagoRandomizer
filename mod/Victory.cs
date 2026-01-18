using HarmonyLib;
using System;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
public class Victory
{
    public enum GoalSetting: long
    {
        SongOfFive = 0,
        SongOfTheNomai = 1,
        SongOfTheStranger = 2,
        SongOfSix = 3,
        SongOfSeven = 4,
        EchoesOfTheEye = 5
    }

    public static GoalSetting goalSetting = GoalSetting.SongOfFive;

    public static void SetGoal(long goal)
    {
        if (Enum.IsDefined(typeof(GoalSetting), goal))
            goalSetting = (GoalSetting)goal;
        else
            APRandomizer.OWMLModConsole.WriteLine($"{goal} is not a valid goal setting", OWML.Common.MessageType.Error);
    }

    public static bool hasMetSolanum() => PlayerData.GetPersistentCondition("MET_SOLANUM");
    public static bool hasMetPrisoner() => PlayerData.GetPersistentCondition("MET_PRISONER");

    public static void OnCompleteSceneLoad(OWScene _scene, OWScene loadScene)
    {
        if (loadScene != OWScene.EyeOfTheUniverse) return;

        var metSolanum = hasMetSolanum();
        var metPrisoner = hasMetPrisoner();

        APRandomizer.OWMLModConsole.WriteLine($"EyeOfTheUniverse scene loaded.\n" +
            $"MET_SOLANUM: {metSolanum}\n" +
            $"MET_PRISONER: {metPrisoner}\n" +
            $"Goal setting is: {goalSetting}");

        bool isVictory = false;
        string uniqueMessagePart = null;
        if (goalSetting == GoalSetting.SongOfFive)
        {
            isVictory = true;
        }
        else if (goalSetting == GoalSetting.SongOfTheNomai)
        {
            if (metSolanum)
                isVictory = true;
            else
                uniqueMessagePart = "Your goal is Song of the Nomai, but you haven't met Solanum yet.";
        }
        else if (goalSetting == GoalSetting.SongOfTheStranger)
        {
            if (metPrisoner)
                isVictory = true;
            else
                uniqueMessagePart = "Your goal is Song of the Stranger, but you haven't met the Prisoner yet.";
        }
        else if (goalSetting == GoalSetting.SongOfSix)
        {
            if (metSolanum || metPrisoner)
                isVictory = true;
            else
                uniqueMessagePart = "Your goal is Song of Six, but you haven't met either Solanum or the Prisoner yet.";
        }
        else if (goalSetting == GoalSetting.SongOfSeven)
        {
            if (metSolanum && metPrisoner)
                isVictory = true;
            else
            {
                if (!metSolanum && !metPrisoner)
                    uniqueMessagePart = "Your goal is Song of Seven, but you haven't met either Solanum or the Prisoner yet.";
                else if (!metSolanum)
                    uniqueMessagePart = "Your goal is Song of Seven, but you haven't met Solanum yet.";
                else
                    uniqueMessagePart = "Your goal is Song of Seven, but you haven't met the Prisoner yet.";
            }
        }
        else if (goalSetting == GoalSetting.EchoesOfTheEye)
        {
            uniqueMessagePart = "Your goal is Echoes of the Eye, which doesn't involve warping to the Eye of the Universe.";
        }
        else
        {
            APRandomizer.OWMLModConsole.WriteLine($"Goal setting is an unsupported value of {goalSetting}. Aborting.", OWML.Common.MessageType.Error);
            return;
        }

        if (isVictory)
        {
            SetGoalAchieved();
        }
        else
        {
            APRandomizer.OWMLModConsole.WriteLine($"Goal {goalSetting} is NOT completed. Notifying the player.", OWML.Common.MessageType.Info);
            var inGameMessage = "<color=red>Goal NOT completed.</color> " + uniqueMessagePart + " You can quickly return to the solar system " +
                "without completing the Eye by pausing and selecting 'Quit and Reset to Solar System'.";
            APRandomizer.InGameAPConsole.AddText(inGameMessage);
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(EchoesOverController), nameof(EchoesOverController.OnTriggerEndOfDLC))]
    public static void EchoesOverController_OnTriggerEndOfDLC()
    {
        APRandomizer.OWMLModConsole.WriteLine($"EchoesOverController_OnTriggerEndOfDLC() called");

        if (goalSetting == GoalSetting.EchoesOfTheEye)
            SetGoalAchieved();
        else
            APRandomizer.OWMLModConsole.WriteLine($"Echoes of the Eye DLC completed, but the goal was {goalSetting}. Doing nothing.", OWML.Common.MessageType.Info);
    }

    private static void SetGoalAchieved()
    {
        APRandomizer.OWMLModConsole.WriteLine($"Goal {goalSetting} completed! Notifying AP server.", OWML.Common.MessageType.Success);

        APRandomizer.InGameAPConsole.AddText("<color='green'>Goal completed!</color> Notifying AP server.");

        APRandomizer.APSession.SetGoalAchieved();
    }
}
