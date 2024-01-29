using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ArchipelagoRandomizer.InGameTracker
{
    /// <summary>
    /// The inventory screen. All the functions here are required, even if empty.
    /// </summary>
    public class TrackerInventoryMode : ShipLogMode
    {
        public ItemListWrapper Wrapper;
        public GameObject RootObject;
        public TrackerManager Tracker;

        private int selectedIndex;
        private Image icon => Wrapper.GetPhoto();
        private Text questionMark => Wrapper.GetQuestionMark();
        private Dictionary<Item, uint> inventory => Randomizer.SaveData.itemsAcquired;

        // Runs when the mode is created
        public override void Initialize(ScreenPromptList centerPromptList, ScreenPromptList upperRightPromptList, OWAudioSource oneShotSource)
        {
            Randomizer.LogInfo("Tracker Mode created");
        }

        // Runs when the mode is opened in the ship computer
        public override void EnterMode(string entryID = "", List<ShipLogFact> revealQueue = null)
        {
            if (Wrapper == null)
            {
                Randomizer.LogError("Wrapper is null!");
            }
            else
            {
                Randomizer.LogInfo("Opened Inventory Mode");
            }
            Wrapper.Open();
            Wrapper.SetName("AP Inventory");
            Wrapper.SetItems(Tracker.InventoryItems);
            Wrapper.SetSelectedIndex(0);
            Wrapper.UpdateList();
            selectedIndex = 0;
            RootObject.name = "ArchipelagoTrackerMode";

            SelectItem(0);
        }

        // Runs when the mode is closed
        public override void ExitMode()
        {
            Wrapper.Close();
        }


        // Runs when player enters computer, update info that changes between computer sessions. Runs after EnterMode
        public override void OnEnterComputer()
        {

        }

        // Runs when the player exits the computer, after ExitMode
        public override void OnExitComputer()
        {

        }

        // Runs every frame the mode is active
        public override void UpdateMode()
        {
            int changeIndex = Wrapper.UpdateList();

            if (changeIndex != 0)
            {
                selectedIndex += changeIndex;

                if (selectedIndex < 0) selectedIndex = Tracker.InventoryItems.Count - 1;
                if (selectedIndex >= Tracker.InventoryItems.Count) selectedIndex = 0;

                SelectItem(selectedIndex);
            }
        }

        // Allows leaving the computer in this mode
        public override bool AllowCancelInput()
        {
            return true;
        }
        
        // Allows swapping modes while in this mode
        public override bool AllowModeSwap()
        {
            return true;
        }

        // Returns the ID of the selected ship entry, used for knowing which entry should be highlighted when switching to Map Mode. Useless for us probably.
        public override string GetFocusedEntryID()
        {
            return "";
        }

        private void SelectItem(int index)
        {
            string itemID = Tracker.ItemEntries.ElementAt(index).Key;
            Sprite tex = TrackerManager.GetSprite(itemID);
            bool itemExists = Enum.TryParse(itemID, out Item result);
            // Only item that doesn't exist is the FrequencyOWV which we want to show as obtained regardless
            if (!itemExists || inventory[result] > 0)
            {
                if (tex != null)
                {
                    icon.sprite = tex;
                    icon.gameObject.SetActive(true);
                    questionMark.gameObject.SetActive(false);
                }
            }
            else
            {
                icon.gameObject.SetActive(false);
                questionMark.gameObject.SetActive(true);
            }

            TrackerDescriptions.DisplayItemText(itemID, Wrapper);
        }
    }
}
