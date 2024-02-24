using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ArchipelagoRandomizer.InGameTracker
{
    public class TrackerSelectionMode : ShipLogMode
    {
        public ItemListWrapper Wrapper;
        public GameObject RootObject;
        public TrackerManager Tracker;

        private int selectedIndex;
        

        // Runs when the mode is created
        public override void Initialize(ScreenPromptList centerPromptList, ScreenPromptList upperRightPromptList, OWAudioSource oneShotSource)
        {
            APRandomizer.OWMLModConsole.WriteLine("Selector Mode created", OWML.Common.MessageType.Success);
        }

        // Runs when the mode is opened in the ship computer
        public override void EnterMode(string entryID = "", List<ShipLogFact> revealQueue = null)
        {
            Tracker.CheckInventory();
            if (Wrapper == null)
            {
                APRandomizer.OWMLModConsole.WriteLine("Wrapper is null!", OWML.Common.MessageType.Error);
            }
            else
            {
                APRandomizer.OWMLModConsole.WriteLine("Opened Selector Mode", OWML.Common.MessageType.Info);
            }
            Wrapper.Open();
            Wrapper.SetName("AP Tracker");
            Wrapper.SetItems(optionsList);
            Wrapper.SetSelectedIndex(0);
            Wrapper.UpdateList();
            selectedIndex = 0;
            RootObject.name = "ArchipelagoSelectorMode";

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
            if (OWInput.IsNewlyPressed(InputLibrary.menuConfirm))
            {
                OpenTrackerPage(selectedIndex);
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

        // Shows the item selected and the associated info
        private void SelectItem(int index)
        {
            
        }


    }
}
