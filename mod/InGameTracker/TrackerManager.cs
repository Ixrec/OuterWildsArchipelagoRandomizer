using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ArchipelagoRandomizer.InGameTracker
{
    public class TrackerManager : MonoBehaviour
    {
        // Tuples: name, green arrow, green exclamation point, orange asterisk
        public List<Tuple<string, bool, bool, bool>> InventoryItems;
        public List<Tuple<string, bool, bool, bool>> CurrentLocations;
        /// <summary>
        /// Parsed version of locations.jsonc
        /// </summary>
        public Dictionary<string, TrackerLocationData> TrackerLocations;

        // This dictionary is the list of items in the Inventory Mode
        // They'll also display in this order, with the second string as the visible name
        // The ID being both the key and the the first value in the InventoryItemEntry is intentional redundancy for cleaner code
        public Dictionary<string, InventoryItemEntry> ItemEntries = new()
        {
            {Item.Coordinates.ToString(), new InventoryItemEntry(Item.Coordinates.ToString(), "Eye of the Universe Coordinates") },
            {Item.LaunchCodes.ToString(), new InventoryItemEntry(Item.LaunchCodes.ToString(), "Launch Codes") },
            {Item.Translator.ToString(), new InventoryItemEntry(Item.Translator.ToString(), "Translator") },
            {Item.Signalscope.ToString(), new InventoryItemEntry(Item.Signalscope.ToString(), "Signalscope") },
            {Item.EntanglementRule.ToString(), new InventoryItemEntry(Item.EntanglementRule.ToString(), "Suit Lights Controls") },
            {Item.ElectricalInsulation.ToString(), new InventoryItemEntry(Item.ElectricalInsulation.ToString(), "Electrical Insulation") },
            {Item.SilentRunning.ToString(), new InventoryItemEntry(Item.SilentRunning.ToString(), "Silent Running Mode") },
            {Item.TornadoAdjustment.ToString(), new InventoryItemEntry(Item.TornadoAdjustment.ToString(), "Tornado Aerodynamic Adjustments") },
            {Item.Scout.ToString(), new InventoryItemEntry(Item.Scout.ToString(), "Camera: Scout Launcher") },
            {Item.CameraGM.ToString(), new InventoryItemEntry(Item.CameraGM.ToString(), "Camera: Ghost Matter Frequency") },
            {Item.CameraQuantum.ToString(), new InventoryItemEntry(Item.CameraQuantum.ToString(), "Camera: Quantum Objects") },
            {Item.WarpPlatformCodes.ToString(), new InventoryItemEntry(Item.WarpPlatformCodes.ToString(), "Nomai: Warp Platform Codes") },
            {Item.WarpCoreManual.ToString(), new InventoryItemEntry(Item.WarpCoreManual.ToString(), "Nomai: Warp Core Installation Manual") },
            {Item.ShrineDoorCodes.ToString(), new InventoryItemEntry(Item.ShrineDoorCodes.ToString(), "Nomai: Shrine Door Codes") },
            {"FrequencyOWV", new InventoryItemEntry("FrequencyOWV", "Frequency: Outer Wilds Ventures", false) },
            {Item.FrequencyDB.ToString(), new InventoryItemEntry(Item.FrequencyDB.ToString(), "Frequency: Distress Beacons") },
            {Item.FrequencyQF.ToString(), new InventoryItemEntry(Item.FrequencyQF.ToString(), "Frequency: Quantum Fluctuations") },
            {Item.FrequencyHS.ToString(), new InventoryItemEntry(Item.FrequencyHS.ToString(), "Frequency: Hide and Seek") }
        };

        /// <summary>
        /// List of all locations and associated info for the currently selected category in the tracker
        /// </summary>
        public Dictionary<string, TrackerInfo> Infos;

        private ICustomShipLogModesAPI api;
        private TrackerInventoryMode inventoryMode;
        private TrackerLocationChecklistMode checklistMode;
        private ArchipelagoSession session;

        private void Awake()
        {
            api = APRandomizer.Instance.ModHelper.Interaction.TryGetModApi<ICustomShipLogModesAPI>("dgarro.CustomShipLogModes");
            if (api != null )
            {
                APRandomizer.OWMLModConsole.WriteLine("Custom Ship Log Modes API found!", OWML.Common.MessageType.Success);
            }
            else
            {
                APRandomizer.OWMLModConsole.WriteLine("Custom Ship Log Modes API not found! Make sure the mod is correctly installed. Tracker will not function.", OWML.Common.MessageType.Error);
                return;
            }

            inventoryMode = gameObject.AddComponent<TrackerInventoryMode>();
            checklistMode = gameObject.AddComponent <TrackerLocationChecklistMode>();

            LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
            {
                if (loadScene == OWScene.SolarSystem) AddModes();
            };
        }

        private void Start()
        {
            APRandomizer.OnSessionOpened += (s) =>
            {
                session = s;
                ReadHints(s.DataStorage.GetHints());
                s.DataStorage.TrackHints(ReadHints);
                APRandomizer.OWMLModConsole.WriteLine("Session opened!", OWML.Common.MessageType.Success);
            };
            APRandomizer.OnSessionClosed += (s, m) =>
            {
                if (s != null)
                {
                    APRandomizer.OWMLModConsole.WriteLine("Session closed!", OWML.Common.MessageType.Success);
                    foreach (InventoryItemEntry entry in ItemEntries.Values)
                    {
                        entry.SetHints("", "");
                    }
                }
                else APRandomizer.OWMLModConsole.WriteLine("Ran session cleanup, but no session was found", OWML.Common.MessageType.Warning);
            };
        }
        
        /// <summary>
        /// Adds the custom modes for the Ship Log
        /// </summary>
        public void AddModes()
        {
            APRandomizer.OWMLModConsole.WriteLine("Creating Tracker Mode...", OWML.Common.MessageType.Info);

            // Retrive hints from server and set up subscription to hint events in the future
            CheckInventory();
            api.AddMode(inventoryMode, () => true, () => "AP Inventory");
            api.ItemListMake(true, true, itemList =>
            {
                inventoryMode.Wrapper = new(api, itemList);
                inventoryMode.RootObject = itemList.gameObject;
            });
            inventoryMode.Tracker = this;

            api.AddMode(checklistMode, () => true, () => "Tracker");
            api.ItemListMake(true, true, itemList =>
            {
                checklistMode.Wrapper = new ItemListWrapper(api, itemList);
                checklistMode.RootObject = itemList.gameObject;
            });
            checklistMode.Tracker = this;
        }

        // Reads hints from the AP server
        private void ReadHints(Hint[] hintList)
        {
            APRandomizer.OWMLModConsole.WriteLine($"Received {hintList.Length} hints!", OWML.Common.MessageType.Info);
            foreach (Hint hint in hintList)
            {
                // We only care about hints for the current world
                // Probably change this later once location hinting is implemented
                if (hint.ReceivingPlayer != session.ConnectionInfo.Slot) continue; 
                string itemName = ItemNames.archipelagoIdToItem[hint.ItemId].ToString();
                APRandomizer.OWMLModConsole.WriteLine($"Received a hint for item {itemName}", OWML.Common.MessageType.Success);
                // We don't need to track hints for items that aren't on the tracker
                if (!ItemEntries.ContainsKey(itemName))
                {
                    APRandomizer.OWMLModConsole.WriteLine($"...but it's not an item in the inventory, so skipping", OWML.Common.MessageType.Warning);
                    continue;
                }
                string hintedLocation = session.Locations.GetLocationNameFromId(hint.LocationId);
                string hintedWorld = session.Players.GetPlayerName(hint.FindingPlayer);
                string hintedEntrance = hint.Entrance;
                ItemEntries[itemName].SetHints(hintedLocation, hintedWorld, hintedEntrance);
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
                    uint quantity = items[subject];
                    string itemName = $"[{(quantity != 0 ? "X" : " ")}] {item.Name}"; // Would produce a string like "[X] Launch Codes"
                    bool hasHint = quantity == 0 && item.HintedLocation != "";
                    // Tuple: name, green arrow, green exclamation point, orange asterisk
                    InventoryItems.Add(new Tuple<string, bool, bool, bool>(itemName, false, item.ItemIsNew, hasHint));
                }
                else if (item.ID == "FrequencyOWV")
                {
                    string itemName = "[X] Frequency: Outer Wilds Ventures";
                    InventoryItems.Add(new Tuple<string, bool, bool, bool>(itemName, false, item.ItemIsNew, false));
                }
                else
                {
                    APRandomizer.OWMLModConsole.WriteLine($"Tried to parse {item} as an Item enum, but it was invalid. Unable to determine if the item is in the inventory.", OWML.Common.MessageType.Error);
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
                    APRandomizer.OWMLModConsole.WriteLine($"Unable to find the texture requested at {path}.", OWML.Common.MessageType.Error);
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
                APRandomizer.OWMLModConsole.WriteLine("Unable to load provided texture: " + e.Message, OWML.Common.MessageType.Error);
                return null;
            }
        }

        /// <summary>
        /// Sets an item as new, so it'll have a green exclamation point in the inventory
        /// </summary>
        /// <param name="item"></param>
        public static void MarkItemAsNew(Item item)
        {
            string itemID = item.ToString();
            TrackerManager tracker = APRandomizer.Tracker;
            if (itemID.Contains("Signal"))
            {
                if (TryGetFrequency(item, out string frequency))
                {
                    if (!tracker.ItemEntries.ContainsKey(frequency))
                    {
                        APRandomizer.OWMLModConsole.WriteLine($"Invalid frequency {frequency} requested to be marked as new! Skipping", OWML.Common.MessageType.Warning);
                        return;
                    }
                    tracker.ItemEntries[frequency].SetNew(true);
                    APRandomizer.OWMLModConsole.WriteLine($"Marking frequency {frequency} for {itemID} as new", OWML.Common.MessageType.Success);
                }
                else APRandomizer.OWMLModConsole.WriteLine($"Provided signal {itemID} does not belong to any mapped frequency, cannot mark as new", OWML.Common.MessageType.Warning);
            }
            else if (tracker.ItemEntries.ContainsKey(itemID))
            {
                if (!tracker.ItemEntries.ContainsKey(itemID))
                {
                    APRandomizer.OWMLModConsole.WriteLine($"Invalid item {itemID} requested to be marked as new! Skipping", OWML.Common.MessageType.Warning);
                    return;
                }
                tracker.ItemEntries[itemID].SetNew(true);
                APRandomizer.OWMLModConsole.WriteLine($"Marking item {itemID} as new", OWML.Common.MessageType.Success);
            }
            else APRandomizer.OWMLModConsole.WriteLine($"Item received is {itemID}, which does not exist in the inventory. Skipping.", OWML.Common.MessageType.Warning);
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
        public void GenerateLocationChecklist()
        {
            CurrentLocations = new();
            foreach (TrackerInfo info in Infos.Values)
            {
                // TODO add hints and confirmation of checked locations
                CurrentLocations.Add(new($"[ ] {info.locationModID}", false, false, false));
            }
        }

        public TrackerLocationData GetLocationByID(int id)
        {
            return TrackerLocations.Values.FirstOrDefault((x) => x.address == id);
        }

        // parses the locations.jsonc file
        private void ParseLocations()
        {
            string path = APRandomizer.Instance.ModHelper.Manifest.ModFolderPath + "/locations.jsonc";
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                JObject obj = JObject.Parse(json);







                List<TrackerLocationData> locations = JsonConvert.DeserializeObject<List<TrackerLocationData>>(File.ReadAllText(path));
                // index the locations for faster searching
                foreach (TrackerLocationData location in locations)
                {
                    TrackerLocations.Add(location.name, location);
                }
            }
            else
            {
                APRandomizer.OWMLModConsole.WriteLine($"Could not find the file at {path}!", OWML.Common.MessageType.Error);
            }
        }
        #endregion
    }
}