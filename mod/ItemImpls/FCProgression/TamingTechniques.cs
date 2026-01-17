using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ArchipelagoRandomizer.ItemImpls.FCProgression
{
    class TamingTechniques
    {
        public static bool _hasTamingTechniques = false;
        public static bool HasTamingTechniques
        {
            get => _hasTamingTechniques;
            set
            {
                _hasTamingTechniques = value;

                if (_hasTamingTechniques)
                {
                    if (APRandomizer.NewHorizonsAPI == null) return;
                    if (APRandomizer.NewHorizonsAPI.GetCurrentStarSystem() != "DeepBramble") return;

                    GameObject.Find("BrightHollow_Body/Sector/observation_lab/fish/domestic_fish/Beast_Anglerfish/B_angler_root/B_angler_body01/B_angler_body02/eye_interacts").SetActive(true);
                    GameObject.Find("BrightHollow_Body/Sector/observation_lab/fish/domestic_fish (1)/Beast_Anglerfish/B_angler_root/B_angler_body01/B_angler_body02/eye_interacts").SetActive(true);
                    GameObject.Find("BrightHollow_Body/Sector/observation_lab/fish/domestic_fish (2)/Beast_Anglerfish/B_angler_root/B_angler_body01/B_angler_body02/eye_interacts").SetActive(true);
                    GameObject.Find("TheNursery_Body/Sector/nursery_tube/kevin/Beast_Anglerfish/B_angler_root/B_angler_body01/B_angler_body02/eye_triggers").SetActive(true);
                }
            }
        }
        public static void OnDeepBrambleLoadEvent()
        {
            if (APRandomizer.NewHorizonsAPI == null) return;
            if (APRandomizer.NewHorizonsAPI.GetCurrentStarSystem() != "DeepBramble") return;

            APRandomizer.Instance.StartCoroutine(DisableAnglerEyes());
        }

        private static IEnumerator DisableAnglerEyes()
        {
            // If we do this too quickly the triggers have issues when re-enabled
            yield return new WaitForSeconds(1f);

            // In case the player received the item within the past second, we check again
            if (!HasTamingTechniques)
            {
                // Disable petting anglerfish eyes in Bright Hollow
                GameObject.Find("BrightHollow_Body/Sector/observation_lab/fish/domestic_fish/Beast_Anglerfish/B_angler_root/B_angler_body01/B_angler_body02/eye_interacts").SetActive(false);
                GameObject.Find("BrightHollow_Body/Sector/observation_lab/fish/domestic_fish (1)/Beast_Anglerfish/B_angler_root/B_angler_body01/B_angler_body02/eye_interacts").SetActive(false);
                GameObject.Find("BrightHollow_Body/Sector/observation_lab/fish/domestic_fish (2)/Beast_Anglerfish/B_angler_root/B_angler_body01/B_angler_body02/eye_interacts").SetActive(false);
                // Disable Kevin's eye trigger
                GameObject.Find("TheNursery_Body/Sector/nursery_tube/kevin/Beast_Anglerfish/B_angler_root/B_angler_body01/B_angler_body02/eye_triggers").SetActive(false);
            }
        }
    }
}
