using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using ArchipelagoRandomizer.InGameTracker;
using HarmonyLib;
using Newtonsoft.Json;
using OWML.Common;
using OWML.ModHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace ArchipelagoRandomizer;

public class APConnectionData
{
    public string hostname;
    public uint port;
    public string slotName;
    public string password;
    public string? roomId;
}
public class APRandomizerSaveData
{
    public APConnectionData apConnectionData;
    public Dictionary<Location, bool> locationsChecked;
    public Dictionary<Item, uint> itemsAcquired;
    public Dictionary<Location, ArchipelagoItem> scoutedLocations;
    public Dictionary<string, string[]> hintsGenerated;
}

public class APRandomizer : ModBehaviour
{
    public static APRandomizer Instance;

    public static APRandomizerSaveData SaveData;
    public static AssetBundle Assets;
    public static string SaveFileName;
    public static ArchipelagoSession APSession;

    public static Dictionary<string, object> SlotData;
    public static bool SlotEnabledLogsanity() =>
        SlotData.ContainsKey("logsanity") && (long)SlotData["logsanity"] > 0;
    public static bool SlotEnabledEotEDLC() =>
        SlotData.ContainsKey("enable_eote_dlc") && (long)SlotData["enable_eote_dlc"] > 0;
    public static bool SlotEnabledDLCOnly() =>
        SlotData.ContainsKey("dlc_only") && (long)SlotData["dlc_only"] > 0;
    public static bool SlotEnabledSplitTranslator() =>
        SlotData.ContainsKey("split_translator") && (long)SlotData["split_translator"] == 1;

    public static IModConsole OWMLModConsole { get => Instance.ModHelper.Console; }
    public static ArchConsoleManager InGameAPConsole;
    public static TrackerManager Tracker;
    public static LocationScouter LocationScouter;
    public static INewHorizons? NewHorizonsAPI = null;

    public static bool IsVanillaSystemLoaded() =>
        LoadManager.GetCurrentScene() == OWScene.SolarSystem &&
        (NewHorizonsAPI == null || NewHorizonsAPI.GetCurrentStarSystem() == "SolarSystem");

    /// <summary>
    /// Runs whenever a new session is created
    /// </summary>
    public static event Action<ArchipelagoSession> OnSessionOpened;
    /// <summary>
    /// Runs whenever a session is closed manually or the application is closed, returns a null session if none is open.
    /// The bool is true if the session was manually closed, false if the session was closed via the application closing.
    /// Note that if the bool is false, the Mod Manager will have lost its connection to the game before this runs, so you'll need to check the log file manually for errors and other logs.
    /// </summary>
    public static event Action<ArchipelagoSession, bool> OnSessionClosed;

    public static bool AutoNomaiText = false;
    public static bool ColorNomaiText = true;
    public static bool InstantTranslator = false;

    public static bool HasSeenSettingsText = false;
    public static bool DisableConsole = false;
    public static bool DisableInGameLocationSending = false;
    private static bool DisableInGameItemReceiving = false;
    public static bool DisableInGameItemApplying = false;
    private static bool DisableInGameSaveFileWrites = false;

    // Throttle save file writes to once per second to avoid IOExceptions for conflicting write attempts
    private static Task pendingSaveFileWrite = null;
    private static DateTimeOffset lastWriteTime = DateTimeOffset.UtcNow;
    public static void WriteToSaveFile()
    {
        if (DisableInGameSaveFileWrites && LoadManager.GetCurrentScene() == OWScene.SolarSystem) return;

        if (pendingSaveFileWrite != null) return;

        if (lastWriteTime < DateTimeOffset.UtcNow.AddSeconds(-1))
        {
            APRandomizer.Instance.ModHelper.Storage.Save<APRandomizerSaveData>(SaveData, SaveFileName);
            lastWriteTime = DateTimeOffset.UtcNow;
        }

        pendingSaveFileWrite = Task.Run(async () =>
        {
            await Task.Delay(1000);

            APRandomizer.Instance.ModHelper.Storage.Save<APRandomizerSaveData>(SaveData, SaveFileName);
            lastWriteTime = DateTimeOffset.UtcNow;

            pendingSaveFileWrite = null;
        });
    }

