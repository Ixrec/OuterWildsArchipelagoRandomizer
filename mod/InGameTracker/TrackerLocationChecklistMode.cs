using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;

namespace ArchipelagoRandomizer.InGameTracker
{
    public class TrackerLocationChecklistMode : ShipLogMode
    {
        public ItemListWrapper ChecklistWrapper;
        public ItemListWrapper SelectionWrapper;
        
        public GameObject RootObject;
        public TrackerManager Tracker;

        public bool IsInChecklist = false;

        private int selectedIndex;
        private GameObject shipLogPanRoot;

        private readonly string[] optionsEntries =
        {
            "Mission",
            "Hourglass Twins",
            "Timber Hearth",
            "Brittle Hollow",
            "Giant's Deep",
            "Dark Bramble",
            "The Outer Wilds"
            // When DLC support is added, add Stranger and Dreamworld conditionally
        };
        private List<Tuple<string, bool, bool, bool>> optionsList;

        // Runs when the mode is created
        public override void Initialize(ScreenPromptList centerPromptList, ScreenPromptList upperRightPromptList, OWAudioSource oneShotSource)
        {
            optionsList = new();
            foreach (var entry in optionsEntries)
            {
                optionsList.Add(new(entry, false, false, false));
            }
            APRandomizer.OWMLModConsole.WriteLine("Location Checklist Mode Created", OWML.Common.MessageType.Success);
        }

        // Runs when the mode is opened in the ship computer
        public override void EnterMode(string entryID = "", List<ShipLogFact> revealQueue = null)
        {
            IsInChecklist = false;
            OpenSelectionMode();
        }

        // Runs when the mode is closed
        public override void ExitMode()
        {
            SelectionWrapper.Close();
            ChecklistWrapper.Close();
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
            int changeIndex = IsInChecklist ? ChecklistWrapper.UpdateList() : SelectionWrapper.UpdateList();

            if (changeIndex != 0)
            {
                selectedIndex += changeIndex;

                int listLength = IsInChecklist ? Tracker.CurrentLocations.Count : optionsList.Count;
                if (selectedIndex < 0) selectedIndex = listLength - 1;
                if (selectedIndex >= listLength) selectedIndex = 0;

                if (IsInChecklist)
                {
                    SelectChecklistItem(selectedIndex);
                }
                else
                {
                    
                }
            }

            if (IsInChecklist)
            {
                if (OWInput.IsNewlyPressed(InputLibrary.cancel))
                {
                    ChecklistWrapper.Close();
                    OpenSelectionMode();
                }
            }
            else
            {
                if (OWInput.IsNewlyPressed(InputLibrary.menuConfirm))
                {
                    OpenChecklistPage(selectedIndex);
                }
            }
        }

        // Allows leaving the computer in this mode
        public override bool AllowCancelInput()
        {
            return !IsInChecklist; 
        }

        // Allows swapping modes while in this mode
        public override bool AllowModeSwap()
        {
            return !IsInChecklist; 
        }

        // Returns the ID of the selected ship entry, used for knowing which entry should be highlighted when switching to Map Mode. Useless for us probably.
        public override string GetFocusedEntryID()
        {
            return "";
        }

        #region checklist
        private void OpenChecklistPage(int index)
        {
            string pageName;
            switch (index)
            {
                case 1:
                    {
                        PopulateInfos(TrackerCategory.HourglassTwins);
                        pageName = "Hourglass Twins";
                        break;
                    }
                case 2:
                    {
                        PopulateInfos(TrackerCategory.TimberHearth);
                        pageName = "Timber Hearth";
                        break;
                    }
                case 3:
                    {
                        PopulateInfos(TrackerCategory.BrittleHollow);
                        pageName = "Brittle Hollow";
                        break;
                    }
                case 4:
                    {
                        PopulateInfos(TrackerCategory.GiantsDeep);
                        pageName = "Giant's Deep";
                        break;
                    }
                case 5:
                    {
                        PopulateInfos(TrackerCategory.DarkBramble);
                        pageName = "Dark Bramble";
                        break;
                    }
                case 6:
                    {
                        PopulateInfos(TrackerCategory.OuterWilds);
                        pageName = "The Outer Wilds";
                        break;
                    }
                default:
                    {
                        // We don't need to care about switching to the checklist if an invalid entry is selected
                        return;
                    }
            }
            SelectionWrapper.Close();
            ChecklistWrapper.Open();
            ChecklistWrapper.SetName(pageName);
            ChecklistWrapper.SetItems(Tracker.CurrentLocations);
            ChecklistWrapper.SetSelectedIndex(0);
            ChecklistWrapper.UpdateList();
            selectedIndex = 0;
            RootObject.name = "ArchipelagoChecklistMode";

            IsInChecklist = true;
            SelectChecklistItem(0);
        }

