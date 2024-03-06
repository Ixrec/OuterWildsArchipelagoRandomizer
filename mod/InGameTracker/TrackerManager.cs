﻿using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ArchipelagoRandomizer.InGameTracker
{
    public class TrackerManager : MonoBehaviour
    {
        public TrackerLogic logic;

        // Tuples: name, green arrow, green exclamation point, orange asterisk
        public List<Tuple<string, bool, bool, bool>> InventoryItems;
        public List<Tuple<string, bool, bool, bool>> CurrentLocations;

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

            // Signalscope frequencies. The individual signals are listed within each frequency entry.
            new InventoryItemEntry("FrequencyOWV", "Frequency: Outer Wilds Ventures"),
            new InventoryItemEntry(Item.FrequencyDB, "Frequency: Distress Beacons"),
            new InventoryItemEntry(Item.FrequencyQF, "Frequency: Quantum Fluctuations"),
            new InventoryItemEntry(Item.FrequencyHS, "Frequency: Hide and Seek"),

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
        public Dictionary<string, InventoryItemEntry> ItemEntries = _ItemEntries.ToDictionary(entry => entry.ID, entry => entry);

        /// <summary>
        /// List of all locations and associated info for the currently selected category in the tracker
        /// </summary>
        public Dictionary<string, TrackerInfo> Infos;

        // Location checklist data for each area
        public Dictionary<string, TrackerChecklistData> HGTLocations;
        public Dictionary<string, TrackerChecklistData> THLocations;
        public Dictionary<string, TrackerChecklistData> BHLocations;
        public Dictionary<string, TrackerChecklistData> GDLocations;
        public Dictionary<string, TrackerChecklistData> DBLocations;
        public Dictionary<string, TrackerChecklistData> OWLocations;

        private ICustomShipLogModesAPI api;
        private TrackerInventoryMode inventoryMode;
        //private TrackerSelectionMode selectionMode;
        private TrackerLocationChecklistMode checklistMode;
        private ArchipelagoSession session;

        private void Awake()
        {
            api = APRandomizer.Instance.ModHelper.Interaction.TryGetModApi<ICustomShipLogModesAPI>("dgarro.CustomShipLogModes");
            if (api == null)
            {
                APRandomizer.OWMLWriteLine("Custom Ship Log Modes API not found! Make sure the mod is correctly installed. Tracker will not function.", OWML.Common.MessageType.Error);
                return;
            }

            inventoryMode = gameObject.AddComponent<TrackerInventoryMode>();
            checklistMode = gameObject.AddComponent <TrackerLocationChecklistMode>();

            LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
            {
                if (loadScene == OWScene.SolarSystem) AddModes();
            };

            logic = new();
        }

        private void Start()
        {
            APRandomizer.OnSessionOpened += (s) =>
            {
                session = s;
                logic.previouslyObtainedItems = s.Items.AllItemsReceived;
                logic.InitializeAccessibility();
                s.Items.ItemReceived += logic.RecheckAccessibility;
                s.Locations.CheckedLocationsUpdated += logic.CheckLocations;
                logic.CheckLocations(s.Locations.AllLocationsChecked);
                s.DataStorage.TrackHints(ReadHints);
            };
            APRandomizer.OnSessionClosed += (s, m) =>
            {
                if (s != null)
                {
                    foreach (InventoryItemEntry entry in ItemEntries.Values)
                    {
                        entry.Hints.Clear();
                    }
                }
                else APRandomizer.OWMLWriteLine("Ran session cleanup, but no session was found", OWML.Common.MessageType.Warning);
            };
            logic.ParseLocations();
        }
        
        /// <summary>
        /// Adds the custom modes for the Ship Log
        /// </summary>
        public void AddModes()
        {
            // Retrive hints from server and set up subscription to hint events in the future
            CheckInventory();
            api.AddMode(inventoryMode, () => true, () => "AP Inventory");
            api.ItemListMake(true, true, itemList =>
            {
                inventoryMode.Wrapper = new(api, itemList);
                inventoryMode.RootObject = itemList.gameObject;
            });
            inventoryMode.Tracker = this;

            api.AddMode(checklistMode, () => true, () => "AP Checklist");
            api.ItemListMake(false, false, itemList =>
            {
                checklistMode.SelectionWrapper = new ItemListWrapper(api, itemList);
                checklistMode.SelectionRootObject = itemList.gameObject;
            });
            api.ItemListMake(true, true, itemList =>
            {
                checklistMode.ChecklistWrapper = new ItemListWrapper(api, itemList);
                checklistMode.ChecklistRootObject = itemList.gameObject;
            });
            checklistMode.Tracker = this;

        }

        // Reads hints from the AP server
        private void ReadHints(Hint[] hintList)
        {
            foreach (Hint hint in hintList)
            {
                // hints for items that belong to your world
                if (hint.ReceivingPlayer == session.ConnectionInfo.Slot)
                {
                    // We don't care about hints for items that have already been found
                    if (hint.Found) continue;

                    string itemName = ItemNames.archipelagoIdToItem[hint.ItemId].ToString();
                    // We don't need to track hints for items that aren't on the tracker
                    if (!ItemEntries.ContainsKey(itemName))
                    {
                        APRandomizer.OWMLWriteLine($"{itemName} is not an item in the inventory, so skipping", OWML.Common.MessageType.Warning);
                        continue;
                    }
                    string hintedLocation = session.Locations.GetLocationNameFromId(hint.LocationId);
                    string hintedWorld = session.Players.GetPlayerName(hint.FindingPlayer);
                    string hintedEntrance = hint.Entrance;
                    ItemEntries[itemName].AddHint(hintedLocation, hintedWorld, hintedEntrance);
                }
                // hints for items placed in your world
                if (hint.FindingPlayer == session.ConnectionInfo.Slot)
                {
                    logic.ApplyHint(hint, session);
                }
            }
        }

        #region Inventory
        // Determines what items the player has and shows them in the inventory mode
        public void CheckInventory()
        {
            Dictionary<Item, uint> items = APRandomizer.SaveData.itemsAcquired;

            InventoryItems = [];
            foreach (InventoryItemEntry item in ItemEntries.Values)
            {
                if (Enum.TryParse(item.ID, out Item subject))
                {
                    uint quantity = items.ContainsKey(subject) ? items[subject] : 0;

                    // The three marshmallow items are treated as a single "Marshmallow" entry by the tracker
                    if (subject == Item.Marshmallow)
                        quantity += items[Item.BurntMarshmallow] + items[Item.PerfectMarshmallow];

                    // Produce a string like "[X] Launch Codes" or "[5] Marshmallow"
                    bool couldHaveMultiple = item.ApItem >= Item.OxygenCapacityUpgrade; // see comments in Item enum
                    string countText = couldHaveMultiple ? quantity.ToString() : (quantity != 0 ? "X" : " "); // only unique items use X
                    string itemName = $"[{countText}] {item.Name}";

                    // Tuple: name, green arrow, green exclamation point, orange asterisk
                    InventoryItems.Add(new Tuple<string, bool, bool, bool>(itemName, false, item.ItemIsNew, item.Hints.Any()));
                }
                else if (item.ID == "FrequencyOWV")
                {
                    string itemName = "[X] Frequency: Outer Wilds Ventures";
                    InventoryItems.Add(new Tuple<string, bool, bool, bool>(itemName, false, item.ItemIsNew, false));
                }
                else
                {
                    APRandomizer.OWMLWriteLine($"Tried to parse {item} as an Item enum, but it was invalid. Unable to determine if the item is in the inventory.", OWML.Common.MessageType.Error);
                }

            }
        }

        /// <summary>
        /// Gets the requested texture from the InGameTracker\Icons folder. Thanks xen!
        /// </summary>
        /// <param name="filename">The name of the file, do not include extension or path</param>
        /// <returns></returns>
        public static Sprite GetSprite(string filename)
        {
            try
            {
                string path = Path.Combine([APRandomizer.Instance.ModHelper.Manifest.ModFolderPath, "InGameTracker", "Icons", filename + ".png"]);

                byte[] data = null;
                if(File.Exists(path)) 
                {
                    data = File.ReadAllBytes(path);
                }
                else
                {
                    APRandomizer.OWMLWriteLine($"Unable to find the texture requested at {path}.", OWML.Common.MessageType.Error);
                    return null;
                }
                Texture2D tex = new(512, 512, TextureFormat.RGBA32, false);
                tex.LoadImage(data);

                var rect = new Rect(0, 0, tex.width, tex.height);
                var pivot = new Vector2(0.5f, 0.5f);

                return Sprite.Create(tex, rect, pivot);
            }
            catch(Exception e)
            {
                APRandomizer.OWMLWriteLine("Unable to load provided texture: " + e.Message, OWML.Common.MessageType.Error);
                return null;
            }
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

            string itemID = item.ToString();
            TrackerManager tracker = APRandomizer.Tracker;
            if (ItemNames.itemToSignal.ContainsKey(item))
            {
                if (TryGetFrequency(item, out string frequency))
                {
                    if (!tracker.ItemEntries.ContainsKey(frequency))
                    {
                        APRandomizer.OWMLWriteLine($"Invalid frequency {frequency} requested to be marked as new! Skipping", OWML.Common.MessageType.Warning);
                        return;
                    }
                    tracker.ItemEntries[frequency].SetNew(true);
                }
                else APRandomizer.OWMLWriteLine($"Provided signal {itemID} does not belong to any mapped frequency, cannot mark as new", OWML.Common.MessageType.Warning);
            }
            else if (tracker.ItemEntries.ContainsKey(itemID))
            {
                if (!tracker.ItemEntries.ContainsKey(itemID))
                {
                    APRandomizer.OWMLWriteLine($"Invalid item {itemID} requested to be marked as new! Skipping", OWML.Common.MessageType.Warning);
                    return;
                }
                tracker.ItemEntries[itemID].SetNew(true);
            }
            else APRandomizer.OWMLWriteLine($"Item received is {itemID}, which does not exist in the inventory. Skipping.", OWML.Common.MessageType.Warning);
        }

        /// <summary>
        /// Returns the frequency that the signal belongs to.
        /// If you need to get the enum entry, you can just Enum.TryParse(GetFrequency(signal), out Item signalItem).
        /// </summary>
        /// <param name="signal"></param>
        /// <returns></returns>
        public static bool TryGetFrequency(Item signal, out string frequency)
        {
            if (signal == Item.SignalChert || signal == Item.SignalEsker || signal == Item.SignalRiebeck || signal == Item.SignalGabbro || signal == Item.SignalFeldspar)
            {
                frequency = "FrequencyOWV";
                return true;
            }
            else if (signal == Item.SignalCaveShard || signal == Item.SignalGroveShard || signal == Item.SignalIslandShard || signal == Item.SignalMuseumShard || signal == Item.SignalTowerShard || signal == Item.SignalQM)
            {
                frequency = Item.FrequencyQF.ToString();
                return true;
            }
            else if (signal == Item.SignalEP1 || signal == Item.SignalEP2 || signal == Item.SignalEP3)
            {
                frequency = Item.FrequencyDB.ToString();
                return true;
            }
            else if (signal == Item.SignalGalena || signal == Item.SignalTephra)
            {
                frequency = Item.FrequencyHS.ToString();
                return true;
            }
            frequency = "";
            return false;
        }
        #endregion

        #region Tracker
        public void GenerateLocationChecklist(TrackerCategory category)
        {
            CurrentLocations = new();
            Dictionary<string, TrackerChecklistData> checklistDatas = logic.GetLocationChecklist(category);
            foreach (TrackerInfo info in Infos.Values)
            {
                // TODO add hints and confirmation of checked locations
                if (Enum.TryParse<Location>(info.locationModID, out Location loc))
                {
                    if (!LocationNames.locationToArchipelagoId.ContainsKey(loc))
                    {
                        APRandomizer.OWMLWriteLine($"Unable to find Location {loc}!", OWML.Common.MessageType.Warning);
                        continue;
                    }
                    if (!checklistDatas.ContainsKey(logic.GetLocationByName(info).name))
                    {
                        APRandomizer.OWMLWriteLine($"Unable to find the location {logic.GetLocationByName(info).name} in the given checklist!", OWML.Common.MessageType.Error);
                        continue;
                    }
                    TrackerChecklistData data = checklistDatas[logic.GetLocationByName(info).name];
                    long id = LocationNames.locationToArchipelagoId[loc];
                    bool locationChecked = data.hasBeenChecked;
                    string name = logic.GetLocationByID(id).name;
                    // Shortens the display name by removing "Ship Log", the region prefix, and the colon from the name
                    name = Regex.Replace(name, ".*:.{1}", "");

                    string colorTag;
                    if (locationChecked) colorTag = "white";
                    else if (data.isAccessible) colorTag = "lime";
                    else colorTag = "red";

                    CurrentLocations.Add(new($"<color={colorTag}>[{(locationChecked ? "X" : " ")}] {name}</color>", false, false, !string.IsNullOrEmpty(data.hintText)));
                }
                else
                {
                    APRandomizer.OWMLWriteLine($"Unable to find location {info.locationModID} for the checklist! Skipping.", OWML.Common.MessageType.Warning);
                }
            }
        }
        #endregion
    }
}