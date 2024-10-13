using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ArchipelagoRandomizer.InGameTracker;

// Tuples: name, green arrow, green exclamation point, orange asterisk
using InventoryDisplayItem = Tuple<string, bool, bool, bool>;

/// <summary>
/// The inventory screen. All the functions here are required, even if empty.
/// </summary>
public class APInventoryMode : ShipLogMode
{
    public ItemListWrapper Wrapper;
    public GameObject RootObject;
    public TrackerManager Tracker;

    private Image Icon => Wrapper.GetPhoto();
    private Text QuestionMark => Wrapper.GetQuestionMark();

    // This dictionary is the list of items in the Inventory Mode
    // They'll also display in this order, with the second string as the visible name
    private static List<InventoryItemEntry> _ItemEntries = new()
    {
        // Progression items you normally start with in vanilla
        new InventoryItemEntry(Item.Spacesuit, "Spacesuit"),
        new InventoryItemEntry(Item.LaunchCodes, "Launch Codes"),
        new InventoryItemEntry(Item.Translator, "Translator"),
        new InventoryItemEntry(Item.Signalscope, "Signalscope"),
        new InventoryItemEntry(Item.Scout, "Scout"),
        new InventoryItemEntry(Item.CameraGM, "Ghost Matter Wavelength"),

        // Progression items that represent vanilla knowledge checks you wouldn't have at the start
        new InventoryItemEntry(Item.ElectricalInsulation, "Electrical Insulation"),
        new InventoryItemEntry(Item.SilentRunning, "Silent Running Mode"),
        new InventoryItemEntry(Item.TornadoAdjustment, "Tornado Aerodynamic Adjustments"),
        new InventoryItemEntry(Item.WarpPlatformCodes, "Nomai Warp Platform Codes"),
        new InventoryItemEntry(Item.WarpCoreManual, "Nomai Warp Core Installation Manual"),
        new InventoryItemEntry(Item.CameraQuantum, "Imaging Rule / Quantum Wavelength"),
        new InventoryItemEntry(Item.EntanglementRule, "Entanglement Rule / Suit Lights Controls"),
        new InventoryItemEntry(Item.ShrineDoorCodes, "Sixth Location Rule / Shrine Door Codes"),
        new InventoryItemEntry(Item.Coordinates, "Eye of the Universe Coordinates"),

        // Progression items for the EotE DLC
        new InventoryItemEntry(Item.LightModulator, "Stranger Light Modulator", true),
        new InventoryItemEntry(Item.BreachOverrideCodes, "Breach Override Codes", true),
        new InventoryItemEntry(Item.RLPaintingCode, "River Lowlands Painting Code", true),
        new InventoryItemEntry(Item.CIPaintingCode, "Cinder Isles Painting Code", true),
        new InventoryItemEntry(Item.HGPaintingCode, "Hidden Gorge Painting Code", true),
        new InventoryItemEntry(Item.DreamTotemPatch, "Dream Totem Patch", true),
        new InventoryItemEntry(Item.RaftDocksPatch, "Raft Docks Patch", true),
        new InventoryItemEntry(Item.LimboWarpPatch, "Limbo Warp Patch", true),
        new InventoryItemEntry(Item.ProjectionRangePatch, "Projection Range Patch", true),
        new InventoryItemEntry(Item.AlarmBypassPatch, "Alarm Bypass Patch", true),

        // Signalscope frequencies. The individual signals are listed within each frequency entry.
        new InventoryItemEntry("FrequencyOWV", "Frequency: Outer Wilds Ventures"),
        new InventoryItemEntry(Item.SignalFeldspar, "   Signal: Feldspar's Harmonica"),
        new InventoryItemEntry(Item.FrequencyDB, "Frequency: Distress Beacons"),
        new InventoryItemEntry(Item.SignalEP3, "   Signal: Escape Pod 3"),
        new InventoryItemEntry(Item.FrequencyQF, "Frequency: Quantum Fluctuations"),
        new InventoryItemEntry(Item.SignalQM, "   Signal: Quantum Moon"),
        new InventoryItemEntry(Item.FrequencyHS, "Frequency: Hide and Seek"),
        new InventoryItemEntry(Item.FrequencyDSR, "Frequency: Deep Space Radio"),
        new InventoryItemEntry("StoryModFrequencies", "Story Mod Frequencies"),

        // Hearth's Neighbor 2: Magistarium custom items
        new InventoryItemEntry(Item.MemoryCubeInterface, "HN2: Memory Cube Interface", false, "enable_hn2_mod"),
        new InventoryItemEntry(Item.MagistariumLibraryAccessCode, "HN2: Magistarium Library Access Code", false, "enable_hn2_mod"),
        new InventoryItemEntry(Item.MagistariumDormitoryAccessCode, "HN2: Magistarium Dormitory Access Code", false, "enable_hn2_mod"),
        new InventoryItemEntry(Item.MagistariumEngineAccessCode, "HN2: Magistarium Engine Access Code", false, "enable_hn2_mod"),

        // Non-progression ship and equipment upgrades
        new InventoryItemEntry(Item.Autopilot, "Autopilot"),
        new InventoryItemEntry(Item.LandingCamera, "Landing Camera"),
        new InventoryItemEntry(Item.EjectButton, "Eject Button"),
        new InventoryItemEntry(Item.VelocityMatcher, "Velocity Matcher"),
        new InventoryItemEntry(Item.SurfaceIntegrityScanner, "Surface Integrity Scanner"),
        new InventoryItemEntry(Item.OxygenCapacityUpgrade, "Suit Upgrade: Oxygen Capacity"),
        new InventoryItemEntry(Item.FuelCapacityUpgrade, "Suit Upgrade: Fuel Capacity"),
        new InventoryItemEntry(Item.BoostDurationUpgrade, "Suit Upgrade: Boost Duration"),

        // Filler items
        new InventoryItemEntry(Item.OxygenRefill, "Oxygen Refill"),
        new InventoryItemEntry(Item.FuelRefill, "Jetpack Fuel Refill"),
        new InventoryItemEntry(Item.Marshmallow, "Marshmallow"), // includes Perfect and Burnt

        // Trap items
        new InventoryItemEntry(Item.ShipDamageTrap, "Ship Damage Trap"),
        new InventoryItemEntry(Item.AudioTrap, "Audio Trap"),
        new InventoryItemEntry(Item.NapTrap, "Nap Trap"),
    };

