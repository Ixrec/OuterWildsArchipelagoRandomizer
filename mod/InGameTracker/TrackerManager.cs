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

    private ICustomShipLogModesAPI api;
    private APInventoryMode inventoryMode;
    //private TrackerSelectionMode selectionMode;
    private APChecklistMode checklistMode;
    private ArchipelagoSession session;

    private void Awake()
    {
        api = APRandomizer.Instance.ModHelper.Interaction.TryGetModApi<ICustomShipLogModesAPI>("dgarro.CustomShipLogModes");
        if (api == null)
        {
            APRandomizer.OWMLModConsole.WriteLine("Custom Ship Log Modes API not found! Make sure the mod is correctly installed. Tracker will not function.", OWML.Common.MessageType.Error);
            return;
        }

        inventoryMode = gameObject.AddComponent<APInventoryMode>();
        checklistMode = gameObject.AddComponent <APChecklistMode>();

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
        string itemTitle = $"<color={itemColor}>{session.Items.GetItemName(hint.ItemId)}</color>";
        string hintDescription = $"It looks like {playerName} <color={itemColor}>{itemTitle}</color> can be found here";
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
            string path = Path.Combine([APRandomizer.Instance.ModHelper.Manifest.ModFolderPath, "InGameTracker", "Icons", filename + ".png"]);

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