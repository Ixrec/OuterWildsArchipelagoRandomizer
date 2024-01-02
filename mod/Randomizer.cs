using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.MessageLog.Messages;
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
using UnityEngine;
using UnityEngine.UI;

namespace ArchipelagoRandomizer
{
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
    public class Randomizer : ModBehaviour
    {
        public static Randomizer Instance;

        public static APRandomizerSaveData SaveData;
        public static AssetBundle Assets;
        private static string SaveFileName;
        public static ArchipelagoSession APSession;

        public static IModConsole OWMLModConsole { get => Instance.ModHelper.Console; }
        public static ArchConsoleManager InGameAPConsole;

        public void WriteToSaveFile() =>
            ModHelper.Storage.Save<APRandomizerSaveData>(SaveData, SaveFileName);

        private void SetupSaveData()
        {
            var saveDataFolder = ModHelper.Manifest.ModFolderPath + "SaveData";
            if (!Directory.Exists(saveDataFolder))
            {
                OWMLModConsole.WriteLine($"Creating SaveData folder: {saveDataFolder}");
                Directory.CreateDirectory(saveDataFolder);
            }

            StandaloneProfileManager.SharedInstance.OnProfileReadDone += () => {
                if (StandaloneProfileManager.SharedInstance._currentProfile is null)
                {
                    OWMLModConsole.WriteLine($"No profile loaded", OWML.Common.MessageType.Error);
                    return;
                }
                var profileName = StandaloneProfileManager.SharedInstance._currentProfile.profileName;

                OWMLModConsole.WriteLine($"Profile {profileName} read by the game. Checking for a corresponding AP Randomizer save file.");

                SaveFileName = $"SaveData/{profileName}.json";
                SaveData = ModHelper.Storage.Load<APRandomizerSaveData>(SaveFileName);
                if (SaveData is null)
                {
                    OWMLModConsole.WriteLine($"No save file found for this profile.");
                    // Hiding the resume button here doesn't stick. We have to wait for TitleScreenManager_SetUpMainMenu_Postfix to do it.
                }
                else
                {
                    OWMLModConsole.WriteLine($"Existing save file loaded. You've checked {SaveData.locationsChecked.Where(kv => kv.Value).Count()} out of {SaveData.locationsChecked.Count} locations " +
                        $"and acquired one or more of {SaveData.itemsAcquired.Where(kv => kv.Value > 0).Count()} different item types out of {SaveData.itemsAcquired.Count} total types.");

                    foreach (var kv in SaveData.itemsAcquired)
                        LocationTriggers.ApplyItemToPlayer(kv.Key, kv.Value);

                    ConnectToAPServer(SaveData.apConnectionData);
                }
            };
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(TitleScreenManager), nameof(TitleScreenManager.SetUpMainMenu))]
        public static void TitleScreenManager_SetUpMainMenu_Postfix()
        {
            if (SaveData is null)
            {
                Randomizer.OWMLModConsole.WriteLine($"TitleScreenManager_SetUpMainMenu_Postfix hiding Resume button since there's no randomizer save file.");
                var resumeButton = GameObject.Find("TitleMenu/TitleCanvas/TitleLayoutGroup/MainMenuBlock/MainMenuLayoutGroup/Button-ResumeGame");
                resumeButton.SetActive(false);
            }
        }

        // Ideally errors here would put the user in a popup to decide whether to retry with the same connection info or enter different info,
        // but working with Outer Wilds menu buttons and popups from mod code is just too brittle and painful for that to be practical.
        // So if there are any errors during connection, we just explode ASAP.
        private static void ConnectToAPServer(APConnectionData cdata)
        {
            OWMLModConsole.WriteLine($"ConnectToAPServer() called with {cdata.hostname} / {cdata.port} / {cdata.slotName} / {cdata.password}");
            APSession = ArchipelagoSessionFactory.CreateSession(cdata.hostname, (int)cdata.port);
            LoginResult result = APSession.TryConnectAndLogin("Outer Wilds", cdata.slotName, ItemsHandlingFlags.AllItems, version: new Version(0, 4, 4), password: cdata.password, requestSlotData: true);
            if (!result.Successful)
                throw new Exception($"Failed to connect to AP server:\n{string.Join("\n", ((LoginFailure)result).Errors)}");

            var loginSuccess = (LoginSuccessful)result;
            OWMLModConsole.WriteLine($"AP login succeeded, slot data is: {JsonConvert.SerializeObject(loginSuccess.SlotData)}");

            if (loginSuccess.SlotData.ContainsKey("death_link"))
                DeathLinkManager.Enable((long)loginSuccess.SlotData["death_link"]);

            if (loginSuccess.SlotData.ContainsKey("goal"))
                Victory.SetGoal((long)loginSuccess.SlotData["goal"]);

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
                    Randomizer.Instance.WriteToSaveFile();
            }