    // The ID being both the key and the the first value in the InventoryItemEntry is intentional redundancy in the public API for cleaner client code
    private static Dictionary<string, InventoryItemEntry> ItemEntries = _ItemEntries.ToDictionary(entry => entry.ID, entry => entry);

    private static Dictionary<string, InventoryItemEntry> VisibleItemEntries = null;

    // Runs when the mode is created
    public override void Initialize(ScreenPromptList centerPromptList, ScreenPromptList upperRightPromptList, OWAudioSource oneShotSource)
    {}

    // Runs when the mode is opened in the ship computer
    public override void EnterMode(string entryID = "", List<ShipLogFact> revealQueue = null)
    {
        Wrapper.Open();
        Wrapper.SetName("AP Inventory");
        Wrapper.SetItems(GenerateDisplayItems());
        Wrapper.SetSelectedIndex(0);
        Wrapper.UpdateList();
        RootObject.name = "ArchipelagoInventoryMode";

        SelectItem(Wrapper.GetSelectedIndex());
    }

    // Runs when the mode is closed
    public override void ExitMode()
    {
        foreach (InventoryItemEntry entry in ItemEntries.Values)
        {
            entry.SetNew(false);
        }
        Wrapper.Close();
    }


    // Runs when player enters computer, update info that changes between computer sessions. Runs after EnterMode
    public override void OnEnterComputer()
    {

    }

    // Runs when the player exits the computer, after ExitMode
    public override void OnExitComputer()
    {

    }

