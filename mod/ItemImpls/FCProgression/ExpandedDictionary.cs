using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ArchipelagoRandomizer.ItemImpls.FCProgression
{

    [HarmonyPatch]
    static class ExpandedDictionary
    {
        internal const string RenamedDreeTextName = "dre";

        private static readonly List<NomaiWallText> _deepBrambleTextWalls = [];

        private static bool _hasExpandedDictionary = false;

        public static bool hasExpandedDictionary
        {
            get => _hasExpandedDictionary;
            set
            {
                _hasExpandedDictionary = value;

                if (_hasExpandedDictionary)
                {
                    var nd = new NotificationData(NotificationTarget.Player, "RECONFIGURING TRANSLATOR TO INCLUDE DREE TRANSLATION DICTIONARY.", 10);
                    NotificationManager.SharedInstance.PostNotification(nd, false);
                }
            }
        }

        public static void OnCompleteSceneLoad() => _deepBrambleTextWalls.Clear(); // Clear the list before NH loads in the Deep Bramble dimension

        public static void OnDeepBrambleLoadEvent()
        {
            textChanged = false;
        }

        private static bool textChanged = false;

        [HarmonyPostfix, HarmonyPatch(typeof(PlayerSectorDetector), nameof(PlayerSectorDetector.OnAddSector))]
        public static void PlayerSectorDetector_OnAddSector(PlayerSectorDetector __instance, Sector sector)
        {
            if (sector._idString != "titans_tears_fc" || textChanged) return;

            foreach (NomaiWallText wall in _deepBrambleTextWalls)
            {
                if (!wall._initialized) continue;
                foreach (NomaiTextLine txt in wall._textLines)
                    if (txt._renderer.sharedMaterial.name.Contains("dree"))
                        txt._renderer.sharedMaterial.name = txt._renderer.sharedMaterial.name.Replace("dree", RenamedDreeTextName);
            }
            textChanged = true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(NomaiWallText), nameof(NomaiWallText.Awake))]
        public static void NomaiWallText_Awake(NomaiWallText __instance)
        {
            // We need to rename the material of Dree text to steal control of the translation from FC,
            // but the material isn't set at this point. So we make a list now and process them later.
            _deepBrambleTextWalls.Add(__instance);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(NomaiTranslatorProp), nameof(NomaiTranslatorProp.DisplayTextNode))]
        public static bool HideDreeText(NomaiTranslatorProp __instance)
        {
            if (APRandomizer.NewHorizonsAPI == null || APRandomizer.NewHorizonsAPI.GetCurrentStarSystem() != "DeepBramble")
                return true;

            bool isDreeText = __instance._scanBeams[0]._nomaiTextLine != null
                && __instance._scanBeams[0]._nomaiTextLine._renderer.sharedMaterial.name.Contains(RenamedDreeTextName);

            // If the text is dree, and the player lacks the upgrade, hide the text
            if (isDreeText && !hasExpandedDictionary)
            {
                __instance._textField.text = UITextLibrary.GetString(UITextType.TranslatorUntranslatableWarning);
                return false;
            }
            // Otherwise, run normally
            return true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(NomaiTranslatorProp), nameof(NomaiTranslatorProp.DisplayTextNode))]
        public static void ChangeDreeUnreadMessage(NomaiTranslatorProp __instance)
        {
            bool isDreeText = __instance._scanBeams[0]._nomaiTextLine != null
                && __instance._scanBeams[0]._nomaiTextLine._renderer.sharedMaterial.name.Contains(RenamedDreeTextName);

            if (isDreeText && hasExpandedDictionary && __instance._translationTimeElapsed == 0f && !__instance._nomaiTextComponent.IsTranslated(__instance._currentTextID))
                __instance._textField.text = "<!> Untranslated Dree writing <!>";
        }
    }
}
