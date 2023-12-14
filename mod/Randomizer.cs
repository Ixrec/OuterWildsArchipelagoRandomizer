using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ArchipelagoRandomizer
{
    public class Randomizer : ModBehaviour
    {
        public static Randomizer Instance;

        public class APRandomizerSaveData
        {
            public Dictionary<Location, bool> locationsChecked;
            public Dictionary<Item, uint> itemsAcquired;
        }
        public static APRandomizerSaveData SaveData;
        private static string SaveFileName;

        public void WriteToSaveFile() =>
            ModHelper.Storage.Save<APRandomizerSaveData>(SaveData, SaveFileName);

        private void SetupSaveData()
        {
            var saveDataFolder = ModHelper.Manifest.ModFolderPath + "SaveData";
            if (!Directory.Exists(saveDataFolder))
            {
                ModHelper.Console.WriteLine($"Creating SaveData folder: {saveDataFolder}");
                Directory.CreateDirectory(saveDataFolder);
            }

            StandaloneProfileManager.SharedInstance.OnProfileReadDone += () => {
                if (StandaloneProfileManager.SharedInstance._currentProfile is null)
                {
                    ModHelper.Console.WriteLine($"No profile loaded", MessageType.Error);
                    return;
                }
                var profileName = StandaloneProfileManager.SharedInstance._currentProfile.profileName;

                ModHelper.Console.WriteLine($"Profile {profileName} read by the game. Checking for a corresponding AP Randomizer save file.");

                SaveFileName = $"SaveData/{profileName}.json";
                APRandomizerSaveData saveData = ModHelper.Storage.Load<APRandomizerSaveData>(SaveFileName);
                if (saveData is null)
                {
                    ModHelper.Console.WriteLine($"No save file found for this profile. Creating a new one.");

                    saveData = new();
                    saveData.locationsChecked = Enum.GetValues(typeof(Location)).Cast<Location>()
                        .ToDictionary(ln => ln, _ => false);
                    saveData.itemsAcquired = Enum.GetValues(typeof(Item)).Cast<Item>()
                        .ToDictionary(ln => ln, _ => 0u);

                    ModHelper.Storage.Save<APRandomizerSaveData>(saveData, SaveFileName);
                }
                else
                {
                    ModHelper.Console.WriteLine($"Existing save file loaded. You've checked {saveData.locationsChecked.Where(kv => kv.Value).Count()} out of {saveData.locationsChecked.Count} locations " +
                        $"and acquired one or more of {saveData.itemsAcquired.Where(kv => kv.Value > 0).Count()} different item types out of {saveData.itemsAcquired.Count} total types.");

                    foreach (var kv in saveData.itemsAcquired)
                        LocationTriggers.ApplyItemToPlayer(kv.Key, kv.Value);
                }

                SaveData = saveData;
            };
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
            InGameConsole.Setup();

            WarpPlatforms.Setup();
            Tornadoes.Setup();
            QuantumImaging.Setup();
            Jellyfish.Setup();
            GhostMatter.Setup();

            SetupSaveData();

            ModHelper.Console.WriteLine($"Loaded Ixrec's Archipelago Randomizer", MessageType.Success);
        }
    }
}
