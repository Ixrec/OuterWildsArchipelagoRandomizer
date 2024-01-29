using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static UnityEngine.UI.ContentSizeFitter;

namespace ArchipelagoRandomizer.InGameTracker
{
    public class TrackerManager : MonoBehaviour
    {
        public List<Tuple<string, bool, bool, bool>> InventoryItems;
        public Dictionary<string, bool> NewItems;

        public Dictionary<string, string> ItemEntries = new Dictionary<string, string>
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
                Randomizer.LogSuccess("Custom Ship Log Modes API found!");
            }
            else
            {
                Randomizer.LogWarning("Custom Ship Log Modes API not found! Make sure the mod is correctly installed. Tracker will not function.");
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
            Randomizer.LogInfo("Creating Tracker Mode...");
            CheckInventory();
            api.AddMode(inventoryMode, () => true, () => "AP Inventory");
            api.ItemListMake(true, true, itemList =>
            {
                inventoryMode.Wrapper = new(api, itemList);
                inventoryMode.RootObject = itemList.gameObject;
            });
            inventoryMode.Tracker = this;
        }

        public void CheckInventory()
        {
            Dictionary<Item, uint> items = Randomizer.SaveData.itemsAcquired;

            InventoryItems = new();
            foreach (string key in ItemEntries.Keys)
            {
                Item subject;
                if (Enum.TryParse(key, out subject))
                {
                    uint quantity = items[subject];
                    string itemName = ItemEntries[key];
                    // Tuple: name, green arrow, green exclamation point, orange asterisk
                    InventoryItems.Add(new Tuple<string, bool, bool, bool>(itemName, false, NewItems[key], quantity == 0));
                }
                else
                {
                    Randomizer.LogError($"Tried to parse {key} as an Item enum, but it was invalid. Unable to determine if the item is in the inventory.");
                }

            }

            foreach (Item item in items.Keys) 
            {
                if (ItemEntries.ContainsKey(item.ToString()))
                {
                    InventoryItems = new();
                    foreach (string itemEntry in ItemEntries.Values)
                    {
                        InventoryItems.Add(new Tuple<string, bool, bool, bool>(itemEntry, false, false, false));
                    }
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
                    Randomizer.LogError($"Unable to find the texture requested at {path}.");
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
                Randomizer.LogError("Unable to load provided texture: " + e.Message);
                return null;
            }
        }
    }
}