using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ArchipelagoRandomizer.ItemImpls.FCProgression
{
    class RandomizeFollyLevers
    {
        public static void OnDeepBrambleLoadEvent()
        {
            leversRandomized = false;
        }

        private static bool leversRandomized = false;

        [HarmonyPostfix, HarmonyPatch(typeof(PlayerSectorDetector), nameof(PlayerSectorDetector.OnAddSector))]
        public static void PlayerSectorDetector_OnAddSector(PlayerSectorDetector __instance, Sector sector)
        {
            if (sector._idString != "Graviton's Folly" || leversRandomized) return;

            APRandomizer.OWMLModConsole.WriteLine("Randomizing Folly levers");

            MD5 hasher = MD5.Create(); // The room seed is a string, so we hash it to get our seed
            System.Random prng = new System.Random(BitConverter.ToInt32(hasher.ComputeHash(Encoding.UTF8.GetBytes(APRandomizer.APSession.RoomState.Seed)), 0) + APRandomizer.APSession.ConnectionInfo.Slot);

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
