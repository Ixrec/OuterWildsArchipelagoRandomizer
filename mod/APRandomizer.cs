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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ArchipelagoRandomizer;

public class APConnectionData
{
    public string hostname;
    public uint port;
    public string slotName;
    public string password;
}
public class APRandomizerSaveData
{
    public APConnectionData apConnectionData;
    public Dictionary<Location, bool> locationsChecked;
    public Dictionary<Item, uint> itemsAcquired;
}

[HarmonyPatch]
public class APRandomizer : ModBehaviour
{
    public static APRandomizer Instance;

    public static APRandomizerSaveData SaveData;
    public static AssetBundle Assets;
    private static string SaveFileName;
    public static ArchipelagoSession APSession;
    public static Dictionary<string, object> SlotData;

    public static IModConsole OWMLModConsole { get => Instance.ModHelper.Console; }
    public static ArchConsoleManager InGameAPConsole;
    public static TrackerManager Tracker;

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

    public static bool DisableConsole = false;
    public static bool DisableInGameLocationSending = false;
    private static bool DisableInGameItemReceiving = false;
    private static bool DisableInGameSaveFileWrites = false;

    // Throttle save file writes to once per second to avoid IOExceptions for conflicting write attempts
    private static Task pendingSaveFileWrite = null;
    private static DateTimeOffset lastWriteTime = DateTimeOffset.UtcNow;
    public void WriteToSaveFile()
    {
        if (DisableInGameSaveFileWrites && LoadManager.GetCurrentScene() == OWScene.SolarSystem) return;

        if (pendingSaveFileWrite != null) return;

        if (lastWriteTime < DateTimeOffset.UtcNow.AddSeconds(-1))
        {
            ModHelper.Storage.Save<APRandomizerSaveData>(SaveData, SaveFileName);
            lastWriteTime = DateTimeOffset.UtcNow;
        }

        pendingSaveFileWrite = Task.Run(async () =>
        {
            await Task.Delay(1000);

            ModHelper.Storage.Save<APRandomizerSaveData>(SaveData, SaveFileName);
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
                OWMLModConsole.WriteLine($"No profile loaded", OWML.Common.MessageType.Error);
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
                if (ResumeRandomExpeditionGO != null)
                {
                    ChangeConnInfoGO?.SetActive(false);
                    ResumeRandomExpeditionGO?.SetActive(false);
                }
            }
            else
            {
                OWMLModConsole.WriteLine($"Existing save file loaded. You've checked {SaveData.locationsChecked.Where(kv => kv.Value).Count()} out of {SaveData.locationsChecked.Count} locations " +
                    $"and acquired one or more of {SaveData.itemsAcquired.Where(kv => kv.Value > 0).Count()} different item types out of {SaveData.itemsAcquired.Count} total types.");

                foreach (var kv in SaveData.itemsAcquired)
                    LocationTriggers.ApplyItemToPlayer(kv.Key, kv.Value);

                if (ResumeRandomExpeditionGO != null)
                {
                    ChangeConnInfoGO?.SetActive(true);
                    ResumeRandomExpeditionGO?.SetActive(true);
                }
            }
        };
    }

    // OWML.MenuHelper has no API for hiding the vanilla buttons, so we do that here instead
    [HarmonyPostfix, HarmonyPatch(typeof(TitleScreenManager), nameof(TitleScreenManager.SetUpMainMenu))]
    public static void TitleScreenManager_SetUpMainMenu_Postfix()
    {
        var resumeButton = GameObject.Find("TitleMenu/TitleCanvas/TitleLayoutGroup/MainMenuBlock/MainMenuLayoutGroup/Button-ResumeGame");
        resumeButton.SetActive(false);
        var newButton = GameObject.Find("TitleMenu/TitleCanvas/TitleLayoutGroup/MainMenuBlock/MainMenuLayoutGroup/Button-NewGame");
        newButton.SetActive(false);
    }

