﻿using HarmonyLib;
using UnityEngine;

namespace ArchipelagoRandomizer
{
    [HarmonyPatch]
    internal class HUDCorruptionTrap
    {
        private static uint _hudCorruptionTraps;

        public static uint hudCorruptionTraps
        {
            get => _hudCorruptionTraps;
            set
            {
                if (value > _hudCorruptionTraps)
                {
                    CorruptPlayerHUD();
                }
                _hudCorruptionTraps = value;
            }
        }

        private static HUDCorruptionComponent corruptionComponent = null;

        static void CorruptPlayerHUD()
        {
            //ignore if we're on menus or in credits
            if (LoadManager.GetCurrentScene() != OWScene.SolarSystem && LoadManager.GetCurrentScene() != OWScene.EyeOfTheUniverse)
                return;

            //no sense in corrupting a HUD the player can't see. Ignoring further corruptions if one is already active as well
            if (corruptionComponent != null || !PlayerState.IsWearingSuit())
                return;

            corruptionComponent = Locator.GetPlayerTransform().gameObject.AddComponent<HUDCorruptionComponent>();
        }

        //HUD Helmet animator is the component responsible for how the HUD animates (aberration, flickering, etc)
        private static HUDHelmetAnimator helmetAnimator = null;

        [HarmonyPrefix, HarmonyPatch(typeof(HUDHelmetAnimator), nameof(HUDHelmetAnimator.Awake))]
        public static void HUDHelmetAnimator_Awake(HUDHelmetAnimator __instance) => helmetAnimator = __instance;

        class HUDCorruptionComponent : MonoBehaviour
        {
            //length of the HUD corruption, in seconds
            float corruptionDuration = 10.0f;

            void Start()
            {
                var nd = new NotificationData(NotificationTarget.Player, "HUD MALFUNCTION. RECALIBRATING.", corruptionDuration);
                NotificationManager.SharedInstance.PostNotification(nd);
            }

            void Update()
            {
                if (corruptionDuration > 0)
                {
                    helmetAnimator._hudDamageWobble = Mathf.PerlinNoise(Time.timeSinceLevelLoad, 0f) * 0.3f; //responsible for the chromatic aberration. Higher value = more erratic
                    helmetAnimator._hudTimer = Mathf.PerlinNoise(Time.timeSinceLevelLoad, 1f); //responsible for the flickering. Fluctuates randomly between 0 and 1
                    corruptionDuration -= Time.deltaTime;
                }
                else
                {
                    gameObject.DestroyAllComponents<HUDCorruptionComponent>();
                }
            }
        }
    }
}
