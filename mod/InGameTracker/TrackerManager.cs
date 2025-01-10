using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ArchipelagoRandomizer.InGameTracker;

public class TrackerManager : MonoBehaviour
{
    public Logic logic;

    private ICustomShipLogModesAPI shipApi;
    private ISuitLogAPI suitApi;
    private APInventoryMode shipInventoryMode;
    private APInventoryMode suitInventoryMode;
    //private TrackerSelectionMode selectionMode;
    private APChecklistMode shipChecklistMode;
    private APChecklistMode suitChecklistMode;
    private ArchipelagoSession session;

    private void Awake()
    {
        shipApi = APRandomizer.Instance.ModHelper.Interaction.TryGetModApi<ICustomShipLogModesAPI>("dgarro.CustomShipLogModes");
        if (shipApi == null)
        {
            APRandomizer.OWMLModConsole.WriteLine("Custom Ship Log Modes API not found! Make sure the mod is correctly installed. Tracker will not function.", OWML.Common.MessageType.Error);
            return;
        }

        shipInventoryMode = gameObject.AddComponent<APInventoryMode>();
        shipChecklistMode = gameObject.AddComponent<APChecklistMode>();

        // Optional dependency on SuitLog
        suitApi = APRandomizer.Instance.ModHelper.Interaction.TryGetModApi<ISuitLogAPI>("dgarro.SuitLog");
        if (suitApi != null) {
            suitInventoryMode = gameObject.AddComponent<APInventoryMode>();
            suitChecklistMode = gameObject.AddComponent<APChecklistMode>();
        }

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
                APInventoryMode.ClearAllHints();
            else APRandomizer.OWMLModConsole.WriteLine("Ran session cleanup, but no session was found", OWML.Common.MessageType.Warning);
        };
        logic.ParseLocations();
    }
    
    /// <summary>
    /// Adds the custom modes for the Ship Log
    /// </summary>
    public void AddModes()
    {
        shipApi.AddMode(shipInventoryMode, () => true, () => "AP Inventory");
        shipApi.ItemListMake(true, true, itemList =>
        {
            shipInventoryMode.Wrapper = new ShipLogItemListWrapper(shipApi, itemList);
            shipInventoryMode.RootObject = itemList.gameObject;
        });
        shipInventoryMode.Tracker = this;

        shipApi.AddMode(shipChecklistMode, () => true, () => "AP Checklist");
        shipApi.ItemListMake(false, false, itemList =>
        {
            shipChecklistMode.SelectionWrapper = new ShipLogItemListWrapper(shipApi, itemList);
            shipChecklistMode.SelectionRootObject = itemList.gameObject;
        });
        shipApi.ItemListMake(true, true, itemList =>
        {
            shipChecklistMode.ChecklistWrapper = new ShipLogItemListWrapper(shipApi, itemList);
            shipChecklistMode.ChecklistRootObject = itemList.gameObject;
        });
        shipChecklistMode.Tracker = this;

        if (suitApi != null) {
            suitApi.AddMode(suitInventoryMode, () => true, () => "AP Inventory");
            suitApi.ItemListMake(itemList =>
            {
                Coordinates.EnsureShipLogCoordsSpriteCreated();
                SuitLogItemListWrapper wrapper = new SuitLogItemListWrapper(suitApi, itemList);
                wrapper.DescriptionFieldOpen();
                suitInventoryMode.Wrapper = wrapper;
                suitInventoryMode.RootObject = itemList.gameObject;
            });
            suitInventoryMode.Tracker = this;

            suitApi.AddMode(suitChecklistMode, () => true, () => "AP Checklist");
            suitApi.ItemListMake(itemList =>
            {
                suitChecklistMode.SelectionWrapper = new SuitLogItemListWrapper(suitApi, itemList);
                suitChecklistMode.SelectionRootObject = itemList.gameObject;
            });
            suitApi.ItemListMake(itemList =>
            {
                Coordinates.EnsureShipLogCoordsSpriteCreated();
                SuitLogItemListWrapper wrapper = new SuitLogItemListWrapper(suitApi, itemList);
                wrapper.DescriptionFieldOpen();
                suitChecklistMode.ChecklistWrapper = wrapper;
                suitChecklistMode.ChecklistRootObject = itemList.gameObject;
            });
            suitChecklistMode.Tracker = this;
        }
    }

    // Reads hints from the AP server
    private void ReadHints(Hint[] hintList)
    {
        foreach (Hint hint in hintList)
        {
            // hints for items that belong to your world
            if (hint.ReceivingPlayer == session.ConnectionInfo.Slot)
                APInventoryMode.AddHint(hint, session);

            // hints for items placed in your world
            if (hint.FindingPlayer == session.ConnectionInfo.Slot)
                AddHintToChecklistModeDescriptions(hint, session);
        }
    }

    /// <summary>
    /// Reads a hint and applies it to the checklist
    /// </summary>
    /// <param name="hint"></param>
    private void AddHintToChecklistModeDescriptions(Hint hint, ArchipelagoSession session)
    {
        string playerName;
        if (hint.ReceivingPlayer == session.ConnectionInfo.Slot)
        {
            playerName = "your";
        }
        else
        {
            playerName = session.Players.GetPlayerName(hint.ReceivingPlayer) + "'s";
        }
        string itemColor;
        switch (hint.ItemFlags)
        {
            case Archipelago.MultiClient.Net.Enums.ItemFlags.Advancement: itemColor = "#B883B4"; break;
            case Archipelago.MultiClient.Net.Enums.ItemFlags.NeverExclude: itemColor = "#524798"; break;
            case Archipelago.MultiClient.Net.Enums.ItemFlags.Trap: itemColor = "#DA6F62"; break;
            default: itemColor = "#01CACA"; break;
        }
        string receivingGame = session.Players.GetPlayerInfo(hint.ReceivingPlayer).Game;
        string itemName = session.Items.GetItemName(hint.ItemId, receivingGame); // the game name argument is required to work with non-OW items
        string hintDescription = $"It looks like {playerName} <color={itemColor}>{itemName}</color> can be found here";
        TrackerLocationData loc = logic.GetLocationByID(hint.LocationId);
        if (!logic.LocationChecklistData.ContainsKey(loc.name))
        {
            APRandomizer.OWMLModConsole.WriteLine($"ApplyHint was unable to find a checklist data object for {loc.name}!", OWML.Common.MessageType.Error);
            return;
        }
        if (!logic.LocationChecklistData[loc.name].hasBeenChecked)
            logic.LocationChecklistData[loc.name].hintText = hintDescription;
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
            if (!filename.EndsWith(".png"))
                filename = filename + ".png";
            string path = Path.Combine([APRandomizer.Instance.ModHelper.Manifest.ModFolderPath, "InGameTracker", "Icons", filename]);

            byte[] data = null;
            if (File.Exists(path))
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
        catch (Exception e)
        {
            APRandomizer.OWMLModConsole.WriteLine("Unable to load provided texture: " + e.Message, OWML.Common.MessageType.Error);
            return null;
        }
    }
}