            APSession.Items.ItemReceived += (receivedItemsHelper) => {
                OWMLModConsole.WriteLine($"APSession.Items.ItemReceived handler called");

                bool saveDataChanged = false;
                while (receivedItemsHelper.PeekItem().Item != 0)
                {
                    var itemId = receivedItemsHelper.PeekItem().Item;
                    OWMLModConsole.WriteLine($"ItemReceived handler received item id {itemId}");
                    saveDataChanged = SyncItemCountWithAPServer(itemId);
                    receivedItemsHelper.DequeueItem();
                }

                if (saveDataChanged)
                    Randomizer.Instance.WriteToSaveFile();
            };

            APSession.MessageLog.OnMessageReceived += (LogMessage message) => ArchConsoleManager.AddAPMessage(message);

            // ensure that our local locations state matches the AP server by simply re-reporting all checked locations
            // it's important to do this after setting up the event handlers above, since a missed location will lead to AP sending us an item and a message
            var allCheckedLocationIds = SaveData.locationsChecked
                .Where(kv => kv.Value && LocationNames.locationToArchipelagoId.ContainsKey(kv.Key))
                .Select(kv => (long)LocationNames.locationToArchipelagoId[kv.Key]);
            APSession.Locations.CompleteLocationChecks(allCheckedLocationIds.ToArray());
        }

        private static bool SyncItemCountWithAPServer(long itemId)
        {
            var item = ItemNames.archipelagoIdToItem[itemId];
            var itemCountSoFar = APSession.Items.AllItemsReceived.Where(i => i.Item == itemId).Count();

            var savedCount = Randomizer.SaveData.itemsAcquired[item];
            if (savedCount >= itemCountSoFar)
            {
                // APSession does client-side caching, so AllItemsReceived having fewer of an item than our save data usually just means the
                // client-side cache is out of date and will be brought up to date shortly with ItemReceived events. Thus, we ignore this case.
                Randomizer.OWMLModConsole.WriteLine($"Received {itemCountSoFar}-th instance of {itemId} ({item}) from AP server. Ignoring since SaveData already has {savedCount} of it.");
                return false;
            }
            else
            {
                Randomizer.OWMLModConsole.WriteLine($"Received {itemCountSoFar}-th instance of {itemId} ({item}) from AP server. Updating player inventory since SaveData has only {savedCount} of it.");

                Randomizer.SaveData.itemsAcquired[item] = (uint)itemCountSoFar;
                LocationTriggers.ApplyItemToPlayer(item, Randomizer.SaveData.itemsAcquired[item]);
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
            Randomizer.OWMLModConsole.WriteLine($"loaded Archipelago item and location IDs");

            WarpPlatforms.Setup();
            Tornadoes.Setup();
            QuantumImaging.Setup();
            Jellyfish.Setup();
            GhostMatter.Setup();

            var menuFramework = ModHelper.Interaction.TryGetModApi<IMenuAPI>("_nebula.MenuFramework");
            ModHelper.Menus.MainMenu.OnInit += () => StartCoroutine(SetupMainMenu(menuFramework));

            SetupSaveData();

            Assets = ModHelper.Assets.LoadBundle("Assets/archrandoassets");
            InGameAPConsole = gameObject.AddComponent<ArchConsoleManager>();

            OWMLModConsole.WriteLine($"Loaded Ixrec's Archipelago Randomizer", OWML.Common.MessageType.Success);
        }

        private IEnumerator SetupMainMenu(IMenuAPI menuFramework)
        {
            yield return new WaitForEndOfFrame();

            float popupOpenTime = 0;
            List<string> connectionInfoUserInput = new();

            var initialHeaderText = "Connection Info (1/3): Hostname & Port\n\ne.g. \"localhost:38281\", \"archipelago.gg:12345\"";
            var initialPlaceholderText = "Enter hostname:port...";
            var connectionInfoPopup = menuFramework.MakeInputFieldPopup(initialHeaderText, initialPlaceholderText, "Confirm", "Cancel");

            var newRandomExpeditionButton = menuFramework.TitleScreen_MakeMenuOpenButton("NEW RANDOMIZED EXPEDITION", 2, connectionInfoPopup);
            ModHelper.Menus.MainMenu.NewExpeditionButton.Hide();

            var headerText = connectionInfoPopup.gameObject.transform.Find("InputFieldBlock/InputFieldElements/Text").GetComponent<Text>();
            var inputField = connectionInfoPopup.gameObject.transform.Find("InputFieldBlock/InputFieldElements/InputField").GetComponent<InputField>();

            // Without these hacks, clicking the button to open this popup would also close this popup before the user could interact with it.
            connectionInfoPopup.CloseMenuOnOk(false);
            connectionInfoPopup.OnActivateMenu += () => popupOpenTime = Time.time;
            connectionInfoPopup.OnPopupConfirm += () =>
            {
                if (OWMath.ApproxEquals(Time.time, popupOpenTime)) return;

                connectionInfoUserInput.Add(connectionInfoPopup.GetInputText());
                OWMLModConsole.WriteLine($"connectionInfoUserInput: {string.Join(", ", connectionInfoUserInput)}");

                if (connectionInfoUserInput.Count == 1)
                {
                    headerText.text = "Connection Info (2/3): Slot/Player Name\n\ne.g. \"Hearthian1\", \"Hearthian2\"";
                    inputField.text = "";
                    ((Text)inputField.placeholder).text = "Enter slot/player name...";
                }
                else if (connectionInfoUserInput.Count == 2)
                {
                    headerText.text = "Connection Info (3/3): Password\n\nLeave blank if your server isn't using a password";
                    inputField.text = "";
                    ((Text)inputField.placeholder).text = "Enter password...";
                }
                else if (connectionInfoUserInput.Count == 3)
                {
                    connectionInfoPopup.EnableMenu(false);

                    OWMLModConsole.WriteLine($"Creating fresh save file for this profile with connectionInfo: {string.Join(", ", connectionInfoUserInput)}");

                    APConnectionData cdata = new();

                    var split = connectionInfoUserInput[0].Split(':');
                    cdata.hostname = split[0];
                    // if the player left out a port number, use the default localhost port of 38281
                    cdata.port = (split.Length > 1) ? uint.Parse(split[1]) : 38281;

                    cdata.slotName = connectionInfoUserInput[1];
                    cdata.password = connectionInfoUserInput[2];

                    APRandomizerSaveData saveData = new();
                    saveData.apConnectionData = cdata;
                    saveData.locationsChecked = Enum.GetValues(typeof(Location)).Cast<Location>()
                        .ToDictionary(ln => ln, _ => false);
                    saveData.itemsAcquired = Enum.GetValues(typeof(Item)).Cast<Item>()
                        .ToDictionary(ln => ln, _ => 0u);

                    SaveData = saveData;

                    // reset popup state in case connection fails
                    connectionInfoUserInput.Clear();
                    headerText.text = initialHeaderText;
                    inputField.text = "";
                    ((Text)inputField.placeholder).text = initialPlaceholderText;

                    ConnectToAPServer(cdata);

                    Instance.ModHelper.Storage.Save<APRandomizerSaveData>(saveData, SaveFileName);

                    foreach (var kv in SaveData.itemsAcquired)
                        LocationTriggers.ApplyItemToPlayer(kv.Key, kv.Value);

                    // quick and dirty attempt to reproduce what the vanilla New Expedition button does
                    PlayerData.ResetGame();
                    LoadManager.LoadSceneAsync(OWScene.SolarSystem, true, LoadManager.FadeType.ToBlack, 1f, false);
                    newRandomExpeditionButton.transform.GetChild(0).GetChild(1).GetComponent<Text>().text = "Loading..."; // not trying to reproduce the % for now
                }
                else
                    throw new Exception($"somehow connectionInfoUserInput has size {connectionInfoUserInput.Count}: {string.Join(", ", connectionInfoUserInput)}");
            };
        }
    }
}
