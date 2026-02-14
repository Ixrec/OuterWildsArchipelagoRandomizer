using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;

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
        EchoesOfTheEye = 5,
        SongOfTheUniverse = 6,
    }

    public static GoalSetting goalSetting = GoalSetting.SongOfFive;
    // This is set to whichever system we most recently spawned in. Apparently, the afterlife is part of the vanilla system according to NH
    private static string currentSystem = "SolarSystem";

    public static void SetGoal(long goal)
    {
        if (Enum.IsDefined(typeof(GoalSetting), goal))
            goalSetting = (GoalSetting)goal;
        else
            APRandomizer.OWMLModConsole.WriteLine($"{goal} is not a valid goal setting", OWML.Common.MessageType.Error);
    }

    public static bool HasMetSolanum => PlayerData.GetPersistentCondition("MET_SOLANUM");
    public static bool HasMetPrisoner => PlayerData.GetPersistentCondition("MET_PRISONER");
    public static bool HasFinishedHearthsNeighbor1 => APRandomizer.SaveData.locationsChecked[Location.HN1_SIGNAL_GC_COCKPIT];
    public static bool HasFinishedTheOutsider => APRandomizer.SaveData.locationsChecked[Location.TO_CLIFFSIDE_DECAY];
    public static bool HasFinishedAstralCodec => APRandomizer.SaveData.locationsChecked[Location.AC_LC_ASTRAL_CODEC];
    public static bool HasFinishedHearthsNeighbor2 => APRandomizer.SaveData.locationsChecked[Location.HN2_ASCEND];
    public static bool HasFinishedFretsQuest => APRandomizer.SaveData.locationsChecked[Location.FQ_LYRICS_DONE];
    public static bool HasFinishedForgottenCastaways => APRandomizer.SaveData.locationsChecked[Location.FC_MOURNING];
    public static bool HasFinishedEchoHike => APRandomizer.SaveData.locationsChecked[Location.EH_PHOSPHORS];
    public static int FriendsMet => ((IEnumerable<bool>)[
        HasMetSolanum,
        HasMetPrisoner,
        HasFinishedHearthsNeighbor1,
        HasFinishedTheOutsider,
        HasFinishedAstralCodec,
        HasFinishedHearthsNeighbor2,
        HasFinishedFretsQuest,
        HasFinishedForgottenCastaways,
        HasFinishedEchoHike,
    ]).Count(x => x);
    public static bool HasMetRequiredFriends {
        get {
            if (!APRandomizer.SlotData.TryGetValue("required_friends", out object required_friends))
            {
                // If there's an issue retrieving the required_friends option, just default to true instead of preventing victory entirely
                APRandomizer.OWMLModConsole.WriteLine("Unable to read 'required_friends' from slot data.", OWML.Common.MessageType.Error);
                return true;
            }

            return FriendsMet >= (long)required_friends;
        }
    }

    public static void OnCompleteSceneLoad(OWScene _scene, OWScene loadScene)
    {
        if (APRandomizer.NewHorizonsAPI is INewHorizons nhAPI) // Save current system for future reference
            currentSystem = APRandomizer.NewHorizonsAPI.GetCurrentStarSystem();

        if (loadScene != OWScene.EyeOfTheUniverse) return;

        var metSolanum = HasMetSolanum;
        var metPrisoner = HasMetPrisoner;

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
        else if (goalSetting == GoalSetting.SongOfTheUniverse)
        {
            if (!APRandomizer.SlotData.TryGetValue("required_friends", out object required_friends))
            {
                APRandomizer.OWMLModConsole.WriteLine("Unable to read 'required_friends' from slot data.", OWML.Common.MessageType.Error);
                uniqueMessagePart = "Your goal is Song of the Universe, but there was an issue retrieving the `required_friends` option. So congrats!";
                isVictory = true;
            }
            else
            {
                int friendsMet = FriendsMet;
                long requiredFriends = (long)required_friends;
                if (friendsMet >= requiredFriends)
                    isVictory = true;
                else
                    uniqueMessagePart = $"Your goal is Song of the Universe, but you have only met {friendsMet} of the required {requiredFriends} friends.";
            }
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

        if (currentSystem != "SolarSystem")
            return; // Do not complete goal unless we're seeing DLC credits in the vanilla system

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
