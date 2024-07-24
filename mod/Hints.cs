using Archipelago.MultiClient.Net.Enums;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;

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
        { "Feldspar", ["FeldsparPrimaryMenu"] },
    };

    private const string HintOptionTextId = "APRandomizer_HintOption";

    private const string Placeholder = "HINT_NOT_YET_GENERATED";

    private static Dictionary<string, string> TextIDToDisplayText = new Dictionary<string, string>
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

        var prefixes = characterToLocationPrefixes[character];
        var relevantLocations = LocationNames.locationNames
            .Where(kv => prefixes.Any(p => kv.Value.StartsWith(p)))
            .Select(kv => kv.Key);

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
            APRandomizer.OWMLModConsole.WriteLine($"CharacterDialogueTree_InputDialogueOption only found {relevantScouts.Count()} relevant scouts for {uncheckedRelevantLocations.Count()} uncheckedRelevantLocations, aborting hint generation", OWML.Common.MessageType.Error);
            return;
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

        TextIDToDisplayText[textId1] = $"The best item I know of is '{stuffToHint[0].Value.ItemName}' at '{LocationNames.locationNames[stuffToHint[0].Key]}'.";
        APRandomizer.APSession.Locations.ScoutLocationsAsync(true, [LocationNames.locationToArchipelagoId[stuffToHint[0].Key]]);

        var flags = stuffToHint[1].Value.Flags;
        if (!flags.HasFlag(ItemFlags.Advancement) && !flags.HasFlag(ItemFlags.NeverExclude))
            TextIDToDisplayText[textId2] = $"There's nothing else of value around here.";
        else
        {
            var adjective = flags.HasFlag(ItemFlags.Advancement) ? "important" : "useful";
            TextIDToDisplayText[textId2] = $"There's also something {adjective} at '{LocationNames.locationNames[stuffToHint[1].Key]}', but I'm not sure what.";
        }

        if (APRandomizer.SaveData.hintsGenerated == null) APRandomizer.SaveData.hintsGenerated = new();
        APRandomizer.SaveData.hintsGenerated[character] = [TextIDToDisplayText[textId1], TextIDToDisplayText[textId2]];
        APRandomizer.WriteToSaveFile();
    }

    [HarmonyPostfix, HarmonyPatch(typeof(CharacterDialogueTree), nameof(CharacterDialogueTree.StartConversation))]
    public static void CharacterDialogueTree_StartConversation_Postfix(CharacterDialogueTree __instance)
    {
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
}
