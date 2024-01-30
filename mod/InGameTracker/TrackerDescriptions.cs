using Archipelago.MultiClient.Net.Models;
using System;
using System.Collections.Generic;

namespace ArchipelagoRandomizer.InGameTracker
{
    /// <summary>
    /// Class containing descriptions for the various items, put on its own for clarity
    /// </summary>
    public class TrackerDescriptions
    {
        public static void DisplayItemText(string itemID, ItemListWrapper Wrapper)
        {
            var inventory = Randomizer.SaveData.itemsAcquired;
            Dictionary<string, Tuple<string, string>> hints = Randomizer.Tracker.Hints;
            Wrapper.DescriptionFieldClear();
            List<string> infos = new();
            bool discoveredItem = Enum.TryParse(itemID, out Item result);
            // There may be a few dummy items that aren't actually tracked by the randomizer, like FrequencyOWV
            // In the event of a dummy item being submitted, we want to assume it does exist
            // If it doesn't it'll run into the default condition
            // Can't use the enums here for some reason, so use strings as the names
            if (!discoveredItem || inventory[result] > 0)
            {
                switch (itemID)
                {
                    case "Coordinates":
                        infos.Add("These are the coordinates of the Eye of the Universe.");
                        infos.Add("They will show in the bottom left corner when you're ready to input them.");
                        break;
                    case "LaunchCodes":
                        infos.Add("Codes from Hornfels that permit you to pilot your ship.");
                        break;
                    case "Translator":
                        infos.Add("The translator tool that you and Hal have been working on since Feldspar brought that Nomai wall to the museum.");
                        break;
                    case "Signalscope":
                        infos.Add("A fancy combination of telescope and signal receiver.");
                        infos.Add("You can obtain items by discovering signals.");
                        break;
                    case "Scout":
                        infos.Add("A standard-issue scout launcher, equipped with a light and camera.");
                        infos.Add("Your cameras require special lenses to detect certain frequencies.");
                        infos.Add("Note that you can still use Photo Mode even if your Scout Launcher is not functional.");
                        break;
                    case "CameraGM":
                        infos.Add("A special lens attached to your cameras that allow them to detect deadly Ghost Matter.");
                        infos.Add("Note that you can still use Photo Mode even if your Scout Launcher is not functional.");
                        break;
                    case "CameraQuantum":
                        infos.Add("A special lens attached to your cameras that allow them to avoid jamming when observing Quantum Objects.");
                        infos.Add("This will permit you to utilize the Rule of Quantum Imaging.");
                        infos.Add("Note that you can still use Photo Mode even if your Scout Launcher is not functional.");
                        break;
                    case "WarpPlatformCodes":
                        infos.Add("Special Nomai instructions for activating their Warp Platform technology at will.");
                        infos.Add("You do not need to wait for the Warp Platform to align with its target body to use the warp.");
                        infos.Add("Simply step on the Warp Platform and look down at it to activate a warp.");
                        break;
                    case "WarpCoreManual":
                        infos.Add("Attempting to remove and install alien technology could result in disaster if you don't know what you're doing.");
                        infos.Add("Fortunately, this manual provides detailed instructions. Turns out it's extremely simple. Just pull the Advanced Warp Core out and slot it back in.");
                        break;
                    case "EntanglementRule":
                        infos.Add("The Rule of Quantum Entanglement requires that you are not observing your environment at all.");
                        infos.Add("Small lights on your suit interfere with your ability to utilize this rule, so now you can turn them off to utilize Entanglement.");
                        break;
                    case "ShrineDoorCodes":
                        infos.Add("The shrine on the Quantum Moon used to be locked by a Nomai Interface Orb, but the shrine has fallen into disrepair and the orb seems to have disappeared.");
                        infos.Add("Fortunately, you've found some codes allowing you to override the door controls.");
                        break;
                    case "TornadoAdjustment":
                        infos.Add("With some slight adjustments to your ship's aerodynamics, you can utilize retrograde tornados to sink below the current on Giant's Deep.");
                        break;
                    case "SilentRunning":
                        infos.Add("You managed to fix the mufflers on your ship's and your jetpack's engines, allowing you to cruise in silence.");
                        break;
                    case "ElectricalInsulation":
                        infos.Add("Special padding applied to your space suit which allows you to endure low amounts of voltage, such as bioelectricity.");
                        infos.Add("However, it will not protect you from strong current from electrical wires or a damaged space ship.");
                        break;
                    case "FrequencyOWV":
                        infos.Add("The official frequency of the Outer Wilds Ventures space-faring travelers. Give it a listen to hear each Traveler's unique instrument!");
                        break;
                    case "FrequencyQF":
                        infos.Add("Strange signals emanate from the various Quantum objects scattered across the solar system.");
                        break;
                    case "FrequencyDB":
                        infos.Add("The Nomai seem to have left various distress beacons connected to their escape pods.");
                        break;
                    case "FrequencyHS":
                        infos.Add("Tephra and Galena want to play Hide and Seek with you!");
                        break;
                    default:
                        infos.Add($"Hmm, looks like an incorrect item ID was requested: {itemID}");
                        infos.Add("Please let Ixrec or Gamewyrm on the Archipelago or Outer Wilds Modding Discord know if you see this.");
                        Randomizer.OWMLModConsole.WriteLine($"ItemID {itemID} was requested on the tracker inventory, but no text for it could be found.", OWML.Common.MessageType.Error);
                        break;
                }
            }
            else
            {
                infos.Add("You have not obtained this yet.");

                if (hints.ContainsKey(itemID))
                {
                    infos.Add($"It looks like this item can be found at <color=#00FF7F>{hints[itemID].Item1}</color> in <color=#FAFAD2>{hints[itemID].Item2}</color>'s world.");
                }
            }
            switch (itemID)
            {
                case "FrequencyOWV":
                    infos.Add("Signals found: ");
                    if (inventory[Item.SignalEsker] > 0) infos.Add("  Esker's Whistling");
                    if (inventory[Item.SignalChert] > 0) infos.Add("  Chert's Drums");
                    if (inventory[Item.SignalRiebeck] > 0) infos.Add("  Riebeck's Banjo");
                    if (inventory[Item.SignalGabbro] > 0) infos.Add("  Gabbro's Flute");
                    if (inventory[Item.SignalFeldspar] > 0) infos.Add("  Feldspar's Harmonica");
                    break;
                case "FrequencyQF":
                    infos.Add("Signals found: ");
                    if (inventory[Item.SignalMuseumShard] > 0) infos.Add("  Museum Shard");
                    if (inventory[Item.SignalGroveShard] > 0) infos.Add("  Grove Shard");
                    if (inventory[Item.SignalCaveShard] > 0) infos.Add("  Cave Shard");
                    if (inventory[Item.SignalTowerShard] > 0) infos.Add("  Tower Shard");
                    if (inventory[Item.SignalIslandShard] > 0) infos.Add("  Island Shard");
                    if (inventory[Item.SignalQM] > 0) infos.Add("  Quantum Moon");
                    break;
                case "FrequencyDB":
                    infos.Add("Signals found: ");
                    if (inventory[Item.SignalEP1] > 0) infos.Add("  Escape Pod 1");
                    if (inventory[Item.SignalEP2] > 0) infos.Add("  Escape Pod 2");
                    if (inventory[Item.SignalEP3] > 0) infos.Add("  Escape Pod 3");
                    break;
                case "FrequencyHS":
                    infos.Add("Signals found: ");
                    if (inventory[Item.SignalTephra] > 0) infos.Add("  Tephra");
                    if (inventory[Item.SignalGalena] > 0) infos.Add("  Galena");
                    break;
            }
            foreach (string info in infos)
            {
                Wrapper.DescriptionFieldGetNextItem().DisplayText(info);
            }
        }
    }
}
