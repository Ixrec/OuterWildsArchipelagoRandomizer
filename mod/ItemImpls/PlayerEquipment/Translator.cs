using HarmonyLib;
using System.Collections.Generic;
using System.Linq;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class Translator
{
    private static bool _splitTranslator = false;
    public static bool splitTranslator
    {
        get => _splitTranslator;
        set
        {
            if (_splitTranslator != value)
                _splitTranslator = value;

            if (!_splitTranslator && cannotTranslatePrompt != null)
                cannotTranslatePrompt.SetText("Translator Not Available");
        }
    }

    private enum TranslatorSector
    {
        HourglassTwins,
        TimberHearth,
        BrittleHollow,
        GiantsDeep,
        DarkBramble,
        Other
    }

    public static bool hasRegularTranslator = false;
    public static bool hasHGTTranslator = false;
    public static bool hasTHTranslator = false;
    public static bool hasBHTranslator = false;
    public static bool hasGDTranslator = false;
    public static bool hasDBTranslator = false;
    public static bool hasOtherTranslator = false;

    private static TranslatorSector currentTranslatorSector = TranslatorSector.Other;

    private static TranslatorSector GetTranslatorSector(List<Sector> sectorList)
    {
        // For the most part, the translator sector is whichever of the 5 major planet sectors we're in, or Other otherwise.
        // The exceptions are all Unnamed mini-sectors that can change which if any planet they're on, so
        // we need to check for these explicitly and make sure they take precedence over major planet.
        foreach (var sector in sectorList)
        {
            // This is BH's Tower of Quantum Knowledge, because you're expected to translate it after it moves from BH to WH(S).
            if (sector.GetName() == Sector.Name.Unnamed && sector.gameObject.name == "Sector_QuantumFragment")
                return TranslatorSector.BrittleHollow;

            // This covers both of the Nomai shuttles, since both are normally in "Other" (Interloper and Quantum Moon)
            // and for logic simplicity we want the same translator to work on them if recalled to the ET/BH cannons.
            if (sector.GetName() == Sector.Name.Unnamed && sector.gameObject.name == "Sector_NomaiShuttleInterior")
                return TranslatorSector.Other;
        }

        // Now that the exceptions are handled, and the major planets never overlap, we only need one more loop
        foreach (var sector in sectorList)
        {
            if (sector.GetName() == Sector.Name.HourglassTwins)
                return TranslatorSector.HourglassTwins;
            if (sector.GetName() == Sector.Name.TimberHearth)
                return TranslatorSector.TimberHearth;
            if (sector.GetName() == Sector.Name.BrittleHollow)
                return TranslatorSector.BrittleHollow;
            if (sector.GetName() == Sector.Name.GiantsDeep)
                return TranslatorSector.GiantsDeep;
            if (sector.GetName() == Sector.Name.DarkBramble)
                return TranslatorSector.DarkBramble;
            if (sector.GetName() == Sector.Name.BrambleDimension) // the "interior" of DB is not in the DB sector
                return TranslatorSector.DarkBramble;
            if (sector.GetName() == Sector.Name.VesselDimension) // for some reason the Vessel node is not counted in BrambleDimension
                return TranslatorSector.DarkBramble;
        }

        return TranslatorSector.Other;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(PlayerSectorDetector), nameof(PlayerSectorDetector.OnAddSector))]
    public static void PlayerSectorDetector_OnAddSector(PlayerSectorDetector __instance) => PlayerSectorsChanged(__instance);
    [HarmonyPostfix, HarmonyPatch(typeof(PlayerSectorDetector), nameof(PlayerSectorDetector.OnRemoveSector))]
    public static void PlayerSectorDetector_OnRemoveSector(PlayerSectorDetector __instance) => PlayerSectorsChanged(__instance);

    public static void PlayerSectorsChanged(PlayerSectorDetector __instance)
    {
        //var sectorNames = __instance._sectorList.Select(s => $"{s.GetName()} ({s.GetIDString()} / {s.name})");
        //APRandomizer.OWMLModConsole.WriteLine($"PlayerSectorsChanged {string.Join(", ", sectorNames)}");

        if (!splitTranslator) return;

        currentTranslatorSector = GetTranslatorSector(__instance._sectorList);

        switch (currentTranslatorSector)
        {
            case TranslatorSector.HourglassTwins: cannotTranslatePromptText = "Translator (Hourglass Twins) Not Available"; break;
            case TranslatorSector.TimberHearth: cannotTranslatePromptText = "Translator (Timber Hearth) Not Available"; break;
            case TranslatorSector.BrittleHollow: cannotTranslatePromptText = "Translator (Brittle Hollow) Not Available"; break;
            case TranslatorSector.GiantsDeep: cannotTranslatePromptText = "Translator (Giant's Deep) Not Available"; break;
            case TranslatorSector.DarkBramble: cannotTranslatePromptText = "Translator (Dark Bramble) Not Available"; break;
            case TranslatorSector.Other: cannotTranslatePromptText = "Translator (Other) Not Available"; break;
        }
    }

    private static bool hasTranslatorForCurrentSector()
    {
        if (!splitTranslator)
            return hasRegularTranslator;

        switch (currentTranslatorSector)
        {
            case TranslatorSector.HourglassTwins: return hasHGTTranslator;
            case TranslatorSector.TimberHearth: return hasTHTranslator;
            case TranslatorSector.BrittleHollow: return hasBHTranslator;
            case TranslatorSector.GiantsDeep: return hasGDTranslator;
            case TranslatorSector.DarkBramble: return hasDBTranslator;
            case TranslatorSector.Other: return hasOtherTranslator;
            default: throw new System.ArgumentException($"Invalid translator sector: {currentTranslatorSector}");
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(ToolModeSwapper), nameof(ToolModeSwapper.EquipToolMode))]
    public static bool ToolModeSwapper_EquipToolMode_Prefix(ToolMode mode)
    {
        if (mode == ToolMode.Translator && !hasTranslatorForCurrentSector())
            return false;
        return true;
    }

    static ScreenPrompt translatePrompt = null;

    // For some reason calling prompt.SetText() during Add/RemoveSector() often throws NREs, so
    // we have to store the text separately and wait to call .SetText() until the next Update().
    static ScreenPrompt cannotTranslatePrompt = null;
    static string cannotTranslatePromptText = null;

    [HarmonyPostfix, HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.Start))]
    public static void ToolModeUI_Start_Postfix(ToolModeUI __instance)
    {
        translatePrompt = __instance._centerTranslatePrompt;

        cannotTranslatePromptText = "Translator Not Available";
        cannotTranslatePrompt = new ScreenPrompt(cannotTranslatePromptText, 0);
        Locator.GetPromptManager().AddScreenPrompt(cannotTranslatePrompt, PromptPosition.Center, false);
    }

    // Because of the auto-equip translator setting, the enabling/disabling of the translate
    // prompt is a little more complex than usual and gets evaluated in Update().
    [HarmonyPostfix, HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.Update))]
    public static void ToolModeUI_Update_Postfix(ToolModeUI __instance)
    {
        cannotTranslatePrompt.SetVisibility(false);

        // If the vanilla translate prompt is already being displayed, then this is like every other file
        // that needs to change a UI prompt: just swap to the other prompt if we don't have the item yet.
        if (translatePrompt.IsVisible())
        {
            if (!hasTranslatorForCurrentSector())
            {
                translatePrompt.SetVisibility(false);
                cannotTranslatePrompt.SetVisibility(true);
                if (cannotTranslatePrompt.GetText() != cannotTranslatePromptText)
                    cannotTranslatePrompt.SetText(cannotTranslatePromptText);
            }
            return;
        }

        // But if the vanilla game would've auto-equipped the translator, then we need to prevent that
        // to show our "Translator Not Available" prompt instead.
        // This is mostly a copy-paste of the body of ToolModeSwapper.IsTranslatorEquipPromptAllowed(),
        // but with our hasTranslator flag inserted in the right place.
        if (
            __instance._toolSwapper.IsNomaiTextInFocus() &&
            __instance._toolSwapper._currentToolMode != ToolMode.Translator &&
            (!hasTranslatorForCurrentSector() || !__instance._toolSwapper.GetAutoEquipTranslator() || __instance._toolSwapper._waitForLoseNomaiTextFocus) &&
            OWInput.IsInputMode(InputMode.Character)
        )
        {
            translatePrompt.SetVisibility(hasTranslatorForCurrentSector());
            cannotTranslatePrompt.SetVisibility(!hasTranslatorForCurrentSector());
            if (cannotTranslatePrompt.GetText() != cannotTranslatePromptText)
                cannotTranslatePrompt.SetText(cannotTranslatePromptText);
        }
    }
}
