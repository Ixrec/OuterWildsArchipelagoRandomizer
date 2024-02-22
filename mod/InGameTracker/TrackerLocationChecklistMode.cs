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
        public ItemListWrapper Wrapper;
        public GameObject RootObject;
        public TrackerManager Tracker;

        private int selectedIndex;
        private GameObject shipLogPanRoot;


        // Runs when the mode is created
        public override void Initialize(ScreenPromptList centerPromptList, ScreenPromptList upperRightPromptList, OWAudioSource oneShotSource)
        {
            APRandomizer.OWMLModConsole.WriteLine("Location Checklist Mode Created");
        }

        // Runs when the mode is opened in the ship computer
        public override void EnterMode(string entryID = "", List<ShipLogFact> revealQueue = null)
        {
            PopulateInfos(TrackerCategory.TimberHearth);
            Wrapper.Open();
            Wrapper.SetName("Timber Hearth");
            Wrapper.SetItems(Tracker.CurrentLocations);
            Wrapper.SetSelectedIndex(0);
            Wrapper.UpdateList();
            selectedIndex = 0;
            RootObject.name = "ArchipelagoChecklistMode";

            SelectItem(0);
        }

        // Runs when the mode is closed
        public override void ExitMode()
        {
            
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

                if (selectedIndex < 0) selectedIndex = Tracker.CurrentLocations.Count - 1;
                if (selectedIndex >= Tracker.CurrentLocations.Count) selectedIndex = 0;

                SelectItem(selectedIndex);
            }
        }

        // Allows leaving the computer in this mode
        public override bool AllowCancelInput()
        {
            return true; //change to false later
        }

        // Allows swapping modes while in this mode
        public override bool AllowModeSwap()
        {
            return true; //change to false later
        }

        // Returns the ID of the selected ship entry, used for knowing which entry should be highlighted when switching to Map Mode. Useless for us probably.
        public override string GetFocusedEntryID()
        {
            return "";
        }

        private void SelectItem(int index)
        {
            TrackerInfo info = Tracker.Infos.ElementAt(index).Value;
            Wrapper.GetPhoto().sprite = GetShipLogImage(info.thumbnail);
            Wrapper.GetPhoto().gameObject.SetActive(true);
            Wrapper.GetQuestionMark().gameObject.SetActive(false);
            Wrapper.DescriptionFieldClear();
            Wrapper.DescriptionFieldGetNextItem().DisplayText(info.description);
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
            if (shipLogPanRoot == null) shipLogPanRoot = Locator.GetShipBody().gameObject.transform.Find("Module_Cabin/Systems_Cabin/ShipLogPivot/ShipLog/ShipLogPivot/ShipLogCanvas/DetectiveMode/ScaleRoot/PanRoot").gameObject;
            Sprite sprite = shipLogPanRoot.transform.Find($"{fact}/EntryCardRoot/EntryCardBackground/PhotoImage").GetComponent<Image>().sprite;
            return sprite;
        }
    }
}
