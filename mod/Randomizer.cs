using HarmonyLib;
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
                    OWMLModConsole.WriteLine($"No profile loaded", MessageType.Error);
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

            OWMLModConsole.WriteLine($"Loaded Ixrec's Archipelago Randomizer", MessageType.Success);
        }
    }
}
