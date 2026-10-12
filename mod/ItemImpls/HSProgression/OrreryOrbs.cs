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

                    ShatteredGeodeOrb.enabled = true;
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

                    TheBigOneOrb.enabled = true;
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

                    DaucusOrb.enabled = true;
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

                    GlacialAbyssOrb.enabled = true;
                }
            }
        }

        private static NomaiInterfaceOrb ShatteredGeodeOrb;
        private static NomaiInterfaceOrb TheBigOneOrb;
        private static NomaiInterfaceOrb DaucusOrb;
        private static NomaiInterfaceOrb GlacialAbyssOrb;

        public static void OnJam5LoadEvent()
        {
            ShatteredGeodeOrb = GameObject.Find("Walker_Jam5_Station_Body/Sector/OrreryStation/Orrery/OrbRoot/Prefab_NOM_InterfaceOrb").GetComponent<NomaiInterfaceOrb>();
            TheBigOneOrb = GameObject.Find("Walker_Jam5_Station_Body/Sector/OrreryStation/Orrery/OrbRoot/Prefab_NOM_InterfaceOrb (1)").GetComponent<NomaiInterfaceOrb>();
            DaucusOrb = GameObject.Find("Walker_Jam5_Station_Body/Sector/OrreryStation/Orrery/OrbRoot/Prefab_NOM_InterfaceOrb (2)").GetComponent<NomaiInterfaceOrb>();
            GlacialAbyssOrb = GameObject.Find("Walker_Jam5_Station_Body/Sector/OrreryStation/Orrery/OrbRoot/Prefab_NOM_InterfaceOrb (3)").GetComponent<NomaiInterfaceOrb>();

            ShatteredGeodeOrb.enabled = HasShatteredGeodeOrb;
            TheBigOneOrb.enabled = HasTheBigOneOrb;
            DaucusOrb.enabled = HasDaucusOrb;
            GlacialAbyssOrb.enabled = HasGlacialAbyssOrb;
        }
    }
}