    private void SetupSaveData()
    {
        var saveDataFolder = ModHelper.Manifest.ModFolderPath + "SaveData";
        if (!Directory.Exists(saveDataFolder))
            Directory.CreateDirectory(saveDataFolder);

        StandaloneProfileManager.SharedInstance.OnProfileReadDone += () =>
        {
            if (StandaloneProfileManager.SharedInstance._currentProfile == null)
            {
                OWMLModConsole.WriteLine($"No profile loaded", MessageType.Error);
                return;
            }
            var profileName = StandaloneProfileManager.SharedInstance._currentProfile.profileName;

            OWMLModConsole.WriteLine($"Profile {profileName} read by the game. Checking for a corresponding AP APRandomizer save file.");

            var fileName = $"SaveData/{profileName}.json";
            if (SaveFileName == fileName && DisableInGameSaveFileWrites)
            {
                OWMLModConsole.WriteLine($"skipping reload of {profileName} save file because the '[DEBUG] Don't Write To Save File In-Game' is in effect, and we don't want to throw away the pending writes");
                return;
            }

            SaveFileName = fileName;
            SaveData = ModHelper.Storage.Load<APRandomizerSaveData>(SaveFileName);
            if (SaveData == null)
            {
                OWMLModConsole.WriteLine($"No save file found for this profile.");
                if (MainMenu.ResumeRandomExpeditionGO != null)
                {
                    MainMenu.ChangeConnInfoGO?.SetActive(false);
                    MainMenu.ResumeRandomExpeditionGO?.SetActive(false);
                }
            }
            else
            {
                OWMLModConsole.WriteLine($"Existing save file loaded. You've checked {SaveData.locationsChecked.Where(kv => kv.Value).Count()} out of {SaveData.locationsChecked.Count} locations " +
                    $"and acquired one or more of {SaveData.itemsAcquired.Where(kv => kv.Value > 0).Count()} different item types out of {SaveData.itemsAcquired.Count} total types.");

                foreach (var kv in SaveData.itemsAcquired)
                    LocationTriggers.ApplyItemToPlayer(kv.Key, kv.Value);

                if (MainMenu.ResumeRandomExpeditionGO != null)
                {
                    MainMenu.ChangeConnInfoGO?.SetActive(true);
                    MainMenu.ResumeRandomExpeditionGO?.SetActive(true);
                }
            }
        };
    }

    private static LoginResult ConnectToAPServer(APConnectionData cdata, Action successCallback)
    {
        OWMLModConsole.WriteLine($"ConnectToAPServer() called with {cdata.hostname} / {cdata.port} / {cdata.slotName} / {cdata.password}");
        if (APSession != null)
        {
            APSession.Items.ItemReceived -= APSession_ItemReceived;
            APSession.MessageLog.OnMessageReceived -= APSession_OnMessageReceived;
            OnSessionClosed(APSession, true);
        }
        APSession = ArchipelagoSessionFactory.CreateSession(cdata.hostname, (int)cdata.port);
        LoginResult result = APSession.TryConnectAndLogin("Outer Wilds", cdata.slotName, ItemsHandlingFlags.AllItems, version: new Version(0, 4, 4), password: cdata.password, requestSlotData: true);
        if (!result.Successful)
            return result;

        APSession.Socket.ErrorReceived += APSession_ErrorReceived;

        var oldRoomId = SaveData.apConnectionData.roomId;
        var newRoomId = APSession.RoomState.Seed;
        OWMLModConsole.WriteLine($"old room id from save file is {oldRoomId}, new room id from AP server is {newRoomId}");

        // I don't know if RoomState.Seed is guaranteed to always exist, so if it somehow doesn't and newRoomId is null just act like nothing happened
        // oldRoomId being null usually means this is our first connection on a New Expedition.
        if (oldRoomId == null || newRoomId == null || oldRoomId == newRoomId)
        {
            var saveDataChanged = false;
            if (oldRoomId == null || newRoomId == null)
            {
                SaveData.apConnectionData.roomId = newRoomId;
                saveDataChanged = true;
            }

            FinishConnectingToAPServer(result, saveDataChanged, successCallback);
            return result;
        }

        // Room id doesn't match, show the user a warning popup letting them choose whether to "finish" the connection we've started.
        var modSaveCheckedLocationsCount = SaveData.locationsChecked.Where(kv => kv.Value).Count();
        var apServerCheckedLocationCount = APSession.Locations.AllLocationsChecked.Count;
        var countDifference = modSaveCheckedLocationsCount - apServerCheckedLocationCount;
        var warning = $"This AP server has a different room id from the one you previously connected to on this profile. ";
        if (countDifference > 0)
            warning += $"Continuing with this connection will likely tell the server to immediately mark {countDifference} locations as checked. ";
        warning += $"This usually means you forgot to start a New Random Expedition, create a new profile, or change connection info. Connect anyway?";
        OWMLModConsole.WriteLine(warning);

        var connectionFailedPopup = Instance.ModHelper.MenuHelper.PopupMenuManager.CreateTwoChoicePopup(warning, "Connect Anyway", "Cancel");
        connectionFailedPopup.EnableMenu(true);
        connectionFailedPopup.OnPopupConfirm += () =>
        {
            SaveData.apConnectionData.roomId = APSession.RoomState.Seed;
            FinishConnectingToAPServer(result, true /* saveDataChanged */, successCallback);
        };
        return result;
    }

