using Archipelago.MultiClient.Net.Models;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ArchipelagoRandomizer.InGameTracker
{
    public class TrackerManager : MonoBehaviour
    {
        public List<Tuple<string, bool, bool, bool>> InventoryItems;
        public Dictionary<string, bool> NewItems;

        /// <summary>
        /// A list of hints received from the AP server. In order, the strings are the item name, the location, and the world.
        /// </summary>
        public Dictionary<string, Tuple<string, string>> Hints;

        // This dictionary is the list of items in the Inventory Mode
        // They'll also display in this order, with the second string as the visible name
        public readonly Dictionary<string, string> ItemEntries = new()
        {
            {Item.Coordinates.ToString(), "Eye of the Universe Coordinates" },
            {Item.LaunchCodes.ToString(), "Launch Codes"},
            {Item.Translator.ToString(), "Translator" },
            {Item.Signalscope.ToString(), "Signalscope" },
            {Item.EntanglementRule.ToString(), "Suit Lights Controls" },
            {Item.ElectricalInsulation.ToString(), "Electrical Insulation" },
            {Item.SilentRunning.ToString(), "Silent Running Mode" },
            {Item.TornadoAdjustment.ToString(), "Tornado Aerodynamic Adjustments" },
            {Item.Scout.ToString(), "Camera: Scout Launcher" },
            {Item.CameraGM.ToString(), "Camera: Ghost Matter Frequency" },
            {Item.CameraQuantum.ToString(), "Camera: Quantum Objects" },
            {Item.WarpPlatformCodes.ToString(), "Nomai: Warp Platform Codes" },
            {Item.WarpCoreManual.ToString(), "Nomai: Warp Core Installation Manual" },
            {Item.ShrineDoorCodes.ToString(), "Nomai: Shrine Door Codes" },
            {"FrequencyOWV", "Frequency: Outer Wilds Ventures" },
            {Item.FrequencyDB.ToString(), "Frequency: Distress Beacons" },
            {Item.FrequencyQF.ToString(), "Frequency: Quantum Fluctuations" },
            {Item.FrequencyHS.ToString(), "Frequency: Hide and Seek" }
        };

        private ICustomShipLogModesAPI api;
        private TrackerInventoryMode inventoryMode;

        private void Awake()
        {
            api = Randomizer.Instance.ModHelper.Interaction.TryGetModApi<ICustomShipLogModesAPI>("dgarro.CustomShipLogModes");
            if (api != null )
            {
                Randomizer.OWMLModConsole.WriteLine("Custom Ship Log Modes API found!", OWML.Common.MessageType.Success);
            }
            else
            {
                Randomizer.OWMLModConsole.WriteLine("Custom Ship Log Modes API not found! Make sure the mod is correctly installed. Tracker will not function.", OWML.Common.MessageType.Error);
                return;
            }

            inventoryMode = gameObject.AddComponent<TrackerInventoryMode>();
            LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
            {
                if (loadScene == OWScene.SolarSystem) AddModes();
            };

            NewItems = new();
            foreach (string item in ItemEntries.Keys)
            {
                NewItems.Add(item, false);
            }
        }
        
        /// <summary>
        /// Adds the custom modes for the Ship Log
        /// </summary>
        public void AddModes()
        {
            Randomizer.OWMLModConsole.WriteLine("Creating Tracker Mode...", OWML.Common.MessageType.Info);

            CheckInventory();
            api.AddMode(inventoryMode, () => true, () => "AP Inventory");
            api.ItemListMake(true, true, itemList =>
            {
                inventoryMode.Wrapper = new(api, itemList);
                inventoryMode.RootObject = itemList.gameObject;
            });
            inventoryMode.Tracker = this;
        }

        // Reads hints from the AP server
        private void ReadHints()
        {
            Hint[] hintList = Randomizer.APSession.DataStorage.GetHints();
            Hints = new();
            var session = Randomizer.APSession;
            foreach (Hint hint in hintList)
            {
                string itemName = ItemNames.archipelagoIdToItem[hint.ItemId].ToString();
                string location = session.Locations.GetLocationNameFromId(hint.LocationId);
                string player = session.Players.GetPlayerName(hint.FindingPlayer);
                Hints.Add(itemName, new Tuple<string, string>(location, player));
            }
        }

        // Determines what items the player has and shows them in the inventory mode
        public void CheckInventory()
        {
            ReadHints();
            Dictionary<Item, uint> items = Randomizer.SaveData.itemsAcquired;

            InventoryItems = new();
            foreach (string key in ItemEntries.Keys)
            {
                if (Enum.TryParse(key, out Item subject))
                {
                    uint quantity = items[subject];
                    string itemName = $"[{(quantity != 0 ? "X" : " ")}] {ItemEntries[key]}"; // Would produce a string like "[X] Launch Codes"
                    bool hasHint = quantity == 0 && Hints.ContainsKey(key);
                    // Tuple: name, green arrow, green exclamation point, orange asterisk
                    InventoryItems.Add(new Tuple<string, bool, bool, bool>(itemName, false, NewItems[key], hasHint));
                }
                else if (key == "FrequencyOWV")
                {
                    string itemName = "[X] Frequency: Outer Wilds Ventures";
                    InventoryItems.Add(new Tuple<string, bool, bool, bool>(itemName, false, NewItems["FrequencyOWV"], false));
                }
                else
                {
                    Randomizer.OWMLModConsole.WriteLine($"Tried to parse {key} as an Item enum, but it was invalid. Unable to determine if the item is in the inventory.", OWML.Common.MessageType.Error);
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
                string path = Path.Combine(new string[] { Randomizer.Instance.ModHelper.Manifest.ModFolderPath, "InGameTracker", "Icons", filename + ".png"});

                byte[] data = null;
                if(File.Exists(path)) 
                {
                    data = File.ReadAllBytes(path);
                }
                else
                {
                    Randomizer.OWMLModConsole.WriteLine($"Unable to find the texture requested at {path}.", OWML.Common.MessageType.Error);
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
                Randomizer.OWMLModConsole.WriteLine("Unable to load provided texture: " + e.Message, OWML.Common.MessageType.Error);
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
            TrackerManager tracker = Randomizer.Tracker;
            if (!itemID.Contains("Signal"))
            {
                tracker.NewItems[itemID] = true;
            }
            else
            {
                if (item == Item.SignalEsker || item == Item.SignalChert || item == Item.SignalRiebeck || item == Item.SignalGabbro || item == Item.SignalFeldspar)
                {
                    tracker.NewItems["FrequencyOWV"] = true;
                }
                else if (item == Item.SignalCaveShard || item == Item.SignalGroveShard || item == Item.SignalIslandShard || item == Item.SignalMuseumShard || item == Item.SignalTowerShard || item == Item.SignalQM)
                {
                    tracker.NewItems[Item.FrequencyQF.ToString()] = true;
                }
                else if (item == Item.SignalEP1 || item == Item.SignalEP2 || item == Item.SignalEP3)
                {
                    tracker.NewItems[Item.FrequencyDB.ToString()] = true;
                }
                else if (item == Item.SignalGalena || item == Item.SignalTephra)
                {
                    tracker.NewItems[Item.FrequencyHS.ToString()] = true;
                }
            }
        }
    }
}