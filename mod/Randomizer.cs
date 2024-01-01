using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Models;
using HarmonyLib;
using Newtonsoft.Json;
using OWML.Common;
using OWML.ModHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ArchipelagoRandomizer
{
    [HarmonyPatch]
    public class Randomizer : ModBehaviour
    {
        public static Randomizer Instance;

        public class APRandomizerSaveData
        {
            public Dictionary<Location, bool> locationsChecked;
            public Dictionary<Item, uint> itemsAcquired;
        }
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
                    OWMLModConsole.WriteLine($"No save file found for this profile. Will hide Resume button.");
                    var resumeButton = GameObject.Find("TitleMenu/TitleCanvas/TitleLayoutGroup/MainMenuBlock/MainMenuLayoutGroup/Button-ResumeGame");
                    OWMLModConsole.WriteLine($"resumeButton {resumeButton} {resumeButton.name}");
                    resumeButton.SetActive(false);
                }
                else
                {
                    OWMLModConsole.WriteLine($"Existing save file loaded. You've checked {SaveData.locationsChecked.Where(kv => kv.Value).Count()} out of {SaveData.locationsChecked.Count} locations " +
                        $"and acquired one or more of {SaveData.itemsAcquired.Where(kv => kv.Value > 0).Count()} different item types out of {SaveData.itemsAcquired.Count} total types.");

                    foreach (var kv in SaveData.itemsAcquired)
                        LocationTriggers.ApplyItemToPlayer(kv.Key, kv.Value);

                    ConnectToAPServer();
                }
            };
        }

        private static void ConnectToAPServer()
        {
            OWMLModConsole.WriteLine($"ConnectToAPServer() called");
            APSession = ArchipelagoSessionFactory.CreateSession("localhost", 38281);
            LoginResult result = APSession.TryConnectAndLogin("Outer Wilds", "Hearthian1", ItemsHandlingFlags.AllItems, version: new Version(0, 4, 4), requestSlotData: true);
            if (!result.Successful)
                throw new Exception($"Failed to connect to AP server:\n{string.Join("\n", ((LoginFailure)result).Errors)}");

            var loginSuccess = (LoginSuccessful)result;
            OWMLModConsole.WriteLine($"AP login succeeded, slot data is: {JsonConvert.SerializeObject(loginSuccess.SlotData)}");
            // todo: init a death link class
            // todo: tell the Victory class what the goal is

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
            var item = ItemNames.archipelagoIdToItem[(int)itemId];
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

            // I attempted to edit the New Expedition button, but for some reason anything I do to that button gets undone before the menu is shown.
            // var newGameObject = GameObject.Find("TitleMenu/TitleCanvas/TitleLayoutGroup/MainMenuBlock/MainMenuLayoutGroup/Button-NewGame");
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerData), nameof(PlayerData.ResetGame))]
        public static void PlayerData_ResetGame_Prefix()
        {
            Randomizer.OWMLModConsole.WriteLine($"Detected PlayerData.ResetGame() call. Creating fresh save file for this profile.");

            APRandomizerSaveData saveData = new();
            saveData.locationsChecked = Enum.GetValues(typeof(Location)).Cast<Location>()
                .ToDictionary(ln => ln, _ => false);
            saveData.itemsAcquired = Enum.GetValues(typeof(Item)).Cast<Item>()
                .ToDictionary(ln => ln, _ => 0u);

            Instance.ModHelper.Storage.Save<APRandomizerSaveData>(saveData, SaveFileName);

            SaveData = saveData;
            foreach (var kv in SaveData.itemsAcquired)
                LocationTriggers.ApplyItemToPlayer(kv.Key, kv.Value);
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

            SetupSaveData();

            Assets = ModHelper.Assets.LoadBundle("Assets/archrandoassets");
            InGameAPConsole = gameObject.AddComponent<ArchConsoleManager>();

            OWMLModConsole.WriteLine($"Loaded Ixrec's Archipelago Randomizer", OWML.Common.MessageType.Success);
        }
    }
}
