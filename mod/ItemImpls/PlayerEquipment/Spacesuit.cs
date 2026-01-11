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
            var spv = ship.Find("Module_Supplies/Systems_Supplies/ExpeditionGear").GetComponent<SuitPickupVolume>();
            // Only enable/disable the Suit Up / Return Suit prompt. We want Preflight Checklist to work regardless.
            spv._interactVolume.EnableSingleInteraction(hasSpacesuit, spv._pickupSuitCommandIndex);
        }
    }

    // This is public so that it can also be called from Spawn.cs when we spawn already in our spacesuit
    public static void SetSpacesuitVisible(bool spacesuitVisible)
    {
        //APRandomizer.OWMLModConsole.WriteLine($"SetSpacesuitVisible({spacesuitVisible}) called");
        var ship = Locator.GetShipBody()?.gameObject?.transform;
        if (ship != null)
        {
            var gear = ship.Find("Module_Supplies/Systems_Supplies/ExpeditionGear");

            var spv = gear.GetComponent<SuitPickupVolume>();
            if (spv._containsSuit != spacesuitVisible)
            {
                //APRandomizer.OWMLModConsole.WriteLine($"SetSpacesuitVisible({spacesuitVisible}) found spv needs changing to {spacesuitVisible}");
                // a highly simplified version of the parts of SuitPickupVolume::OnPressInteract() we care about, e.g. without the SuitUp() call
                spv._containsSuit = !spv._containsSuit;
                spv._interactVolume.ChangePrompt(spv._containsSuit ? UITextType.SuitUpPrompt : UITextType.ReturnSuitPrompt, spv._pickupSuitCommandIndex);
                spv._suitOWCollider?.SetActivation(spv._containsSuit);
            }

            // Unfortunately the SPV doesn't seem to control most of the objects that toggle visibility when you don and doff the suit,
            // so we still have to manually toggle the rest of these.
            // For some reason all of the relevant objects are grouped together under PlayerSuit_Hanging *except* for the ProbeLauncher.

            // When you don/doff the suit, the game toggles each individual object's visibility one by one, never the whole group.
            // We mimic that here to avoid breaking the Remove Suit/Suit Up interactions.
            var hangingSuitModel = gear.Find("EquipmentGeo/Props_HEA_PlayerSuit_Hanging")?.gameObject;
            for (int c = 0; c < hangingSuitModel.transform.childCount; c++)
                hangingSuitModel.transform.GetChild(c).gameObject.SetActive(spacesuitVisible);

            var scoutLauncherOnFloorModel = gear.Find("EquipmentGeo/Props_HEA_ProbeLauncher")?.gameObject;
            scoutLauncherOnFloorModel.SetActive(spacesuitVisible);
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(ShipPromptController), nameof(ShipPromptController.LateInitialize))]
    public static void ShipPromptController_LateInitialize_Postfix(ShipPromptController __instance)
    {
        ApplyHasSpacesuitFlag(_hasSpacesuit);
    }
}