    // Runs every frame the mode is active
    public override void UpdateMode()
    {
        int changeIndex = Wrapper.UpdateList();

        if (changeIndex != 0)
        {
            SelectItem(Wrapper.GetSelectedIndex());
        }
    }

    // Allows leaving the computer in this mode
    public override bool AllowCancelInput()
    {
        return true;
    }
    
    // Allows swapping modes while in this mode
    public override bool AllowModeSwap()
    {
        return true;
    }

    // Returns the ID of the selected ship entry, used for knowing which entry should be highlighted when switching to Map Mode. Useless for us probably.
    public override string GetFocusedEntryID()
    {
        return "";
    }

    // Shows the item selected and the associated info
    private void SelectItem(int index)
    {
        if (VisibleItemEntries == null) return;
        InventoryItemEntry entry = VisibleItemEntries.ElementAt(index).Value;
        string itemID = entry.ID;
        Sprite sprite = TrackerManager.GetSprite(itemID);
        if (entry.HasOneOrMore() || (entry.ApItem == Item.Translator && APRandomizer.SlotEnabledSplitTranslator()))
        {
            if (sprite != null)
            {
                Icon.sprite = sprite;
                Icon.gameObject.SetActive(true);
                QuestionMark.gameObject.SetActive(false);
            }
        }
        else
        {
            Icon.gameObject.SetActive(false);
            QuestionMark.gameObject.SetActive(true);
        }

        APInventoryDescriptions.DisplayItemText(entry, Wrapper);
    }


    // Determines what items the player has and shows them in the inventory mode
    private List<InventoryDisplayItem> GenerateDisplayItems()
    {
        Dictionary<Item, uint> items = APRandomizer.SaveData.itemsAcquired;

        VisibleItemEntries = new();

        List<InventoryDisplayItem> inventoryDisplayItems = [];
        foreach (var (name, item) in ItemEntries)
        {
            if (item.IsDLCOnly && !APRandomizer.SlotEnabledEotEDLC())
                continue;
            if (item.StoryModOption != null && !(APRandomizer.SlotData.ContainsKey(item.StoryModOption) && (long)APRandomizer.SlotData[item.StoryModOption] > 0))
                continue;

            VisibleItemEntries.Add(name, item);

            if (Enum.TryParse(item.ID, out Item subject))
            {
                uint quantity = items.ContainsKey(subject) ? items[subject] : 0;

                // The three marshmallow items are treated as a single "Marshmallow" entry by the tracker
                if (subject == Item.Marshmallow)
                    quantity += items[Item.BurntMarshmallow] + items[Item.PerfectMarshmallow];

                // Produce a string like "[X] Launch Codes" or "[5] Marshmallow"
                bool couldHaveMultiple = item.ApItem >= Item.OxygenCapacityUpgrade && item.ApItem <= Item.NapTrap; // see comments in Item enum
                string countText = couldHaveMultiple ? quantity.ToString() : (quantity != 0 ? "X" : " "); // only unique items use X
                string itemName = $"[{countText}] {item.Name}";

                // Tuple: name, green arrow, green exclamation point, orange asterisk
                inventoryDisplayItems.Add(new InventoryDisplayItem(itemName, false, item.ItemIsNew, item.Hints.Any()));
            }
            else if (item.ID == "FrequencyOWV")
            {
                string itemName = "[X] Frequency: Outer Wilds Ventures";
                inventoryDisplayItems.Add(new InventoryDisplayItem(itemName, false, item.ItemIsNew, false));
            }
            else if (item.ID == "StoryModFrequencies")
            {
                var storyModFrequencies = items.Where(kv => ItemNames.IsStoryModFrequency(kv.Key));
                var status = " ";
                // technically wrong if you enable some but not all story mods, but I don't think anyone cares that much
                if (storyModFrequencies.All(kv => kv.Value > 0)) status = "X";
                else if (storyModFrequencies.Any(kv => kv.Value > 0)) status = "-";

                string itemName = $"[{status}] Story Mod Frequencies";
                inventoryDisplayItems.Add(new InventoryDisplayItem(itemName, false, item.ItemIsNew, false));
            }
            else
            {
                APRandomizer.OWMLModConsole.WriteLine($"Tried to parse {item} as an Item enum, but it was invalid. Unable to determine if the item is in the inventory.", OWML.Common.MessageType.Error);
            }
        }

        return inventoryDisplayItems;
    }