        private void SelectChecklistItem(int index)
        {
            TrackerInfo info = Tracker.Infos.ElementAt(index).Value;
            ChecklistWrapper.GetPhoto().sprite = GetShipLogImage(info.thumbnail);
            ChecklistWrapper.GetPhoto().gameObject.SetActive(true);
            ChecklistWrapper.GetQuestionMark().gameObject.SetActive(false);
            ChecklistWrapper.DescriptionFieldClear();
            ChecklistWrapper.DescriptionFieldGetNextItem().DisplayText(info.description);
            ChecklistWrapper.DescriptionFieldGetNextItem().DisplayText("Full name: " + Tracker.GetLocationByName(info).name);
            ChecklistWrapper.DescriptionFieldGetNextItem().DisplayText(Tracker.GetLogicString(Tracker.GetLocationByName(info)));
        }

        public void PopulateInfos(TrackerCategory category)
        {
            Tracker.Infos = new Dictionary<string, TrackerInfo>();
            string filepath = APRandomizer.Instance.ModHelper.Manifest.ModFolderPath + "/InGameTracker/LocationInfos/" + GetTrackerInfoFilename(category);
            if (File.Exists(filepath + ".jsonc"))
            {
                List<TrackerInfo> trackerInfos = JsonConvert.DeserializeObject<List<TrackerInfo>>(File.ReadAllText(filepath + ".jsonc"));
                foreach (TrackerInfo info in trackerInfos)
                {
                    Tracker.Infos.Add(info.locationModID, info);
                }

                if (APRandomizer.SlotData.ContainsKey("logsanity"))
                {
                    if ((long)APRandomizer.SlotData["logsanity"] != 0)
                    {
                        if (File.Exists(filepath + "_SL.jsonc"))
                        {
                            trackerInfos = JsonConvert.DeserializeObject<List<TrackerInfo>>(File.ReadAllText(filepath + "_SL.jsonc"));
                            foreach (TrackerInfo info in trackerInfos)
                            {
                                Tracker.Infos.Add(info.locationModID, info);
                            }
                        }
                    }
                }
                else APRandomizer.OWMLModConsole.WriteLine("No logsanity key found in Slot Data!");

                Tracker.GenerateLocationChecklist();
            }
            else APRandomizer.OWMLModConsole.WriteLine($"Unable to locate file at {filepath + ".jsonc"}!", OWML.Common.MessageType.Error);
        }

        private string GetTrackerInfoFilename(TrackerCategory category)
        {
            string filename = "";
            switch (category)
            {
                case TrackerCategory.HourglassTwins:
                    filename = "HT";
                    break;
                case TrackerCategory.TimberHearth:
                    filename = "TH";
                    break;
                case TrackerCategory.BrittleHollow:
                    filename = "BH";
                    break;
                case TrackerCategory.GiantsDeep:
                    filename = "GD";
                    break;
                case TrackerCategory.DarkBramble:
                    filename = "DB";
                    break;
                case TrackerCategory.OuterWilds:
                    filename = "OW";
                    break;
                case TrackerCategory.Stranger:
                    filename = "ST";
                    break;
                case TrackerCategory.Dreamworld:
                    filename = "DW";
                    break;
                default:
                    APRandomizer.OWMLModConsole.WriteLine($"Unable to parse {category} into a filename prefix, leaving blank", OWML.Common.MessageType.Error);
                    break;
            }
            return filename;
        }
        
        // gets the ship log image for the associated fact
        private Sprite GetShipLogImage(string fact)
        {
            if (string.IsNullOrEmpty(fact))
            {
                return TrackerManager.GetSprite("PLACEHOLDER");
            }
            if (shipLogPanRoot == null) shipLogPanRoot = Locator.GetShipBody().gameObject.transform.Find("Module_Cabin/Systems_Cabin/ShipLogPivot/ShipLog/ShipLogPivot/ShipLogCanvas/DetectiveMode/ScaleRoot/PanRoot").gameObject;
            Sprite sprite = shipLogPanRoot.transform.Find($"{fact}/EntryCardRoot/EntryCardBackground/PhotoImage").GetComponent<Image>().sprite;
            return sprite;
        }
        #endregion

        #region Selection

        private void OpenSelectionMode()
        {
            SelectionWrapper.Open();
            SelectionWrapper.SetName("AP Tracker");
            SelectionWrapper.SetItems(optionsList);
            SelectionWrapper.SetSelectedIndex(0);
            SelectionWrapper.UpdateList();
            selectedIndex = 0;
            RootObject.name = "ArchipelagoSelectorMode";

            IsInChecklist = false;
            SelectSelectionItem(0);
        }

        private void SelectSelectionItem(int index)
        {

        }

        #endregion
    }
}
