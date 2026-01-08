using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ArchipelagoRandomizer.ItemImpls.FCProgression
{
    [HarmonyPatch]
    class ThermalInsulation
    {
        public static bool _hasThermalInsulation = false;
        public static bool hasThermalInsulation
        {
            get => _hasThermalInsulation;
            set
            {
                _hasThermalInsulation = value;

                if (_hasThermalInsulation)
                {
                    var nd = new NotificationData(NotificationTarget.Player, "SPACESUIT THERMAL INSULATION AUGMENTED TO WITHSTAND EXTREME TEMPERATURES.", 10);
                    NotificationManager.SharedInstance.PostNotification(nd, false);
                    if (APRandomizer.NewHorizonsAPI == null) return;
                    if (APRandomizer.NewHorizonsAPI.GetCurrentStarSystem() != "DeepBramble") return;

                    GameObject.Find("MagmasRecursion_Body/Sector/MoltenCore/DestructionVolume").transform.localScale = new Vector3(1, 1, 1);
                }
            }
        }
        public static void OnDeepBrambleLoadEvent()
        {
            if (APRandomizer.NewHorizonsAPI == null) return;
            if (APRandomizer.NewHorizonsAPI.GetCurrentStarSystem() != "DeepBramble") return;

            if (!hasThermalInsulation)
                GameObject.Find("MagmasRecursion_Body/Sector/MoltenCore/DestructionVolume").transform.localScale = new Vector3(4, 4, 4);
        }

        static string activeText = "Thermal Insulation: <color=green>Sufficient</color>";
        static string inactiveText = "Thermal Insulation: <color=red>Insufficient</color>";
        static ScreenPrompt thermalInsulationPrompt = new(activeText, 0);

        public static void UpdatePromptText()
        {
            if (hasThermalInsulation)
                thermalInsulationPrompt.SetText(activeText);
            else
                thermalInsulationPrompt.SetText(inactiveText);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.LateInitialize))]
        public static void ToolModeUI_LateInitialize_Postfix()
        {
            Locator.GetPromptManager().AddScreenPrompt(thermalInsulationPrompt, PromptPosition.UpperRight, false);
        }
        [HarmonyPostfix, HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.Update))]
        public static void ToolModeUI_Update_Postfix()
        {
            UpdatePromptText();
            thermalInsulationPrompt.SetVisibility(
                (OWInput.IsInputMode(InputMode.Character) || OWInput.IsInputMode(InputMode.ShipCockpit)) &&
                (
                    Locator.GetPlayerSectorDetector().IsWithinSector("magamas_recursion_fc")
                )
            );
        }
    }
}
