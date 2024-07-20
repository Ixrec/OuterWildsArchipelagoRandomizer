using HarmonyLib;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class Spacesuit
{
    private static bool _hasSpacesuit = false;

    public static bool hasSpacesuit
    {
        get => _hasSpacesuit;
        set
        {
            if (_hasSpacesuit != value)
            {
                _hasSpacesuit = value;
                ApplyHasSpacesuitFlag(_hasSpacesuit);
            }
        }
    }

    private static void ApplyHasSpacesuitFlag(bool hasSpacesuit)
    {
        if (!PlayerState.IsWearingSuit())
            SetSpacesuitVisible(hasSpacesuit);

        var ship = Locator.GetShipBody()?.gameObject?.transform;
        if (ship != null)
        {
            var hangingSuitIR = ship.Find("Module_Supplies/Systems_Supplies/ExpeditionGear/InteractVolume")?.GetComponent<MultiInteractReceiver>();
            hangingSuitIR.EnableSingleInteraction(hasSpacesuit, 0);
        }
    }

    // This is public so that it can also be called from Spawn.cs when we spawn already in our spacesuit
    public static void SetSpacesuitVisible(bool spacesuitVisible)
    {
        APRandomizer.OWMLModConsole.WriteLine($"SetSpacesuitVisible({spacesuitVisible}) called");
        var ship = Locator.GetShipBody()?.gameObject?.transform;
        if (ship != null)
        {
            var hangingSuitModel = ship.Find("Module_Supplies/Systems_Supplies/ExpeditionGear/EquipmentGeo/Props_HEA_PlayerSuit_Hanging")?.gameObject;
            hangingSuitModel.SetActive(spacesuitVisible);

            // the scout launcher model lying on the floor of the ship counts as part of the spacesuit,
            // because we always want it to be shown or hidden whenever the suit is
            var scoutLauncherOnFloorModel = ship.Find("Module_Supplies/Systems_Supplies/ExpeditionGear/EquipmentGeo/Props_HEA_ProbeLauncher")?.gameObject;
            scoutLauncherOnFloorModel.SetActive(spacesuitVisible);
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(ShipPromptController), nameof(ShipPromptController.LateInitialize))]
    public static void ShipPromptController_LateInitialize_Postfix(ShipPromptController __instance)
    {
        ApplyHasSpacesuitFlag(_hasSpacesuit);
    }
}
