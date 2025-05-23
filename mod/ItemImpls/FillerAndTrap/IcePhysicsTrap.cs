using HarmonyLib;
namespace ArchipelagoRandomizer
{
    [HarmonyPatch]
    internal class IcePhysicsTrap
    {
        private static uint _icePhysicsTraps;

        public static uint icePhysicsTraps
        {
            get => _icePhysicsTraps;
            set
            {
                if (value > _icePhysicsTraps)
                {
                    ApplyIcePhysics();
                }
                _icePhysicsTraps = value;
            }
        }

        private static void ApplyIcePhysics()
        {
            if (LoadManager.GetCurrentScene() != OWScene.SolarSystem && LoadManager.GetCurrentScene() != OWScene.EyeOfTheUniverse)
                return;

            characterController.MakeUngrounded();
            icePhysicsApplied = true;
        }

        private static bool icePhysicsApplied;

        private static PlayerCharacterController characterController = null;

        [HarmonyPrefix, HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Awake))]
        private static void PlayerCharacterController_Awake(PlayerCharacterController __instance)
        {
            characterController = __instance;
            icePhysicsApplied = false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.CastForGrounded))]
        private static void PlayerCharacterController_CastForGrounded_Postfix()
        {
            if (!icePhysicsApplied)
                return;

            characterController._groundCollider.material.dynamicFriction = 0.0f;
            characterController._groundSurface = SurfaceType.Ice;
        }
    }
}