    private static void FinishConnectingToAPServer(LoginResult result, bool saveDataChanged, Action successCallback)
    {
        SlotData = ((LoginSuccessful)result).SlotData;
        OWMLModConsole.WriteLine($"Received SlotData: {JsonConvert.SerializeObject(SlotData)}", MessageType.Info);

        // compatibility warnings
        if (SlotEnabledEotEDLC() && EntitlementsManager.IsDlcOwned() != EntitlementsManager.AsyncOwnershipStatus.Owned)
        {
            ArchConsoleManager.WakeupConsoleMessages.Add($"<color=red>Warning</color>: This Archipelago multiworld was generated with enable_eote_dlc: true, " +
                $"but <color=red>the DLC is not installed</color>.");
        }
        foreach (var mod in StoryModMetadata.AllStoryMods)
        {
            var option = mod.slotDataOption;
            bool modEnabled = (APRandomizer.SlotData.ContainsKey(option) && (long)APRandomizer.SlotData[option] > 0);
            if (modEnabled && !Instance.ModHelper.Interaction.ModExists(mod.modManagerUniqueName))
                ArchConsoleManager.WakeupConsoleMessages.Add($"<color=red>Warning</color>: This Archipelago multiworld was generated with {option}: true, " +
                    $"but <color=red>the mod {mod.modManagerUniqueName} is not installed</color>.");
        }
        if (SlotData.ContainsKey("apworld_version"))
        {
            var apworld_version = (string)SlotData["apworld_version"];
            // We don't take this from manifest.json because here we don't want the "-rc" suffix for Relase Candidate versions.
            var mod_version = "0.3.2";
            if (apworld_version != mod_version)
                ArchConsoleManager.WakeupConsoleMessages.Add($"<color=red>Warning</color>: This Archipelago multiworld was generated with .apworld version <color=red>{apworld_version}</color>, " +
                    $"but you're playing version <color=red>{mod_version}</color> of the mod. This may lead to game-breaking bugs.");
        }

        if (SlotData.ContainsKey("death_link"))
            DeathLinkManager.ApplySlotDataSetting((long)SlotData["death_link"]);

        if (SlotData.ContainsKey("goal"))
            Victory.SetGoal((long)SlotData["goal"]);

        if (SlotData.ContainsKey("eotu_coordinates"))
            Coordinates.SetCorrectCoordinatesFromSlotData(SlotData["eotu_coordinates"]);

        if (SlotData.ContainsKey("db_layout"))
            DarkBrambleLayout.ApplySlotDataLayout((string)SlotData["db_layout"]);

        if (SlotData.ContainsKey("planet_order") && SlotData.ContainsKey("orbit_angles") && SlotData.ContainsKey("rotation_axes"))
            Orbits.ApplySlotData(SlotData["planet_order"], SlotData["orbit_angles"], SlotData["rotation_axes"]);

        if (SlotData.ContainsKey("spawn"))
            Spawn.ApplySlotData((long)SlotData["spawn"]);

        if (SlotData.ContainsKey("warps"))
            WarpPlatforms.ApplySlotData(SlotData["warps"]);

        Translator.splitTranslator = SlotEnabledSplitTranslator();

        // Ensure that our local items state matches APSession.Items.AllItemsReceived. It's possible for AllItemsReceived to be out of date,
        // but in that case the ItemReceived event handler will be invoked as many times as it takes to get up to date.
        var totalItemsAcquired = SaveData.itemsAcquired.Sum(kv => kv.Value);
        var totalItemsReceived = APSession.Items.AllItemsReceived.Count;
        if (totalItemsReceived > totalItemsAcquired)
        {
            OWMLModConsole.WriteLine($"AP server state has more items ({totalItemsReceived}) than local save data ({totalItemsAcquired}). Attempting to update local save data to match.");
            foreach (var itemInfo in APSession.Items.AllItemsReceived)
                saveDataChanged = SyncItemCountWithAPServer(itemInfo.ItemId);
        }

        if (saveDataChanged)
            WriteToSaveFile();

        APSession.Items.ItemReceived += APSession_ItemReceived;
        APSession.MessageLog.OnMessageReceived += APSession_OnMessageReceived;

        // ensure that our local locations state matches the AP server by simply re-reporting any "missed" locations
        // it's important to do this after setting up the event handlers above, since a missed location will lead to AP sending us an item and a message
        var locallyCheckedLocationIds = SaveData.locationsChecked
            .Where(kv => kv.Value && LocationNames.locationToArchipelagoId.ContainsKey(kv.Key))
            .Select(kv => (long)LocationNames.locationToArchipelagoId[kv.Key]);
        var apServerCheckedLocationIds = APSession.Locations.AllLocationsChecked;
        var locationIdsMissedByServer = locallyCheckedLocationIds.Where(id => !apServerCheckedLocationIds.Contains(id));
        if (locationIdsMissedByServer.Any())
        {
            ArchConsoleManager.WakeupConsoleMessages.Add($"{locationIdsMissedByServer.Count()} locations you've previously checked were not marked as checked on the AP server:\n" +
                locationIdsMissedByServer.Join(id => "- " + LocationNames.locationNames[LocationNames.archipelagoIdToLocation[id]], "\n") +
                $"\nSending them to the AP server now.");
            APSession.Locations.CompleteLocationChecks(locationIdsMissedByServer.ToArray());
        }

        OnSessionOpened(APSession);

        successCallback();
    }

