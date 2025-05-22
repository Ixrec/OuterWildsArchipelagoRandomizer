using HarmonyLib;
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
            if (LoadManager.GetCurrentScene() != OWScene.SolarSystem && LoadManager.GetCurrentScene() != OWScene.EyeOfTheUniverse)
                return;

            if (corruptionComponent != null || !PlayerState.IsWearingSuit())
                return;

            corruptionComponent = Locator.GetPlayerTransform().gameObject.AddComponent<HUDCorruptionComponent>();
        }

        private static HUDHelmetAnimator helmetAnimator = null;

        [HarmonyPrefix, HarmonyPatch(typeof(HUDHelmetAnimator), nameof(HUDHelmetAnimator.Awake))]
        public static void HUDHelmetAnimator_Awake(HUDHelmetAnimator __instance) => helmetAnimator = __instance;

        class HUDCorruptionComponent : MonoBehaviour
        {
            float corruptionDuration = 10.0f;

            void Start()
            {
                APRandomizer.OWMLModConsole.WriteLine("Corrupting HUD");
                var nd = new NotificationData(NotificationTarget.Player, "HUD MALFUNCTION. RECALIBRATING.", corruptionDuration);
                NotificationManager.SharedInstance.PostNotification(nd);
            }

            void Update()
            {
                if (corruptionDuration > 0)
                {
                    helmetAnimator._hudDamageWobble = Mathf.PerlinNoise(Time.timeSinceLevelLoad, 0f) * 0.3f;
                    helmetAnimator._hudTimer = Mathf.PerlinNoise(Time.timeSinceLevelLoad, 1f);
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
