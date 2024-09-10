using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ArchipelagoRandomizer.InGameTracker;

/// <summary>
/// Class containing descriptions for the various items, put on its own for clarity
/// </summary>
public class APInventoryDescriptions
{
    public static void DisplayItemText(InventoryItemEntry itemEntry, ItemListWrapper Wrapper)
    {
        var inventory = APRandomizer.SaveData.itemsAcquired;
        Wrapper.DescriptionFieldClear();

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

        if (!itemEntry.HasOneOrMore())
        {
            infos.Add("You have not obtained this yet.");
        }

        // We currently only have one Inventory entry that doesn't correspond to an AP item: the OWV frequency
        if (itemEntry.ApItem == null)
        {
            switch (itemEntry.ID)
            {
                case "FrequencyOWV":
                    infos.Add("The official frequency of the Outer Wilds Ventures space-faring travelers. Give it a listen to hear each Traveler's unique instrument!");
                    infos.Add("Signals found: ");
                    // we don't use the SignalsAndFrequencies maps here because these names are slightly different from the AP item names
                    infos.Add($"[{((inventory[Item.SignalEsker] > 0) ? 'X' : ' ')}] Esker's Whistling");
                    infos.Add($"[{((inventory[Item.SignalChert] > 0) ? 'X' : ' ')}] Chert's Drums");
                    infos.Add($"[{((inventory[Item.SignalRiebeck] > 0) ? 'X' : ' ')}] Riebeck's Banjo");
                    infos.Add($"[{((inventory[Item.SignalGabbro] > 0) ? 'X' : ' ')}] Gabbro's Flute");
                    infos.Add($"[{((inventory[Item.SignalFeldspar] > 0) ? 'X' : ' ')}] Feldspar's Harmonica");
                    break;
                default:
                    return GetErrorDescription(itemID);
            }
        }
        else
        {
            // The "normal" case: We have at least 1 of this item, and it's a real AP item
            switch (itemEntry.ApItem)
            {
                // Frequency items are special though
                case Item.FrequencyQF: case Item.FrequencyDB: case Item.FrequencyHS: case Item.FrequencyDSR:
                    switch (itemEntry.ApItem)
                    {
                        case Item.FrequencyQF:
                            infos.Add("Strange signals emanate from the various Quantum objects scattered across the solar system.");
                            break;
                        case Item.FrequencyDB:
                            infos.Add("The Nomai seem to have left various distress beacons connected to their escape pods.");
                            break;
                        case Item.FrequencyHS:
                            infos.Add("Tephra and Galena want to play Hide and Seek with you!");
                            break;
                        case Item.FrequencyDSR:
                            infos.Add("Timber Hearth's radio tower uses this frequency to communicate with the deep space satellite.");
                            break;
                    }

                    infos.Add("Signals found: ");

                    Item apItem = (Item)itemEntry.ApItem; // C# doesn't understand the null check we already did above
                    var signals = SignalsAndFrequencies.frequencyToSignals[ItemNames.itemToFrequency[apItem]];
                    foreach (var signal in signals)
                    {
                        var item = ItemNames.signalToItem[signal];
                        var name = ItemNames.ItemToName(item);
                        infos.Add(((inventory.ContainsKey(item) && inventory[item] > 0) ? "[X] " : "[ ] ") + name.Replace(" Signal", ""));
                    }
                    break;

                case Item.SignalFeldspar:
                    infos.Add("Allows your Signalscope to track Feldspar's harmonica signal no matter how far away they are.");
                    infos.Add("This is required to find Feldspar's camp inside Dark Bramble.");
                    break;
                case Item.SignalEP3:
                    infos.Add("Allows your Signalscope to track Escape Pod 3's distress signal no matter how far away it is.");
                    infos.Add("This is required to find Escape Pod 3 and the Nomai Grave inside Dark Bramble.");
                    break;
                case Item.SignalQM:
                    infos.Add("Allows your Signalscope to track the Quantum Moon signal no matter how far away it is, and scan that signal without landing on the moon.");
                    infos.Add("Often this won't matter, but you might need it to scan the Quantum Moon signal before receiving the Imaging Rule.");
                    break;

                case Item.Coordinates:
                    infos.Add("These are the coordinates of the Eye of the Universe.");
                    infos.Add("They will show in the bottom left corner when you're ready to input them.");
                    break;
                case Item.Spacesuit:
                    infos.Add("The spacesuit that enables breathing in outer space, displays various useful information on its HUD, and most importantly, lets you fly around with a jetpack.");
                    infos.Add("Unless you turned on the shuffle_spacesuit option when generating this world, you would've had this item from the start.");
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
                    infos.Add("By default, your tank starts with 50% of that, at 3.75 minutes. Each upgrade adds another 50% or 3.75 minutes.");
                    infos.Add("These values can be changed in the mod settings (warning: lower values may make the game unbeatable).");
                    break;
                case Item.FuelCapacityUpgrade:
                    infos.Add("An Outer Wilds Ventures standard-issue jetpack fuel tank holds enough fuel to fire thrusters for 100 seconds.");
                    infos.Add("By default, your tank starts with 50% of that, at 50 seconds. Each upgrade adds another 50% or 50 seconds.");
                    infos.Add("These values can be changed in the mod settings (warning: lower values may make the game unbeatable).");
                    break;
                case Item.BoostDurationUpgrade:
                    infos.Add("An Outer Wilds Ventures standard-issue jetpack can boost for 1 second before recharging.");
                    infos.Add("By default, your jetpack starts with 50% of that, i.e. half a second of boost. Each upgrade adds another 50% or half a second.");
                    infos.Add("These values can be changed in the mod settings (warning: lower values may make the game unbeatable).");
                    break;
                case Item.OxygenRefill:
                    infos.Add("Fully refills your suit's oxygen tank.");
                    break;
                case Item.FuelRefill:
                    infos.Add("Fully refills your suit jetpack's fuel tank.");
                    break;
                case Item.Marshmallow:
                    infos.Add("Refills half your health. Also comes in rare 'perfect' and 'burnt' variants that restore full health and no health.");
                    var perfectCount = inventory[Item.PerfectMarshmallow];
                    var burntCount = inventory[Item.BurntMarshmallow];
                    var total = inventory[Item.Marshmallow] + perfectCount + burntCount;
                    infos.Add($"Of the {total} marshmallows you've eaten, {burntCount} were burnt and {perfectCount} were perfect.");
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
                    infos.Add("The primary symptoms are sporadic 'sleep attacks' which last about one minute. Patients report feeling a sleep attack about three seconds before loss of vision.");
                    infos.Add("It is strongly advised not to attempt 'sleepwalking' during these attacks, unless there was something even more dangerous nearby when you fell asleep.");
                    break;
                case Item.LightModulator:
                    infos.Add("Makes your flashlight and your Scout's lights (if you have the Scout) emit the same wavelength as the eyeshine of The Stranger's inhabitants.");
                    infos.Add("That wavelength activates the light sensors controlling most doors, elevators and rafts inside The Stranger.");
                    infos.Add("This is not required to enter The Stranger, because the light sensors in the hangar airlocks accept a wider range of light. Presumably the airlocks were built to higher engineering standards than most of the ship.");
                    infos.Add("This also doesn't matter inside the simulation, since the artifact was already designed to emit this wavelength.");
                    break;
                case Item.BreachOverrideCodes:
                    infos.Add("Allows you to open the Laboratory doors and directly access Hidden Gorge, despite them being locked down to contain a hull breach.");
                    break;
                case Item.RLPaintingCode:
                    infos.Add("Opens the secret door to the basement of the River Lowlands tower.");
                    infos.Add("The green fire in that tower is one of two ways to reach Shrouded Woodlands. The other involves Raft Docks Patch.");
                    break;
                case Item.CIPaintingCode:
                    infos.Add("Opens the secret door to the basement of the Cinder Isles tower.");
                    infos.Add("The green fire in that tower is one of two ways to reach Starlit Cove. The other involves Raft Docks Patch.");
                    break;
                case Item.HGPaintingCode:
                    infos.Add("Opens the secret door to the basement of the Hidden Gorge tower.");
                    infos.Add("The green fire in that tower is one of two ways to reach Endless Canyon. The other involves Raft Docks Patch.");
                    break;
                case Item.DreamTotemPatch:
                    infos.Add("Patches the simulation to make totems compatible with Hearthians. This allows you to project objects, extinguish objects, and warp to hand totems within the simulation.");
                    break;
                case Item.RaftDocksPatch:
                    infos.Add("Patches a door in Shrouded Woodlands, the dock in Starlit Cove, and an elevator in Endless Canyon, so those three areas can all be entered from the raft docks on the simulation's main river.");
                    infos.Add("Normally none of these docks can be used without first using the corresponding green flame to unlock the dock. But this patch allows you to use any one Painting Code to reach all three areas.");
                    break;
                case Item.LimboWarpPatch:
                    infos.Add("After the Stranger's inhabitants discoverd they could fall below the simulation world by jumping off a raft, they developed a 'temporary' hack that simply kills anyone who tries it.");
                    infos.Add("This patches the simulation to undo that hack, allowing you to exploit the original glitch to reach the center of Subterranean Lake.");
                    break;
                case Item.ProjectionRangePatch:
                    infos.Add("After the Stranger's inhabitants discoverd they could walk beyond the simulation artifact's projection radius, they developed a 'temporary' hack that simply teleports you back to the artifact if you get out of range.");
                    infos.Add("This patches the simulation to undo that hack, allowing you to exploit the original glitch to see parts of the simulation hidden by the projection.");
                    break;
                case Item.AlarmBypassPatch:
                    infos.Add("After the Stranger's inhabitants discoverd the simulation's alarm bells don't work on the deceased, they developed a 'temporary' hack that simply refuses to import your brainwaves if no heartbeat is detected.");
                    infos.Add("This patches the simulation to undo that hack, allowing you to exploit the original glitch to bypass alarm bells in the simulation.");
                    break;
                default:
                    return GetErrorDescription(itemID);
            }
        }

        foreach (var hint in itemEntry.Hints)
        {
            infos.Add($"It looks like this item can be found at <color=#00FF7F>{hint.Location}</color> in <color=#FAFAD2>{hint.World}</color>'s world" +
                $"{(hint.Entrance == "" ? "" : $" at <color=#6291E4>{hint.Entrance}</color>")}.");
        }

        return infos;
    }
}
