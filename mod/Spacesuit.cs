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
        var ship = Locator.GetShipBody()?.gameObject?.transform;
        if (ship != null)
        {
            var hangingSuitModel = ship.Find("Module_Supplies/Systems_Supplies/ExpeditionGear/EquipmentGeo/Props_HEA_PlayerSuit_Hanging")?.gameObject;
            hangingSuitModel.SetActive(hasSpacesuit);

            var hangingSuitIR = ship.Find("Module_Supplies/Systems_Supplies/ExpeditionGear/InteractVolume")?.GetComponent<MultiInteractReceiver>();
            hangingSuitIR.EnableSingleInteraction(hasSpacesuit, 0);
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(ShipPromptController), nameof(ShipPromptController.LateInitialize))]
    public static void ShipPromptController_LateInitialize_Postfix(ShipPromptController __instance)
    {
        APRandomizer.OWMLWriteLine($"ShipPromptController_LateInitialize_Postfix applying hasSpacesuit flag", OWML.Common.MessageType.Debug);
        ApplyHasSpacesuitFlag(_hasSpacesuit);
    }
}