    /// <summary>
    /// Sets an item as new, so it'll have a green exclamation point in the inventory
    /// </summary>
    /// <param name="item"></param>
    public static void MarkItemAsNew(Item item)
    {
        // The three marshmallow items are treated as a single "Marshmallow" entry by the tracker
        if (item == Item.BurntMarshmallow || item == Item.PerfectMarshmallow)
            item = Item.Marshmallow;

        if (item >= Item.TranslatorHGT && item <= Item.TranslatorOther)
            item = Item.Translator;

        string itemID = item.ToString();
        TrackerManager tracker = APRandomizer.Tracker;
        if (ItemNames.itemToSignal.ContainsKey(item))
        {
            string frequency = "";
            var sf = SignalsAndFrequencies.signalToFrequency[ItemNames.itemToSignal[item]];
            if (sf == "Traveler")
                frequency = "FrequencyOWV";
            else if (ItemNames.frequencyToItem.TryGetValue(sf, out var frequencyItem))
                if (ItemNames.IsStoryModFrequency(frequencyItem))
                    frequency = "StoryModFrequencies";
                else
                    frequency = frequencyItem.ToString();

            if (frequency == "" || !ItemEntries.ContainsKey(frequency))
            {
                APRandomizer.OWMLModConsole.WriteLine($"Signal item {itemID} with invalid frequency {frequency} requested to be marked as new! Skipping", OWML.Common.MessageType.Warning);
                return;
            }

            ItemEntries[frequency].SetNew(true);
        }
        else if (ItemEntries.ContainsKey(itemID))
        {
            if (!ItemEntries.ContainsKey(itemID))
            {
                APRandomizer.OWMLModConsole.WriteLine($"Invalid item {itemID} requested to be marked as new! Skipping", OWML.Common.MessageType.Warning);
                return;
            }
            ItemEntries[itemID].SetNew(true);
        }
        else APRandomizer.OWMLModConsole.WriteLine($"Item received is {itemID}, which does not exist in the inventory. Skipping.", OWML.Common.MessageType.Warning);
    }

    public static void AddHint(Hint hint, ArchipelagoSession session)
    {
        // We don't care about hints for items that have already been found
        if (hint.Found) return;

        ItemNames.archipelagoIdToItem.TryGetValue(hint.ItemId, out Item item);

        if (item >= Item.TranslatorHGT && item <= Item.TranslatorOther)
            item = Item.Translator;

        string itemName = item.ToString();
        if (ItemNames.IsStoryModFrequency(item))
            itemName = "StoryModFrequencies";

        // We don't need to track hints for items that aren't on the tracker
        if (!ItemEntries.ContainsKey(itemName))
        {
            APRandomizer.OWMLModConsole.WriteLine($"{itemName} is not an item in the inventory, so skipping", OWML.Common.MessageType.Warning);
            return;
        }
        string findingGame = session.Players.GetPlayerInfo(hint.FindingPlayer).Game;
        string hintedLocation = session.Locations.GetLocationNameFromId(hint.LocationId, findingGame);
        string hintedWorld = session.Players.GetPlayerName(hint.FindingPlayer);
        string hintedEntrance = hint.Entrance;
        ItemEntries[itemName].AddHint(hintedLocation, hintedWorld, hintedEntrance);
    }

    public static void ClearAllHints()
    {
        foreach (InventoryItemEntry entry in ItemEntries.Values)
        {
            entry.Hints.Clear();
        }
    }
}
