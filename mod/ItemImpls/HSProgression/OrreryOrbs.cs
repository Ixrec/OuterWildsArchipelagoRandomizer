using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ArchipelagoRandomizer.ItemImpls.HSProgression
{
    class OrreryOrbs
    {
        private static bool _hasShatteredGeodeOrb = false;
        public static bool HasShatteredGeodeOrb
        {
            get => _hasShatteredGeodeOrb;
            set
            {
                _hasShatteredGeodeOrb = value;

                if (_hasShatteredGeodeOrb)
                {
                    if (APRandomizer.NewHorizonsAPI == null) return;
                    if (APRandomizer.NewHorizonsAPI.GetCurrentStarSystem() != "Jam5") return;
                    SetOrbActive(ShatteredGeodeOrb, true);
                }
            }
        }
        private static bool _hasTheBigOneOrb = false;
        public static bool HasTheBigOneOrb
        {
            get => _hasTheBigOneOrb;
            set
            {
                _hasTheBigOneOrb = value;

                if (_hasTheBigOneOrb)
                {
                    if (APRandomizer.NewHorizonsAPI == null) return;
                    if (APRandomizer.NewHorizonsAPI.GetCurrentStarSystem() != "Jam5") return;
                    SetOrbActive(TheBigOneOrb, true);
                }
            }
        }
        private static bool _hasDaucusOrb = false;
        public static bool HasDaucusOrb
        {
            get => _hasDaucusOrb;
            set
            {
                _hasDaucusOrb = value;

                if (_hasDaucusOrb)
                {
                    if (APRandomizer.NewHorizonsAPI == null) return;
                    if (APRandomizer.NewHorizonsAPI.GetCurrentStarSystem() != "Jam5") return;
                    SetOrbActive(DaucusOrb, true);
                }
            }
        }
        private static bool _hasGlacialAbyssOrb = false;
        public static bool HasGlacialAbyssOrb
        {
            get => _hasGlacialAbyssOrb;
            set
            {
                _hasGlacialAbyssOrb = value;

                if (_hasGlacialAbyssOrb)
                {
                    if (APRandomizer.NewHorizonsAPI == null) return;
                    if (APRandomizer.NewHorizonsAPI.GetCurrentStarSystem() != "Jam5") return;
                    SetOrbActive(GlacialAbyssOrb, true);
                }
            }
        }

        private static GameObject ShatteredGeodeOrb;
        private static GameObject TheBigOneOrb;
        private static GameObject DaucusOrb;
        private static GameObject GlacialAbyssOrb;

        public static void OnJam5LoadEvent()
        {
            ShatteredGeodeOrb = GameObject.Find("Walker_Jam5_Station_Body/Sector/OrreryStation/Orrery/OrbRoot/Prefab_NOM_InterfaceOrb");
            TheBigOneOrb = GameObject.Find("Walker_Jam5_Station_Body/Sector/OrreryStation/Orrery/OrbRoot/Prefab_NOM_InterfaceOrb (1)");
            DaucusOrb = GameObject.Find("Walker_Jam5_Station_Body/Sector/OrreryStation/Orrery/OrbRoot/Prefab_NOM_InterfaceOrb (2)");
            GlacialAbyssOrb = GameObject.Find("Walker_Jam5_Station_Body/Sector/OrreryStation/Orrery/OrbRoot/Prefab_NOM_InterfaceOrb (3)");

            SetOrbActive(ShatteredGeodeOrb, HasShatteredGeodeOrb);
            SetOrbActive(TheBigOneOrb, HasTheBigOneOrb);
            SetOrbActive(DaucusOrb, HasDaucusOrb);
            SetOrbActive(GlacialAbyssOrb, HasGlacialAbyssOrb);
        }

        private static void SetOrbActive(GameObject orb, bool isActive)
        {
            if (orb == null) return;
            orb.transform.Find("Props_NOM_Orb").gameObject.SetActive(isActive);
            orb.GetComponent<SphereCollider>().radius = isActive ? 0.65f : 0f;
        }
    }
}
