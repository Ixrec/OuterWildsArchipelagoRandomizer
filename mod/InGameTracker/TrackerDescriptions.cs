using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ArchipelagoRandomizer.InGameTracker
{
    /// <summary>
    /// Class containing descriptions for the various items, put on its own for clarity
    /// </summary>
    public class TrackerDescriptions
    {
        public static void DisplayItemText(string itemID, ItemListWrapper Wrapper)
        {
            var inventory = APRandomizer.SaveData.itemsAcquired;
            Wrapper.DescriptionFieldClear();
            if (!APRandomizer.Tracker.ItemEntries.ContainsKey(itemID))
            {
                APRandomizer.OWMLModConsole.WriteLine($"Could not obtain a tracker description for {itemID}!", OWML.Common.MessageType.Error);
                return;
            }
            InventoryItemEntry itemEntry = APRandomizer.Tracker.ItemEntries[itemID];

            var infos = GetDescriptionText(itemEntry, new ReadOnlyDictionary<Item, uint>(inventory));

            foreach (string info in infos)
                Wrapper.DescriptionFieldGetNextItem().DisplayText(info);
        }
        private static List<string> GetErrorDescription(string itemID)
        {
            APRandomizer.OWMLModConsole.WriteLine($"ItemID {itemID} was requested on the tracker inventory, but no text for it could be found.", OWML.Common.MessageType.Error);
            return [
                $"Hmm, looks like an incorrect item ID was requested: {itemID}",
                "Please let Ixrec or Gamewyrm on the Archipelago or Outer Wilds Modding Discord know if you see this.",
            ];
        }

        private static List<string> GetDescriptionText(InventoryItemEntry itemEntry, ReadOnlyDictionary<Item, uint> inventory)
        {
            var itemID = itemEntry.ID;
            List<string> infos = [];

            // We currently only have one Inventory entry that doesn't correspond to an AP item: the OWV frequency
            if (itemEntry.ApItem == null)
            {
                switch (itemEntry.ID)
                {
                    case "FrequencyOWV":
                        infos.Add("The official frequency of the Outer Wilds Ventures space-faring travelers. Give it a listen to hear each Traveler's unique instrument!");
                        infos.Add("Signals found: ");
                        if (inventory[Item.SignalEsker] > 0) infos.Add("  Esker's Whistling");
                        if (inventory[Item.SignalChert] > 0) infos.Add("  Chert's Drums");
                        if (inventory[Item.SignalRiebeck] > 0) infos.Add("  Riebeck's Banjo");
                        if (inventory[Item.SignalGabbro] > 0) infos.Add("  Gabbro's Flute");
                        if (inventory[Item.SignalFeldspar] > 0) infos.Add("  Feldspar's Harmonica");
                        break;
                    default:
                        return GetErrorDescription(itemID);
                }
            }
            // For now, we use the same placeholder description for any AP item we have 0 of
            else if (!itemEntry.HasOneOrMore())
            {
                infos.Add("You have not obtained this yet.");

                if (itemEntry.HintedLocation != "")
                {
                    infos.Add($"It looks like this item can be found at <color=#00FF7F>{itemEntry.HintedLocation}</color> in <color=#FAFAD2>{itemEntry.HintedWorld}</color>'s world" +
                        $"{(itemEntry.HintedEntrance == "" ? "" : $" at <color=#6291E4>{itemEntry.HintedEntrance}</color>")}.");
                }
            }
            else
            {
                // The "normal" case: We have at least 1 of this item, and it's a real AP item
                switch (itemEntry.ApItem)
                {
                    case Item.FrequencyQF:
                        infos.Add("Strange signals emanate from the various Quantum objects scattered across the solar system.");
                        infos.Add("Signals found: ");
                        if (inventory[Item.SignalMuseumShard] > 0) infos.Add("  Museum Shard");
                        if (inventory[Item.SignalGroveShard] > 0) infos.Add("  Grove Shard");
                        if (inventory[Item.SignalCaveShard] > 0) infos.Add("  Cave Shard");
                        if (inventory[Item.SignalTowerShard] > 0) infos.Add("  Tower Shard");
                        if (inventory[Item.SignalIslandShard] > 0) infos.Add("  Island Shard");
                        if (inventory[Item.SignalQM] > 0) infos.Add("  Quantum Moon");
                        break;
                    case Item.FrequencyDB:
                        infos.Add("The Nomai seem to have left various distress beacons connected to their escape pods.");
                        infos.Add("Signals found: ");
                        if (inventory[Item.SignalEP1] > 0) infos.Add("  Escape Pod 1");
                        if (inventory[Item.SignalEP2] > 0) infos.Add("  Escape Pod 2");
                        if (inventory[Item.SignalEP3] > 0) infos.Add("  Escape Pod 3");
                        break;
                    case Item.FrequencyHS:
                        infos.Add("Tephra and Galena want to play Hide and Seek with you!");
                        infos.Add("Signals found: ");
                        if (inventory[Item.SignalTephra] > 0) infos.Add("  Tephra");
                        if (inventory[Item.SignalGalena] > 0) infos.Add("  Galena");
                        break;

                    case Item.Coordinates:
                        infos.Add("These are the coordinates of the Eye of the Universe.");
                        infos.Add("They will show in the bottom left corner when you're ready to input them.");
                        break;
                    case Item.LaunchCodes:
                        infos.Add("Codes from Hornfels that permit you to pilot your ship.");
                        break;
                    case Item.Translator:
                        infos.Add("The translator tool that you and Hal have been working on since Feldspar brought that Nomai wall to the museum.");
                        break;
                    case Item.Signalscope:
                        infos.Add("A fancy combination of telescope and signal receiver.");
                        infos.Add("You can obtain items by discovering signals.");
                        break;
                    case Item.Scout:
                        infos.Add("A standard-issue Little Scout, equipped with a light and camera.");
                        infos.Add("Like your Photo Mode camera, it requires special lenses to detect certain wavelengths.");
                        infos.Add("Note that your scout launcher can still use Photo Mode even if there's no Scout in it.");
                        break;
                    case Item.CameraGM:
                        infos.Add("A special lens attached to your cameras that allow them to detect deadly Ghost Matter.");
                        infos.Add("Note that your scout launcher can still use Photo Mode even if there's no Scout in it.");
                        break;
                    case Item.CameraQuantum:
                        infos.Add("A special lens attached to your cameras that allow them to avoid jamming when observing Quantum Objects.");
                        infos.Add("This will permit you to utilize the Rule of Quantum Imaging.");
                        infos.Add("Note that your scout launcher can still use Photo Mode even if there's no Scout in it.");
                        break;
                    case Item.WarpPlatformCodes:
                        infos.Add("Special Nomai instructions for activating their Warp Platform technology at will.");
                        infos.Add("You don't need to wait for the Warp Platform to align with its target astral body to use the warp.");
                        infos.Add("Simply step on the Warp Platform and look down at it to activate a warp.");
                        break;
                    case Item.WarpCoreManual:
                        infos.Add("Attempting to remove and install alien technology could result in disaster if you don't know what you're doing.");
                        infos.Add("Fortunately, this manual provides detailed instructions. Turns out it's extremely simple. Just pull the Advanced Warp Core out and slot it back in.");
                        break;
                    case Item.EntanglementRule:
                        infos.Add("The Rule of Quantum Entanglement requires that you are not observing your environment at all.");
                        infos.Add("Small lights on your suit interfere with your ability to utilize this rule, so now you can turn them off to utilize Entanglement.");
                        break;
                    case Item.ShrineDoorCodes:
                        infos.Add("The shrine on the Quantum Moon used to be locked by a Nomai Interface Orb, but the shrine has fallen into disrepair and the orb seems to have disappeared.");
                        infos.Add("Fortunately, you've found some codes allowing you to override the door controls, so now you can utilize the Rule of the Sixth Location.");
                        break;
                    case Item.TornadoAdjustment:
                        infos.Add("With some slight adjustments to your ship's aerodynamics, you can utilize retrograde tornados to sink below the current on Giant's Deep.");
                        break;
                    case Item.SilentRunning:
                        infos.Add("You managed to fix the mufflers on your ship's and your jetpack's engines, allowing you to cruise in silence.");
                        break;
                    case Item.ElectricalInsulation:
                        infos.Add("Special padding applied to your space suit which allows you to endure low amounts of voltage, such as the bioelectricity inside a jellyfish's body.");
                        infos.Add("However, it will not protect you from strong current from electrical wires, a damaged space ship, or jellyfish tentacles.");
                        infos.Add("Did you know a jellyfish's fluffier inner tentacles are actually called 'oral arms'? I can't forget that, so now you can't either.");
                        break;
                    case Item.Autopilot:
                        infos.Add("You fixed the faulty wiring between the autopilot module and your spaceship controls.");
                        break;
                    case Item.LandingCamera:
                        infos.Add("It can be hard to land on things without looking at them, but landing cockpit-first is considered dangerous for some reason.");
                        infos.Add("You cleaned a half-melted marshmallow off of the lens.");
                        break;
                    case Item.EjectButton:
                        infos.Add("You finally got the cover unstuck, giving you an exciting new way to destroy your spaceship.");
                        break;
                    case Item.VelocityMatcher:
                        infos.Add("Hold the button to gradually 'stop moving' relative to the planet you're on, or the object you're locked on to.");
                        infos.Add("Applies to both your spaceship and your spacesuit.");
                        break;
                    case Item.SurfaceIntegrityScanner:
                        infos.Add("An upgrade for your Little Scout that reports the structural integrity of a fragile surface it's attached to.");
                        infos.Add("Most useful on Brittle Hollow.");
                        break;
                    case Item.OxygenCapacityUpgrade:
                        infos.Add("An Outer Wilds Ventures standard-issue oxygen tank holds 7.5 minutes of oxygen.");
                        infos.Add("Your tank started with half of that. Each upgrade doubles its capacity.");
                        break;
                    case Item.FuelCapacityUpgrade:
                        infos.Add("An Outer Wilds Ventures standard-issue jetpack fuel tank holds enough fuel to fire thrusters for 100 seconds.");
                        infos.Add("Your tank started with half of that. Each upgrade doubles its capacity.");
                        break;
                    case Item.BoostDurationUpgrade:
                        infos.Add("An Outer Wilds Ventures standard-issue jetpack can boost for 1 second before recharging.");
                        infos.Add("Your jetpack started with half a second of boost. Each upgrade doubles its length.");
                        break;
                    case Item.OxygenRefill:
                        infos.Add("Fully refills your suit's oxygen tank.");
                        break;
                    case Item.FuelRefill:
                        infos.Add("Fully refills your suit jetpack's fuel tank.");
                        break;
                    case Item.Marshmallow:
                        infos.Add("Refills half your health. Also comes in 'perfect' and 'burnt' variants.");
                        break;
                    case Item.ShipDamageTrap:
                        infos.Add("Mysterious forces spontaneously damage parts of your spaceship.");
                        infos.Add("If you're not in your ship when this happens, your suit will helpfully inform you of what broke.");
                        break;
                    case Item.AudioTrap:
                        infos.Add("A glitch in your suit speakers randomly plays disturbing sounds or music. But they're just sounds, they can't hurt you.");
                        break;
                    case Item.NapTrap:
                        infos.Add("Did you know over 27% of Timber Hearthians suffer from narcolepsy?");
                        infos.Add("The primary symptoms are sporadic 'sleep attacks' which last about one minute. Patients report feeling a sleep attack about three seconds before loss of vision and motor control.");
                        break;
                    default:
                        return GetErrorDescription(itemID);
                }
            }

            return infos;
        }
    }
}