    private static HashSet<string> SocketWarningsAlreadyShown = new();

    private static void APSession_ErrorReceived(Exception e, string message)
    {
        if (!SocketWarningsAlreadyShown.Contains(message))
        {
            SocketWarningsAlreadyShown.Add(message);

            APRandomizer.InGameAPConsole.AddText($"<color='orange'>Received an error from APSession.Socket. This means you may have lost connection to the AP server. " +
                $"In order to safely reconnect to the AP server, we recommend quitting and resuming at your earliest convenience.</color>");

            APRandomizer.OWMLModConsole.WriteLine(
                $"Received error from APSession.Socket: '{message}'\n" +
                $"(duplicates of this error will be silently ignored)\n" +
                $"\n" +
                $"{e.StackTrace}",
                MessageType.Warning);
        }
    }

    private static void APSession_ItemReceived(IReceivedItemsHelper receivedItemsHelper)
    {
        try
        {
            if (DisableInGameItemReceiving && LoadManager.GetCurrentScene() == OWScene.SolarSystem) return;

            bool saveDataChanged = false;

            var receivedItems = new HashSet<long>();
            while (receivedItemsHelper.PeekItem() != null)
            {
                var itemId = receivedItemsHelper.PeekItem().ItemId;
                receivedItems.Add(itemId);
                receivedItemsHelper.DequeueItem();
            }

            OWMLModConsole.WriteLine($"ItemReceived event with item ids {string.Join(", ", receivedItems)}. Updating these item counts.");
            foreach (var itemId in receivedItems)
                saveDataChanged = SyncItemCountWithAPServer(itemId);

            if (saveDataChanged)
                WriteToSaveFile();
        }
        catch (Exception ex)
        {
            APRandomizer.OWMLModConsole.WriteLine(
                $"Caught error in APSession_ItemReceived: '{ex.Message}'\n" +
                $"{ex.StackTrace}",
                MessageType.Error);
        }
    }
    private static void APSession_OnMessageReceived(LogMessage message)
    {
        try
        {
            ArchConsoleManager.AddAPMessage(message);
        }
        catch (Exception ex)
        {
            APRandomizer.OWMLModConsole.WriteLine(
                $"Caught error in APSession_OnMessageReceived: '{ex.Message}'\n" +
                $"{ex.StackTrace}",
                MessageType.Error);
        }
    }

