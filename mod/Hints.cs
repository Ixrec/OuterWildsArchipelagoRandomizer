using Archipelago.MultiClient.Net.Enums;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class Hints
{
    private static Dictionary<string, string[]> characterToLocationPrefixes = new Dictionary<string, string[]>
    {
        { "Chert", [ "AT: ", "ET: ", "AT Ship Log: ", "ET Ship Log: " ] },
        { "Esker", [ "TH: ", "AR: ", "TH Ship Log: ", "AR Ship Log: " ] },
        { "Riebeck", [ "BH: ", "HL: ", "BH Ship Log: ", "HL Ship Log: " ] },
        { "Gabbro", [ "GD: ", "OPC: ", "GD Ship Log: ", "OPC Ship Log: " ] },
        { "Feldspar", [ "DB: ", "DB Ship Log: " ] },
    };
    private static Dictionary<string, string[]> characterToQuestionNodes = new Dictionary<string, string[]>
    {
        { "Chert", ["Questions"] },
        { "Esker", ["Esker1", "Esker2"] },
        { "Riebeck", ["Questions"] },
        { "Gabbro", ["GabbroMenu"] },
        { "Feldspar", ["FeldsparPrimaryMenu", "FeldsparSecondary"] },
    };

    private const string HintOptionTextId = "APRandomizer_HintOption";

    private const string Placeholder = "HINT_NOT_YET_GENERATED";

    private static Dictionary<string, string> TextIDToDisplayText = InitialTextIDToDisplayText();
    private static Dictionary<string, string> InitialTextIDToDisplayText() => new Dictionary<string, string>
    {
        { HintOptionTextId, "[Archipelago Hints] Where should I explore here?" },
        { "APRandomizer_Chert_HintsNode_TextPage1", Placeholder },
        { "APRandomizer_Chert_HintsNode_TextPage2", Placeholder },
        { "APRandomizer_Esker_HintsNode_TextPage1", Placeholder },
        { "APRandomizer_Esker_HintsNode_TextPage2", Placeholder },
        { "APRandomizer_Riebeck_HintsNode_TextPage1", Placeholder },
        { "APRandomizer_Riebeck_HintsNode_TextPage2", Placeholder },
        { "APRandomizer_Gabbro_HintsNode_TextPage1", Placeholder },
        { "APRandomizer_Gabbro_HintsNode_TextPage2", Placeholder },
        { "APRandomizer_Feldspar_HintsNode_TextPage1", Placeholder },
        { "APRandomizer_Feldspar_HintsNode_TextPage2", Placeholder },
    };

    // Cache invalidation, so we don't mistakenly use one slot's hints on another slot
    public static void OnCompleteSceneLoad()
    {
        TextIDToDisplayText = InitialTextIDToDisplayText();
    }

    [HarmonyPrefix, HarmonyPatch(typeof(TextTranslation), nameof(TextTranslation.Translate))]
    public static bool TextTranslation_Translate(TextTranslation __instance, string key, ref string __result)
    {
        if (TextIDToDisplayText.TryGetValue(key, out string displayText))
        {
            __result = displayText;
            return false;
        }
        return true;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(CharacterDialogueTree), nameof(CharacterDialogueTree.InputDialogueOption))]
    public static void CharacterDialogueTree_InputDialogueOption(CharacterDialogueTree __instance, int optionIndex)
    {
        //APRandomizer.OWMLModConsole.WriteLine($"CharacterDialogueTree_InputDialogueOption passed {optionIndex} at node {__instance._currentNode?.Name}");

        string selectedTextId = null;
        if (__instance._currentNode.ListDialogueOptions != null && optionIndex >= 0 && optionIndex < __instance._currentNode.ListDialogueOptions.Count())
            selectedTextId = __instance._currentNode.ListDialogueOptions[optionIndex]?._textID;

        if (selectedTextId != HintOptionTextId)
            return;

        var character = __instance._characterName;
        var textId1 = $"APRandomizer_{character}_HintsNode_TextPage1";
        var textId2 = $"APRandomizer_{character}_HintsNode_TextPage2";

        if (TextIDToDisplayText[textId1] != Placeholder && TextIDToDisplayText[textId2] != Placeholder)
        {
            APRandomizer.OWMLModConsole.WriteLine($"CharacterDialogueTree_InputDialogueOption returning early because {character}'s hints are already set up");
            return;
        }

        if (APRandomizer.SaveData.hintsGenerated != null && APRandomizer.SaveData.hintsGenerated.TryGetValue(character, out var hintsForCharacter) && hintsForCharacter.Length == 2)
        {
            APRandomizer.OWMLModConsole.WriteLine($"CharacterDialogueTree_InputDialogueOption taking {character}'s hints from save data");
            TextIDToDisplayText[textId1] = hintsForCharacter[0];
            TextIDToDisplayText[textId2] = hintsForCharacter[1];
            return;
        }

        APRandomizer.OWMLModConsole.WriteLine($"CharacterDialogueTree_InputDialogueOption generating hints for {character}");

        // Since there are no "hint characters" for DLC or story mods, logsanity
        // is the only option affecting which locations are relevant.
        var prefixes = characterToLocationPrefixes[character];
        var relevantLocations = LocationNames.locationNames.Keys
            .Where(loc => LocationNames.locationToArchipelagoId.ContainsKey(loc))
            .Where(loc => prefixes.Any(p => LocationNames.locationNames[loc].StartsWith(p)));
        if (!APRandomizer.SlotEnabledLogsanity())
            relevantLocations = relevantLocations.Where(loc => !loc.ToString().StartsWith("SLF__"));

        var allChecked = APRandomizer.APSession.Locations.AllLocationsChecked;
        var uncheckedRelevantLocations = relevantLocations.Where(loc => {
            if (!LocationNames.locationToArchipelagoId.ContainsKey(loc)) // e.g. SLF__TH_VILLAGE_X3
                return false;
            return !allChecked.Contains(LocationNames.locationToArchipelagoId[loc]);
        });

        APRandomizer.OWMLModConsole.WriteLine($"CharacterDialogueTree_InputDialogueOption found {uncheckedRelevantLocations.Count()} uncheckedRelevantLocations for {character}");

        if (!uncheckedRelevantLocations.Any())
        {
            TextIDToDisplayText[textId1] = "You've already explored everything here.";
            TextIDToDisplayText[textId2] = "I have no hints to give you.";
            return;
        }

        var relevantScouts = LocationScouter.ScoutedLocations.Where(kv => uncheckedRelevantLocations.Contains(kv.Key));
        if (relevantScouts.Count() != uncheckedRelevantLocations.Count())
        {
            var scoutsOnly = relevantScouts.Where(scout => !uncheckedRelevantLocations.Contains(scout.Key));
            var uncheckedOnly = uncheckedRelevantLocations.Where(loc => !relevantScouts.Any(scout => scout.Key == loc));
            APRandomizer.OWMLModConsole.WriteLine(
                $"CharacterDialogueTree_InputDialogueOption only found {relevantScouts.Count()} relevant scouts for {uncheckedRelevantLocations.Count()} uncheckedRelevantLocations.\n" +
                $"relevantScouts had {string.Join(", ", scoutsOnly.Select(kv => kv.Key))} which uncheckedRelevantLocations did not.\n" +
                $"uncheckedRelevantLocations had {string.Join(", ", uncheckedOnly)} which relevantScouts did not.",
                OWML.Common.MessageType.Warning
            );
        }

        var progression = relevantScouts.Where(kv => kv.Value.Flags.HasFlag(ItemFlags.Advancement)).ToList();
        var useful = relevantScouts.Where(kv => kv.Value.Flags.HasFlag(ItemFlags.NeverExclude) && !kv.Value.Flags.HasFlag(ItemFlags.Advancement)).ToList();
        var other = relevantScouts.Where(kv => !kv.Value.Flags.HasFlag(ItemFlags.NeverExclude) && !kv.Value.Flags.HasFlag(ItemFlags.Advancement)).ToList();

        Random prng = new Random();
        List<KeyValuePair<Location, ArchipelagoItem>> stuffToHint = new();
        while (progression.Any() && stuffToHint.Count() < 2)
        {
            var index = prng.Next(0, progression.Count());
            stuffToHint.Add(progression[index]);
            progression.RemoveAt(index);
        }
        while (useful.Any() && stuffToHint.Count() < 2)
        {
            var index = prng.Next(0, useful.Count());
            stuffToHint.Add(useful[index]);
            useful.RemoveAt(index);
        }
        while (other.Any() && stuffToHint.Count() < 2)
        {
            var index = prng.Next(0, other.Count());
            stuffToHint.Add(other[index]);
            other.RemoveAt(index);
        }

        if (stuffToHint.Count == 0)
        {
            TextIDToDisplayText[textId1] = "You've already explored everything here.";
            TextIDToDisplayText[textId2] = "I have no hints to give you.";
            return;
        }

        // Some AP item/location names contain brackets, and Unity's rich text uses angle brackets for markup, which can break hint display.
        // Unity does not appear to support &lt;/&gt; or \ escaping or <</>> escaping. Various Unicode angle brackets fail to render at all. Even adding spaces doesn't work.
        // So with no viable workaround to actually display an angle bracket safely, we just have to delete them, as that's less harmful than all the hint text getting deleted.
        Func<string, string> removeOpeningAngleBrackets = (text) => text.Replace("<", "");

        var locationToScout = stuffToHint[0].Key;
        var itemName = stuffToHint[0].Value.ItemName;
        var locationName = LocationNames.locationNames[locationToScout];
        TextIDToDisplayText[textId1] = removeOpeningAngleBrackets($"Ignoring everywhere you've already been, the best item I know of is '{itemName}' at '{locationName}'.");

        var scoutHintedLocationTask = Task.Run(() => APRandomizer.APSession.Locations.ScoutLocationsAsync(true, [LocationNames.locationToArchipelagoId[locationToScout]]));
        if (!scoutHintedLocationTask.Wait(TimeSpan.FromSeconds(2)))
        {
            var msg = $"AP server timed out when we tried to tell it about your hint for location '{LocationNames.locationNames[stuffToHint[0].Key]}'. Did the connection go down?";
            APRandomizer.OWMLModConsole.WriteLine(msg, OWML.Common.MessageType.Warning);
            APRandomizer.InGameAPConsole.AddText($"<color='orange'>{msg}</color>");
        }

        if (stuffToHint.Count == 1)
        {
            TextIDToDisplayText[textId2] = "You've already explored everything else here.";
            return;
        }

        var flags = stuffToHint[1].Value.Flags;
        if (!flags.HasFlag(ItemFlags.Advancement) && !flags.HasFlag(ItemFlags.NeverExclude))
            TextIDToDisplayText[textId2] = $"There's nothing else of value around here.";
        else
        {
            var adjective = flags.HasFlag(ItemFlags.Advancement) ? "important" : "useful";
            var locationName2 = LocationNames.locationNames[stuffToHint[1].Key];
            TextIDToDisplayText[textId2] = removeOpeningAngleBrackets($"There's also something {adjective} at '{locationName2}', but I'm not sure what.");
        }

        if (APRandomizer.SaveData.hintsGenerated == null) APRandomizer.SaveData.hintsGenerated = new();
        APRandomizer.SaveData.hintsGenerated[character] = [TextIDToDisplayText[textId1], TextIDToDisplayText[textId2]];
        APRandomizer.WriteToSaveFile();
    }

    [HarmonyPostfix, HarmonyPatch(typeof(CharacterDialogueTree), nameof(CharacterDialogueTree.StartConversation))]
    public static void CharacterDialogueTree_StartConversation_Postfix(CharacterDialogueTree __instance)
    {
        if (LoadManager.GetCurrentScene() == OWScene.EyeOfTheUniverse) return;

        //APRandomizer.OWMLModConsole.WriteLine($"CharacterDialogueTree_StartConversation_Postfix {__instance._characterName}");
        if (!characterToQuestionNodes.ContainsKey(__instance._characterName))
            return; // not a character that provides hints

        foreach (var questionsNodeName in characterToQuestionNodes[__instance._characterName])
        {
            var listDialogueOptions = __instance._mapDialogueNodes[questionsNodeName].ListDialogueOptions;

            if (listDialogueOptions.Any() && listDialogueOptions[0]._textID == HintOptionTextId)
                return; // we've already added the hint option to this character

            var hintNodeName = $"APRandomizer_{__instance._characterName}_HintsNode";
            APRandomizer.OWMLModConsole.WriteLine($"CharacterDialogueTree_StartConversation_Postfix creating DialogueNode with name {hintNodeName}");

            var hintOption = new DialogueOption();
            hintOption._textID = HintOptionTextId;
            hintOption._targetName = hintNodeName;
            listDialogueOptions.Insert(0, hintOption);

            List<DialogueText.TextBlock> textBlocks = [new DialogueText.TextBlock(["_TextPage1", "_TextPage2"], "")];
            var hintText = new DialogueText([], false);
            hintText._listTextBlocks = textBlocks;

            var hintNode1 = new DialogueNode();
            hintNode1.Name = hintNodeName;
            hintNode1.DisplayTextData = hintText;

            __instance._mapDialogueNodes[hintNodeName] = hintNode1;
        }
    }

    // useful for testing
    /*[HarmonyPrefix, HarmonyPatch(typeof(CharacterDialogueTree), nameof(CharacterDialogueTree.ContinueToNextNode), [])]
    public static void CharacterDialogueTree_ContinueToNextNode(CharacterDialogueTree __instance)
    {
        APRandomizer.OWMLModConsole.WriteLine($"CharacterDialogueTree_ContinueToNextNode moving from {__instance._currentNode.Name} to {__instance._currentNode.Target?.Name}");
    }*/
}