    // The main menu does not autoselect the top visible button, but rather a specific named button, so the fact that we're forced
    // to hide some vanilla buttons means we also have to hijack this autoselection logic to unbreak the main menu.
    // In practice this is primarily a problem for controller input.
    [HarmonyPostfix, HarmonyPatch(typeof(OWMenuInputModule), nameof(OWMenuInputModule.SelectOnNextUpdate))]
    public static void OWMenuInputModule_SelectOnNextUpdate(OWMenuInputModule __instance, Selectable selectable)
    {
        if (selectable?.name == "Button-NewGame" || selectable?.name == "Button-ResumeGame")
        {
            GameObject gameObject = (SaveData == null) ? NewRandomExpeditionGO : ResumeRandomExpeditionGO;
            var modButton = gameObject.GetComponent<Button>();
            if (modButton != null)
            {
                OWMLModConsole.WriteLine($"OWMenuInputModule_SelectOnNextUpdate changing selectable from {selectable.name} to {gameObject.name}");
                __instance._nextSelectableQueue.Remove(selectable);
                __instance._nextSelectableQueue.Add(modButton);
            }
        }
    }

    private static LoginResult ConnectToAPServer(APConnectionData cdata)
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

        SlotData = ((LoginSuccessful)result).SlotData;
        OWMLModConsole.WriteLine($"Received SlotData: {JsonConvert.SerializeObject(SlotData)}", MessageType.Info);

        if (SlotData.ContainsKey("apworld_version"))
        {
            var apworld_version = (string)SlotData["apworld_version"];
            // We don't take this from manifest.json because here we don't want the "-rc" suffix for Relase Candidate versions.
            var mod_version = "0.2.0";
            if (apworld_version != mod_version)
                InGameAPConsole.WakeupConsoleMessages.Add($"<color=red>Warning</color>: This Archipelago multiworld was generated with .apworld version <color=red>{apworld_version}</color>, " +
                    $"but you're playing version <color=red>{mod_version}</color> of the mod. This may lead to game-breaking bugs.");
        }

        if (SlotData.ContainsKey("death_link"))
            DeathLinkManager.ApplySlotDataSetting((long)SlotData["death_link"]);

        if (SlotData.ContainsKey("goal"))
            Victory.SetGoal((long)SlotData["goal"]);

        if (SlotData.ContainsKey("eotu_coordinates"))
            Coordinates.SetCorrectCoordinatesFromSlotData(SlotData["eotu_coordinates"]);

        // Ensure that our local items state matches APSession.Items.AllItemsReceived. It's possible for AllItemsReceived to be out of date,
        // but in that case the ItemReceived event handler will be invoked as many times as it takes to get up to date.
        var totalItemsAcquired = SaveData.itemsAcquired.Sum(kv => kv.Value);
        var totalItemsReceived = APSession.Items.AllItemsReceived.Count;
        if (totalItemsReceived > totalItemsAcquired)
        {
            OWMLModConsole.WriteLine($"AP server state has more items ({totalItemsReceived}) than local save data ({totalItemsAcquired}). Attempting to update local save data to match.");
            bool saveDataChanged = false;
            foreach (var networkItem in APSession.Items.AllItemsReceived)
                saveDataChanged = SyncItemCountWithAPServer(networkItem.Item);

            if (saveDataChanged)
                APRandomizer.Instance.WriteToSaveFile();
        }

        APSession.Items.ItemReceived += APSession_ItemReceived;
        APSession.MessageLog.OnMessageReceived += APSession_OnMessageReceived;

        // ensure that our local locations state matches the AP server by simply re-reporting all checked locations
        // it's important to do this after setting up the event handlers above, since a missed location will lead to AP sending us an item and a message
        var allCheckedLocationIds = SaveData.locationsChecked
            .Where(kv => kv.Value && LocationNames.locationToArchipelagoId.ContainsKey(kv.Key))
            .Select(kv => (long)LocationNames.locationToArchipelagoId[kv.Key]);
        APSession.Locations.CompleteLocationChecks(allCheckedLocationIds.ToArray());

        OnSessionOpened(APSession);

