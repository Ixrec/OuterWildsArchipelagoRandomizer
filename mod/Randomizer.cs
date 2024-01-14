using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
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
                }
            };
        }

        // unfortunately hiding the vanilla Resume button with OWML ModHelper doesn't work, so we do that here instead
        [HarmonyPostfix]
        [HarmonyPatch(typeof(TitleScreenManager), nameof(TitleScreenManager.SetUpMainMenu))]
        public static void TitleScreenManager_SetUpMainMenu_Postfix()
        {
            var resumeButton = GameObject.Find("TitleMenu/TitleCanvas/TitleLayoutGroup/MainMenuBlock/MainMenuLayoutGroup/Button-ResumeGame");
            resumeButton.SetActive(false);
        }

        private static LoginResult ConnectToAPServer(APConnectionData cdata)
        {
            OWMLModConsole.WriteLine($"ConnectToAPServer() called with {cdata.hostname} / {cdata.port} / {cdata.slotName} / {cdata.password}");
            if (APSession is not null)
            {
                APSession.Items.ItemReceived -= APSession_ItemReceived;
                APSession.MessageLog.OnMessageReceived -= APSession_OnMessageReceived;
            }
            APSession = ArchipelagoSessionFactory.CreateSession(cdata.hostname, (int)cdata.port);
            LoginResult result = APSession.TryConnectAndLogin("Outer Wilds", cdata.slotName, ItemsHandlingFlags.AllItems, version: new Version(0, 4, 4), password: cdata.password, requestSlotData: true);
            if (!result.Successful)
                return result;

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

            APSession.Items.ItemReceived += APSession_ItemReceived;
            APSession.MessageLog.OnMessageReceived += APSession_OnMessageReceived;

            // ensure that our local locations state matches the AP server by simply re-reporting all checked locations
            // it's important to do this after setting up the event handlers above, since a missed location will lead to AP sending us an item and a message
            var allCheckedLocationIds = SaveData.locationsChecked
                .Where(kv => kv.Value && LocationNames.locationToArchipelagoId.ContainsKey(kv.Key))
                .Select(kv => (long)LocationNames.locationToArchipelagoId[kv.Key]);
            APSession.Locations.CompleteLocationChecks(allCheckedLocationIds.ToArray());

            return result;
        }

        private static void APSession_ItemReceived(ReceivedItemsHelper receivedItemsHelper)
        {
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
        }
        private static void APSession_OnMessageReceived(LogMessage message)
        {
            ArchConsoleManager.AddAPMessage(message);
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

        // The code below is pretty awful because of how limited and broken the UI APIs available to us are.
        // For example, I'd like to use a single popup for entering connection info, but there's no way to make a main menu
        // button launch a popup *and* know which button was clicked later on unless each button has a unique popup.
        // I'd also like to use a separate popup for connection errors, but there's no way to close a popup and then
        // launch a second popup without breaking everything, so we have to iteratively edit elements of a single popup.
        // I also couldn't put a 2nd entry or a 3rd button on a popup in a way that actually works. I won't list everything.

        private static string cinfoHeader1 = "Connection Info (1/3): Hostname & Port\n\ne.g. \"localhost:38281\", \"archipelago.gg:12345\"";
        private static string cinfoPlaceholder1 = "Enter hostname:port...";
        private static string cinfoHeader2 = "Connection Info (2/3): Slot/Player Name\n\ne.g. \"Hearthian1\", \"Hearthian2\"";
        private static string cinfoPlaceholder2 = "Enter slot/player name...";
        private static string cinfoHeader3 = "Connection Info (3/3): Password\n\nLeave blank if your server isn't using a password";
        private static string cinfoPlaceholder3 = "Enter password...";

        private static string cinfoLeftButton = "Confirm";
        private static string cinfoRightButton = "Cancel";

        private IEnumerator SetupMainMenu(IMenuAPI menuFramework)
        {
            yield return new WaitForEndOfFrame();

            var newConnInfoPopup = menuFramework.MakeInputFieldPopup(cinfoHeader1, cinfoPlaceholder1, cinfoLeftButton, cinfoRightButton);
            var changeConnInfoPopup = menuFramework.MakeInputFieldPopup(cinfoHeader1, cinfoPlaceholder1, cinfoLeftButton, cinfoRightButton);
            var resumeFailedPopup = menuFramework.MakeTwoChoicePopup("", "Retry", "Cancel");

            ModHelper.Menus.MainMenu.NewExpeditionButton.Hide();
            var newRandomExpeditionButton = menuFramework.TitleScreen_MakeMenuOpenButton("NEW RANDOM EXPEDITION", 0, newConnInfoPopup);
            // This is a new randomizer-only button, so there's no vanilla button to hide.
            var changeConnInfoButton = menuFramework.TitleScreen_MakeMenuOpenButton("CHANGE CONNECTION INFO", 0, changeConnInfoPopup);
            // unfortunately hiding the vanilla Resume button with OWML ModHelper doesn't work, so we do that in TitleScreenManager_SetUpMainMenu_Postfix instead
            var resumeRandomExpeditionButton = menuFramework.TitleScreen_MakeMenuOpenButton("RESUME RANDOM EXPEDITION", 0, resumeFailedPopup);
            //var resumeRandomExpeditionButton = menuFramework.TitleScreen_MakeSimpleButton("RESUME RANDOM EXPEDITION", 0);

            SetupConnInfoButton(changeConnInfoPopup, cdata =>
            {
                OWMLModConsole.WriteLine($"Connection info changed to \"{cdata.hostname}:{cdata.port}\", slot \"{cdata.slotName}\", password \"{cdata.password}\". Writing to mod save file.");
                changeConnInfoPopup.EnableMenu(false);
                SaveData.apConnectionData = cdata;
                WriteToSaveFile();
            });

            SetupConnInfoButton(newConnInfoPopup, cdata =>
            {
                OWMLModConsole.WriteLine($"Connection info changed to \"{cdata.hostname}:{cdata.port}\", slot \"{cdata.slotName}\", password \"{cdata.password}\".");

                // we set SaveData before the connection attempt so that even if it fails, the previous
                // connection info can be used on a second attempt to pre-populate the input fields
                APRandomizerSaveData saveData = new();
                saveData.apConnectionData = cdata;
                saveData.locationsChecked = Enum.GetValues(typeof(Location)).Cast<Location>().ToDictionary(ln => ln, _ => false);
                saveData.itemsAcquired = Enum.GetValues(typeof(Item)).Cast<Item>().ToDictionary(ln => ln, _ => 0u);
                SaveData = saveData;

                var loginResult = ConnectToAPServer(cdata);
                if (!loginResult.Successful)
                {
                    OWMLModConsole.WriteLine($"ConnectToAPServer failed");

                    var headerText = newConnInfoPopup.gameObject.transform.Find("InputFieldBlock/InputFieldElements/Text").GetComponent<Text>();
                    var inputField = newConnInfoPopup.gameObject.transform.Find("InputFieldBlock/InputFieldElements/InputField").GetComponent<InputField>();
                    var confirmButton = newConnInfoPopup.gameObject.transform.Find("InputFieldBlock/InputFieldElements/Buttons/UIElement-ButtonConfirm");
                    headerText.text = $"Failed to connect to AP server:\n{string.Join("\n", ((LoginFailure)loginResult).Errors)}";
                    inputField.gameObject.SetActive(false);
                    confirmButton.gameObject.SetActive(false);
                }
                else
                {
                    // now that we know we don't need to display an error message, we can finally hide the popup
                    newConnInfoPopup.EnableMenu(false);

                    // we don't overwrite the mod save file and the player's inventory until we're sure we can really start playing this new game
                    Instance.ModHelper.Storage.Save<APRandomizerSaveData>(saveData, SaveFileName);
                    foreach (var kv in SaveData.itemsAcquired)
                        LocationTriggers.ApplyItemToPlayer(kv.Key, kv.Value);

                    // also wipe the vanilla save file, since we've bypassed the base game code that would normally do this
                    PlayerData.ResetGame();

                    LoadTheGame(newRandomExpeditionButton);
                }
            });

            resumeFailedPopup.CloseMenuOnOk(false);
            var headerText = resumeFailedPopup.gameObject.transform.Find("PopupBlock/PopupElements/Text").GetComponent<Text>();
            float popupOpenTime = 0;

            resumeFailedPopup.OnActivateMenu += () => StartCoroutine(ResumePopupActivate());
            IEnumerator ResumePopupActivate()
            {
                popupOpenTime = Time.time;

                yield return new WaitForEndOfFrame();

                AttemptToConnect();
            }

            resumeFailedPopup.OnPopupConfirm += () => StartCoroutine(ResumePopupConfirm());
            IEnumerator ResumePopupConfirm()
            {
                OWMLModConsole.WriteLine($"resume error retry");
                headerText.text = "Connecting...";
                // 1 frame is not enough for the text change to become visible, for me 2 frames works
                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();
                AttemptToConnect();
            };
            resumeFailedPopup.OnPopupCancel += () =>
            {
                OWMLModConsole.WriteLine($"resume error cancel");
            };

            void AttemptToConnect()
            {
                var loginResult = ConnectToAPServer(SaveData.apConnectionData);
                if (!loginResult.Successful)
                {
                    var err = $"Failed to connect to AP server:\n{string.Join("\n", ((LoginFailure)loginResult).Errors)}";
                    OWMLModConsole.WriteLine(err);
                    headerText.text = err;
                }
                else
                {
                    OWMLModConsole.WriteLine($"Connection succeeded, hiding error popup and loading game.");
                    resumeFailedPopup.EnableMenu(false);

                    LoadTheGame(resumeRandomExpeditionButton);
                }
            }
        }

        private void SetupConnInfoButton(PopupInputMenu connectionInfoPopup, Action<APConnectionData> callback)
        {
            // Without these hacks (CloseMenuOnOk and the uses of popupOpenTime), clicking the button to open this popup
            // would also immediately close this popup before the user could interact with it.
            connectionInfoPopup.CloseMenuOnOk(false);
            float popupOpenTime = 0;

            var headerText = connectionInfoPopup.gameObject.transform.Find("InputFieldBlock/InputFieldElements/Text").GetComponent<Text>();
            var inputField = connectionInfoPopup.gameObject.transform.Find("InputFieldBlock/InputFieldElements/InputField").GetComponent<InputField>();

            List<string> connectionInfoUserInput = new();

            connectionInfoPopup.OnActivateMenu += () => StartCoroutine(Test());
            IEnumerator Test()
            {
                popupOpenTime = Time.time;

                yield return new WaitForEndOfFrame();

                // reset popup state in case we're being reused
                headerText.text = cinfoHeader1;
                inputField.text = "";
                ((Text)inputField.placeholder).text = cinfoPlaceholder1;
                inputField.gameObject.SetActive(true);
                connectionInfoUserInput.Clear();

                var cinfo = SaveData?.apConnectionData;
                if (cinfo != null)
                    inputField.text = cinfo.hostname + ':' + cinfo.port;
            }

            connectionInfoPopup.OnPopupConfirm += () =>
            {
                if (OWMath.ApproxEquals(Time.time, popupOpenTime)) return;

                connectionInfoUserInput.Add(connectionInfoPopup.GetInputText());

                if (connectionInfoUserInput.Count == 1)
                {
                    headerText.text = cinfoHeader2;
                    inputField.text = (SaveData?.apConnectionData is null) ? "" : SaveData.apConnectionData.slotName;
                    ((Text)inputField.placeholder).text = cinfoPlaceholder2;
                }
                else if (connectionInfoUserInput.Count == 2)
                {
                    headerText.text = cinfoHeader3;
                    inputField.text = (SaveData?.apConnectionData is null) ? "" : SaveData.apConnectionData.password;
                    ((Text)inputField.placeholder).text = cinfoPlaceholder3;
                }
                else if (connectionInfoUserInput.Count == 3)
                {
                    OWMLModConsole.WriteLine($"user entered connection info: {string.Join(", ", connectionInfoUserInput)}");

                    APConnectionData cdata = new();

                    var split = connectionInfoUserInput[0].Split(':');
                    cdata.hostname = split[0];
                    // if the player left out a port number, use the default localhost port of 38281
                    cdata.port = (split.Length > 1) ? uint.Parse(split[1]) : 38281;

                    cdata.slotName = connectionInfoUserInput[1];
                    cdata.password = connectionInfoUserInput[2];

                    callback(cdata);
                }
                else
                {
                    throw new Exception($"somehow connectionInfoUserInput has size {connectionInfoUserInput.Count}: {string.Join(", ", connectionInfoUserInput)}");
                }
            };
        }

        // quick and dirty attempt to reproduce what the vanilla New/Resume Expedition buttons do
        private void LoadTheGame(GameObject mainMenuButton)
        {
            LoadManager.LoadSceneAsync(OWScene.SolarSystem, true, LoadManager.FadeType.ToBlack, 1f, false);
            mainMenuButton.transform.GetChild(0).GetChild(1).GetComponent<Text>().text = "Loading..."; // not trying to reproduce the % for now
        }
    }
}