    private static bool SyncItemCountWithAPServer(long itemId)
    {
        if (!ItemNames.archipelagoIdToItem.ContainsKey(itemId))
        {
            ArchConsoleManager.WakeupConsoleMessages.Add(
                $"<color=red>Warning</color>: This mod does not recognize the item id {itemId}, which the Archipelago server just sent us. " +
                $"Check if your mod version matches the .apworld version used to generate this multiworld.");
            return false;
        }

        var item = ItemNames.archipelagoIdToItem[itemId];

        // It is technically possible to generate with apworld vX+1 to get X+1-only items, then play on mod vX to get a save file
        // without them, then upgrade to mod vX+1 and suddenly the mod recognizes the item but it's missing from the save file.
        // This works around that corner case by explicitly adding a 0 if it happens.
        if (!SaveData.itemsAcquired.ContainsKey(item))
            SaveData.itemsAcquired[item] = 0;

        var itemCountSoFar = APSession.Items.AllItemsReceived.Where(i => i.ItemId == itemId).Count();

        var savedCount = APRandomizer.SaveData.itemsAcquired[item];
        if (savedCount >= itemCountSoFar)
        {
            // APSession does client-side caching, so AllItemsReceived having fewer of an item than our save data usually just means the
            // client-side cache is out of date and will be brought up to date shortly with ItemReceived events. Thus, we ignore this case.
            return false;
        }
        else
        {
            APInventoryMode.MarkItemAsNew(item);
            APRandomizer.SaveData.itemsAcquired[item] = (uint)itemCountSoFar;
            LocationTriggers.ApplyItemToPlayer(item, APRandomizer.SaveData.itemsAcquired[item]);

            // SetPersistentCondition() is not safe to call on the main menu because e.g. it can lead to Switch Profile mistakently copying save data onto other profiles,
            if (LoadManager.GetCurrentScene() != OWScene.TitleScreen)
                if (ItemNames.itemToPersistentCondition.TryGetValue(item, out var condition))
                    PlayerData.SetPersistentCondition(condition, itemCountSoFar > 0); // for now, only unique items have conditions

            return true;
        }
    }

    private void Awake()
    {
        // You won't be able to access OWML's mod helper in Awake.
        // So you probably don't want to do anything here.
        // Use Start() instead.
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        Instance = this;
    }

