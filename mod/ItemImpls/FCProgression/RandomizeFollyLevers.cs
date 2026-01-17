using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ArchipelagoRandomizer.ItemImpls.FCProgression
{
    class RandomizeFollyLevers
    {
        private static readonly System.Random prng = new();
        public static void OnDeepBrambleLoadEvent()
        {
            if (APRandomizer.NewHorizonsAPI == null || APRandomizer.NewHorizonsAPI.GetCurrentStarSystem() != "DeepBramble")
                return;

            APRandomizer.Instance.StartCoroutine(RandomizeLevers());
        }

        private static IEnumerator RandomizeLevers()
        {
            // If we do this too quickly the triggers have issues when re-enabled
            yield return new WaitForSeconds(1f);

            APRandomizer.OWMLModConsole.WriteLine("Randomizing Folly levers");

            FieldInfo beamField = Type.GetType("DeepBramble.MiscBehaviours.Lever, DeepBramble", true).GetField("beamObject", BindingFlags.NonPublic | BindingFlags.Instance);
            List<object> levers = [
                GameObject.Find("GravitonsFolly_Body/Sector/hollowplanet/planet/crystal_core/beams/levers/lever1").GetComponent("Lever"),
                GameObject.Find("GravitonsFolly_Body/Sector/hollowplanet/planet/crystal_core/beams/levers/lever2").GetComponent("Lever"),
                GameObject.Find("GravitonsFolly_Body/Sector/hollowplanet/planet/crystal_core/beams/levers/lever3").GetComponent("Lever"),
                GameObject.Find("GravitonsFolly_Body/Sector/hollowplanet/planet/crystal_core/beams/levers/lever4").GetComponent("Lever"),
                GameObject.Find("GravitonsFolly_Body/Sector/hollowplanet/planet/crystal_core/beams/levers/lever5").GetComponent("Lever"),
                GameObject.Find("GravitonsFolly_Body/Sector/hollowplanet/planet/crystal_core/beams/levers/lever6").GetComponent("Lever"),
            ];
            List<(object, int)> beams = [.. levers.Select((l, i) => (beamField.GetValue(l), i + 1)).Cast<(object, int)>().OrderBy(_ => prng.Next())];

            for (int i = 0; i < levers.Count; i++)
                beamField.SetValue(levers[i], beams[i].Item1);

            // Figure out lever is which
            int second = beams.FindIndex(t => t.Item2 == 2);
            int secondToLast = beams.FindIndex(t => t.Item2 == 5);

            static string IndexToString(int i) => i switch
            {
                0 => "first",
                1 => "second",
                2 => "third",
                3 => "fourth",
                4 => "second-to-last",
                5 => "last",
                _ => "ERROR"
            };

            var lever1 = IndexToString(second);
            var lever2 = IndexToString(secondToLast);

            // Put the text in a logical order
            if (second > secondToLast)
                (lever2, lever1) = (lever1, lever2);

            // Update the hint text
            NomaiWallText comboHintText = GameObject.Find("GravitonsFolly_Body/Sector/hollowplanet/planet/crystal_core/crystal_lab/Props_NOM_Whiteboard_Shared/combo_hint_text").GetComponent<NomaiWallText>();
            comboHintText._dictNomaiTextData[2].TextNode.InnerText = comboHintText._dictNomaiTextData[2].TextNode.InnerText
                .Replace("except for the second and the second-to-last", $"except for the {lever1} and the {lever2}");
        }
    }
}
