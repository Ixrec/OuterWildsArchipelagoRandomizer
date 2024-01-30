using Archipelago.MultiClient.Net.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        public readonly Dictionary<string, string> ItemEntries = new Dictionary<string, string>
        {
            {"Coordinates", "Eye of the Universe Coordinates" },
            {"LaunchCodes", "Launch Codes"},
            {"Translator", "Translator" },
            {"Signalscope", "Signalscope" },
            {"EntanglementRule", "Suit Lights Controls" },
            {"ElectricalInsulation", "Electrical Insulation" },
            {"SilentRunning", "Silent Running Mode" },
            {"TornadoAdjustment", "Tornado Aerodynamic Adjustments" },
            {"Scout", "Camera: Scout Launcher" },
            {"CameraGM", "Camera: Ghost Matter Frequency" },
            {"CameraQuantum", "Camera: Quantum Objects" },
            {"WarpPlatformCodes", "Nomai: Warp Platform Codes" },
            {"WarpCoreManual", "Nomai: Warp Core Installation Manual" },
            {"ShrineDoorCodes", "Nomai: Shrine Door Codes" },
            {"FrequencyOWV", "Frequency: Outer Wilds Ventures" },
            {"FrequencyDB", "Frequency: Distress Beacons" },
            {"FrequencyQF", "Frequency: Quantum Fluctuations" },
            {"FrequencyHS", "Frequency: Hide and Seek" }
        };

        private ICustomShipLogModesAPI api;
        private TrackerInventoryMode inventoryMode;

        private const string lightGreen = "#9DFCA9";

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

        public void CheckInventory()
        {
            ReadHints();
            Dictionary<Item, uint> items = Randomizer.SaveData.itemsAcquired;

            InventoryItems = new();
            foreach (string key in ItemEntries.Keys)
            {
                Item subject;
                if (Enum.TryParse(key, out subject))
                {
                    uint quantity = items[subject];
                    string itemName = $"[{(quantity != 0 ? "X" : " ")}] {ItemEntries[key]}"; // Would produce a string like "[X] Launch Codes"
                    bool hasHint = quantity == 0 && Hints.ContainsKey(key);
                    // Tuple: name, green arrow, green exclamation point, orange asterisk
                    InventoryItems.Add(new Tuple<string, bool, bool, bool>(itemName, false, NewItems[key], hasHint));
                }
                else if (key == "FrequencyOWV")
                {
                    string itemName = "[X] Outer Wilds Ventures";
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
                Texture2D tex = new Texture2D(512, 512, TextureFormat.RGBA32, false);
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
                    tracker.NewItems["FrequencyQF"] = true;
                }
                else if (item == Item.SignalEP1 || item == Item.SignalEP2 || item == Item.SignalEP3)
                {
                    tracker.NewItems["FrequencyDB"] = true;
                }
                else if (item == Item.SignalGalena || item == Item.SignalTephra)
                {
                    tracker.NewItems["FrequencyHS"] = true;
                }
            }
        }
    }
}