        return result;
    }

    private static void APSession_ItemReceived(ReceivedItemsHelper receivedItemsHelper)
    {
        if (DisableInGameItemReceiving && LoadManager.GetCurrentScene() == OWScene.SolarSystem) return;

        bool saveDataChanged = false;

        var receivedItems = new HashSet<long>();
        while (receivedItemsHelper.PeekItem().Item != 0)
        {
            var itemId = receivedItemsHelper.PeekItem().Item;
            receivedItems.Add(itemId);
            receivedItemsHelper.DequeueItem();
        }

        OWMLModConsole.WriteLine($"ItemReceived event with item ids {string.Join(", ", receivedItems)}. Updating these item counts.");
        foreach (var itemId in receivedItems)
            saveDataChanged = SyncItemCountWithAPServer(itemId);

        if (saveDataChanged)
            APRandomizer.Instance.WriteToSaveFile();
    }
    private static void APSession_OnMessageReceived(LogMessage message)
    {
        ArchConsoleManager.AddAPMessage(message);
    }

    private static bool SyncItemCountWithAPServer(long itemId)
    {
        if (!ItemNames.archipelagoIdToItem.ContainsKey(itemId))
        {
            InGameAPConsole.WakeupConsoleMessages.Add(
                $"<color=red>Warning</color>: This mod does not recognize the item id {itemId}, which the Archipelago server just sent us. " +
                $"Check if your mod version matches the .apworld version used to generate this multiworld.");
            return false;
        }

        var item = ItemNames.archipelagoIdToItem[itemId];
        var itemCountSoFar = APSession.Items.AllItemsReceived.Where(i => i.Item == itemId).Count();

        var savedCount = APRandomizer.SaveData.itemsAcquired[item];
        if (savedCount >= itemCountSoFar)
        {
            // APSession does client-side caching, so AllItemsReceived having fewer of an item than our save data usually just means the
            // client-side cache is out of date and will be brought up to date shortly with ItemReceived events. Thus, we ignore this case.
            return false;
        }
        else
        {
            TrackerManager.MarkItemAsNew(item);
            APRandomizer.SaveData.itemsAcquired[item] = (uint)itemCountSoFar;
            LocationTriggers.ApplyItemToPlayer(item, APRandomizer.SaveData.itemsAcquired[item]);
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

        LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
        {
            WarpPlatforms.OnCompleteSceneLoad(scene, loadScene);
            Tornadoes.OnCompleteSceneLoad(scene, loadScene);
            QuantumImaging.OnCompleteSceneLoad(scene, loadScene);
            Jellyfish.OnCompleteSceneLoad(scene, loadScene);
            GhostMatter.OnCompleteSceneLoad(scene, loadScene);
            Victory.OnCompleteSceneLoad(scene, loadScene);
        };

        SetupSaveData();

        OWMLModConsole.WriteLine($"Loaded Ixrec's Archipelago APRandomizer", OWML.Common.MessageType.Success);

        Application.quitting += () => OnSessionClosed(APSession, false);
    }

    private static GameObject NewRandomExpeditionGO = null;
    private static SubmitAction NewRandomExpeditionSA = null;
    private static GameObject ChangeConnInfoGO = null;
    private static SubmitAction ChangeConnInfoSA = null;
    private static GameObject ResumeRandomExpeditionGO = null;
    private static SubmitAction ResumeRandomExpeditionSA = null;

    public override void SetupTitleMenu(ITitleMenuManager titleManager)
    {
        var hostAndPortInput = Instance.ModHelper.MenuHelper.PopupMenuManager.CreateInputFieldPopup(
            "Connection Info (1/3): Hostname & Port\n\ne.g. \"localhost:38281\", \"archipelago.gg:12345\"",
            "Enter hostname:port...",
            "Confirm", "Cancel");
        var slotInput = Instance.ModHelper.MenuHelper.PopupMenuManager.CreateInputFieldPopup(
            "Connection Info (2/3): Slot/Player Name\n\ne.g. \"Hearthian1\", \"Hearthian2\"",
            "Enter slot/player name...",
            "Confirm", "Cancel");
        var passwordInput = Instance.ModHelper.MenuHelper.PopupMenuManager.CreateInputFieldPopup(
            "Connection Info (3/3): Password\n\nLeave blank if your server isn't using a password",
            "Enter password...",
            "Confirm", "Cancel");

        NewRandomExpeditionSA = ModHelper.MenuHelper.TitleMenuManager.CreateTitleButton("NEW RANDOM EXPEDITION", 0, true);
        ChangeConnInfoSA = ModHelper.MenuHelper.TitleMenuManager.CreateTitleButton("CHANGE CONNECTION INFO", 0, true);
        ResumeRandomExpeditionSA = ModHelper.MenuHelper.TitleMenuManager.CreateTitleButton("RESUME RANDOM EXPEDITION", 0, true);

        var pathToMainMenuButtons = "TitleMenu/TitleCanvas/TitleLayoutGroup/MainMenuBlock/MainMenuLayoutGroup";
        NewRandomExpeditionGO = GameObject.Find($"{pathToMainMenuButtons}/Button-NEW RANDOM EXPEDITION");
        ChangeConnInfoGO = GameObject.Find($"{pathToMainMenuButtons}/Button-CHANGE CONNECTION INFO");
        ResumeRandomExpeditionGO = GameObject.Find($"{pathToMainMenuButtons}/Button-RESUME RANDOM EXPEDITION");

        OWML.Utils.MenuExtensions.SetButtonVisible(ChangeConnInfoSA, SaveData != null);
        OWML.Utils.MenuExtensions.SetButtonVisible(ResumeRandomExpeditionSA, SaveData != null);

        SubmitAction lastButtonClicked = null;
        APConnectionData connData = (SaveData == null) ? new() : SaveData.apConnectionData;

        NewRandomExpeditionSA.OnSubmitAction += () =>
        {
            lastButtonClicked = NewRandomExpeditionSA;
            StartConnInfoInput();
        };
        ChangeConnInfoSA.OnSubmitAction += () =>
        {
            lastButtonClicked = ChangeConnInfoSA;
            StartConnInfoInput();
        };

        ResumeRandomExpeditionSA.OnSubmitAction += () =>
        {
            lastButtonClicked = ResumeRandomExpeditionSA;
            AttemptToConnect(() => LoadTheGame(ResumeRandomExpeditionSA));
        };

        void StartConnInfoInput()
        {
            connData = (SaveData == null) ? new() : SaveData.apConnectionData;

            hostAndPortInput.EnableMenu(true);
            if (connData.hostname != null && connData.hostname.Length > 0)
                hostAndPortInput.GetInputField().text = connData.hostname + ':' + connData.port;
        }

        hostAndPortInput.OnPopupConfirm += () => {
            var inputText = hostAndPortInput.GetInputText();
            OWMLModConsole.WriteLine($"hostAndPortInput.OnPopupConfirm: {inputText}");

            var split = inputText.Split(':');
            connData.hostname = split[0];
            // if the player left out a port number, use the default localhost port of 38281
            connData.port = (split.Length > 1) ? uint.Parse(split[1]) : 38281;

            slotInput.EnableMenu(true);
            if (connData.slotName != null && connData.slotName.Length > 0)
                slotInput.GetInputField().text = connData.slotName;
        };

        slotInput.OnPopupConfirm += () => {
            var inputText = slotInput.GetInputText();
            OWMLModConsole.WriteLine($"slotInput.OnPopupConfirm: {inputText}");

            connData.slotName = inputText;

            passwordInput.EnableMenu(true);
            if (connData.password != null && connData.password.Length > 0)
                passwordInput.GetInputField().text = connData.password;
        };

        passwordInput.OnPopupConfirm += () => {
            var inputText = passwordInput.GetInputText();
            OWMLModConsole.WriteLine($"passwordInput.OnPopupConfirm: {inputText}");

            connData.password = inputText;

            if (lastButtonClicked == ChangeConnInfoSA)
            {
                SaveData.apConnectionData = connData;
                WriteToSaveFile();
            }
            else if (lastButtonClicked == NewRandomExpeditionSA)
            {
                APRandomizerSaveData saveData = new();
                saveData.apConnectionData = connData;
                saveData.locationsChecked = Enum.GetValues(typeof(Location)).Cast<Location>().ToDictionary(ln => ln, _ => false);
                saveData.itemsAcquired = Enum.GetValues(typeof(Item)).Cast<Item>().ToDictionary(ln => ln, _ => 0u);
                SaveData = saveData;

                AttemptToConnect(() =>
                {
                    // we don't overwrite the mod save file and the player's inventory until we're sure we can really start playing this new game
                    Instance.ModHelper.Storage.Save<APRandomizerSaveData>(saveData, SaveFileName);
                    foreach (var kv in SaveData.itemsAcquired)
                        LocationTriggers.ApplyItemToPlayer(kv.Key, kv.Value);

                    // also wipe the vanilla save file, since we've bypassed the base game code that would normally do this
                    PlayerData.ResetGame();

                    LoadTheGame(NewRandomExpeditionSA);
                });
            }
        };
    }

    private void AttemptToConnect(Action successCallback)
    {
        LoginResult loginResult = null;
        string exceptionMessage = null;

        try
        {
            loginResult = ConnectToAPServer(SaveData.apConnectionData);
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
        else
        {
            successCallback();
        }
    }

    // quick and dirty attempt to reproduce what the vanilla New/Resume Expedition buttons do
    private void LoadTheGame(SubmitAction mainMenuButton)
    {
        var scene = PlayerData.GetWarpedToTheEye() ? OWScene.EyeOfTheUniverse : OWScene.SolarSystem;
        LoadManager.LoadSceneAsync(scene, true, LoadManager.FadeType.ToBlack, 1f, false);

        ModHelper.MenuHelper.TitleMenuManager.SetButtonText(mainMenuButton, "Loading...");

        var lpu = GameObject.Find("TitleMenu").AddComponent<LoadProgressUpdater>();
        lpu.titleMenuManager = ModHelper.MenuHelper.TitleMenuManager;
        lpu.mainMenuButton = mainMenuButton;
    }

    class LoadProgressUpdater : MonoBehaviour
    {
        public ITitleMenuManager titleMenuManager;
        public SubmitAction mainMenuButton;

        private void Update()
        {
            if (
                mainMenuButton != null &&
                (LoadManager.GetLoadingScene() == OWScene.SolarSystem || LoadManager.GetLoadingScene() == OWScene.EyeOfTheUniverse)
            )
            {
                // I dunno what's going on with the GetAsyncLoadProgress() return value, but it's not a normal 0-1 or 0-100 number like you'd expect.
                // This translation into a human-readable progress percentage is copy-pasted from:
                // https://github.com/misternebula/MenuFramework/blob/3215c0d66782908e4de557bf71ee36adf693c640/MenuFramework/CustomSubmitActionLoadScene.cs#L16-L19
                var loadProgress = LoadManager.GetAsyncLoadProgress();
                loadProgress = loadProgress < 0.1f
                    ? Mathf.InverseLerp(0f, 0.1f, loadProgress) * 0.9f
                    : 0.9f + (Mathf.InverseLerp(0.1f, 1f, loadProgress) * 0.1f);

                var loadProgressString = loadProgress.ToString("P0"); // P = percentage format, 0 = no decimal digits

                titleMenuManager.SetButtonText(mainMenuButton, $"Loading... {loadProgressString}");
            }
        }
    }

    public override void SetupPauseMenu(IPauseMenuManager pauseManager)
    {
        if (LoadManager.GetCurrentScene() == OWScene.EyeOfTheUniverse)
        {
            var quitAndReset = pauseManager.MakeSimpleButton("QUIT AND RESET\nTO SOLAR SYSTEM", 0, false);
            quitAndReset.OnSubmitAction += () =>
            {
                OWMLModConsole.WriteLine($"reset clicked");
                PlayerData.SaveEyeCompletion();
                LoadManager.LoadScene(OWScene.TitleScreen, LoadManager.FadeType.None, 1f, true);
            };
        }
    }

    public override void Configure(IModConfig config)
    {
        // Configure() is called early and often, including before we create the Console
        DisableConsole = config.GetSettingsValue<bool>("[DEBUG] Disable In-Game Console");
        DisableInGameLocationSending = config.GetSettingsValue<bool>("[DEBUG] Don't Send Locations In-Game");
        DisableInGameItemReceiving = config.GetSettingsValue<bool>("[DEBUG] Don't Receive Items In-Game");
        DisableInGameSaveFileWrites = config.GetSettingsValue<bool>("[DEBUG] Don't Write To Save File In-Game");

        InGameAPConsole?.ModSettingsChanged(config);
        DeathLinkManager.ApplyOverrideSetting();
        SuitResources.ModSettingsChanged(config);
    }
}
