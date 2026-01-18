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

        internal static void ApplyIcePhysics()
        {
            //we're in credits or menus. Ignore.
            if (LoadManager.GetCurrentScene() != OWScene.SolarSystem && LoadManager.GetCurrentScene() != OWScene.EyeOfTheUniverse)
                return;

            // ice physics are not manageable without the suit's jetpack, so if the player hasn't gotten their suit yet,
            // tell them we're ignoring the trap on purpose
            if (!PlayerState.IsWearingSuit())
            {
                APRandomizer.InGameAPConsole.AddText($"Ignoring ice physics trap since you aren't wearing the suit yet.");
                return;
            }

            //this call is necessary, otherwise the player is stuck in place and has to jump to have ice physics applied
            characterController.MakeUngrounded();
            icePhysicsApplied = true;
        }

        private static bool icePhysicsApplied;

        private static PlayerCharacterController characterController = null;

        [HarmonyPrefix, HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.Awake))]
        private static void PlayerCharacterController_Awake(PlayerCharacterController __instance)
        {
            characterController = __instance;
            icePhysicsApplied = false; //resetting this to avoid ice physics carrying over in the next loop
        }

        //CastForGrounded sets the collider and surface type the player is standing on.
        //using a postfix to override that if we have ice physics applied for the duration of the loop
        [HarmonyPostfix, HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.CastForGrounded))]
        private static void PlayerCharacterController_CastForGrounded_Postfix()
        {
            if (!icePhysicsApplied)
                return;

            // only do ice physics in the "real" world, it's not manageable in the suitless dreamworld
            if (PlayerState.InDreamWorld())
                return;

            characterController._groundCollider.material.dynamicFriction = 0.0f;
            characterController._groundSurface = SurfaceType.Ice;
        }
    }
}