    private void Start()
    {
        // Starting here, you'll have access to OWML's mod helper.

        // These .jsonc files are what we share directly with the .apworld to ensure
        // item ids/names, location ids/names and logic rules are kept in sync.
        // That's why this repo has a submodule for my Archipelago fork with the .apworld,
        // and why this project's .csproj has a rule to copy these files out of the submodule.
        ItemNames.LoadArchipelagoIds(ModHelper.Manifest.ModFolderPath + "items.jsonc");
        LocationNames.LoadArchipelagoIds(ModHelper.Manifest.ModFolderPath + "locations.jsonc");
        APRandomizer.OWMLModConsole.WriteLine($"loaded Archipelago item and location IDs");

        // Set up the console first so it can be safely used even in the various Setup() methods
        Assets = ModHelper.Assets.LoadBundle("Assets/archrandoassets");
        InGameAPConsole = gameObject.AddComponent<ArchConsoleManager>();

        Tracker = gameObject.AddComponent<TrackerManager>();

        LocationScouter = new();

        LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
        {
            WarpPlatforms.OnCompleteSceneLoad(scene, loadScene);
            Tornadoes.OnCompleteSceneLoad(scene, loadScene);
            QuantumImaging.OnCompleteSceneLoad(scene, loadScene);
            Jellyfish.OnCompleteSceneLoad(scene, loadScene);
            GhostMatterWavelength.OnCompleteSceneLoad(scene, loadScene);
            Victory.OnCompleteSceneLoad(scene, loadScene);
            DarkBrambleLayout.OnCompleteSceneLoad(scene, loadScene);
            Orbits.OnCompleteSceneLoad(scene, loadScene);
            Spawn.OnCompleteSceneLoad(scene, loadScene);
        };

        // update the Nomai text setting before any can be created
        LoadManager.OnStartSceneLoad += (scene, loadScene) =>
        {
            NomaiTextQoL.NomaiTextQoL.AutoNomaiText = AutoNomaiText;
            NomaiTextQoL.NomaiTextQoL.ColorNomaiText = ColorNomaiText;
        };

        SetupSaveData();

        NewHorizonsAPI = ModHelper.Interaction.TryGetModApi<INewHorizons>("xen.NewHorizons");

        OWMLModConsole.WriteLine($"Loaded Ixrec's Archipelago APRandomizer", OWML.Common.MessageType.Success);

        Application.quitting += () => OnSessionClosed(APSession, false);
    }

    public override void SetupTitleMenu(ITitleMenuManager titleManager) => MainMenu.SetupTitleMenu(titleManager);

    public static void AttemptToConnect(Action successCallback)
    {
        LoginResult loginResult = null;
        string exceptionMessage = null;

        try
        {
            loginResult = ConnectToAPServer(SaveData.apConnectionData, successCallback);
        }
        catch (Exception ex)
        {
            OWMLModConsole.WriteLine($"ConnectToAPServer() threw an exception:\n\n{ex.Message}\n{ex.StackTrace}");
            exceptionMessage = ex.Message;
        }

        if (loginResult == null || !loginResult.Successful)
        {
            var err = (exceptionMessage != null) ?
                    $"Failed to connect to AP server:\n{exceptionMessage}" :
                    $"Failed to connect to AP server:\n{string.Join("\n", ((LoginFailure)loginResult).Errors)}";
            OWMLModConsole.WriteLine(err);

            var connectionFailedPopup = Instance.ModHelper.MenuHelper.PopupMenuManager.CreateTwoChoicePopup(err, "Retry", "Cancel");
            connectionFailedPopup.EnableMenu(true);
            connectionFailedPopup.OnPopupConfirm += () => AttemptToConnect(successCallback);
        }
    }

    public override void SetupPauseMenu(IPauseMenuManager pauseManager) => MainMenu.SetupPauseMenu(pauseManager);

    public override void Configure(IModConfig config)
    {
        // Configure() is called early and often, including before we create the Console
        AutoNomaiText = config.GetSettingsValue<bool>("Auto Expand Nomai Text");
        ColorNomaiText = config.GetSettingsValue<bool>("LocationAppearanceMatchesContents");
        InstantTranslator = config.GetSettingsValue<bool>("Instant Translator");
        NomaiTextQoL.NomaiTextQoL.TranslateTime = InstantTranslator ? 0f : 0.2f;

        DisableConsole = config.GetSettingsValue<bool>("[DEBUG] Disable In-Game Console");
        DisableInGameLocationSending = config.GetSettingsValue<bool>("[DEBUG] Don't Send Locations In-Game");
        DisableInGameItemReceiving = config.GetSettingsValue<bool>("[DEBUG] Don't Receive Items In-Game");
        DisableInGameItemApplying = config.GetSettingsValue<bool>("[DEBUG] Don't Apply Received Items In-Game");
        DisableInGameSaveFileWrites = config.GetSettingsValue<bool>("[DEBUG] Don't Write To Save File In-Game");

        InGameAPConsole?.ModSettingsChanged(config);
        DeathLinkManager.ApplyOverrideSetting();
        SuitResources.ModSettingsChanged(config);
        GhostMatterPlacement.ModSettingsChanged(config);
    }

}
