using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

namespace ArchipelagoRandomizer.InGameTracker
{
    public class TrackerItemChecklistMode : ShipLogMode
    {
        public ItemListWrapper Wrapper;
        public GameObject RootObject;
        public TrackerManager Tracker;
        public Dictionary<string, TrackerInfo> Infos;

        // Runs when the mode is created
        public override void Initialize(ScreenPromptList centerPromptList, ScreenPromptList upperRightPromptList, OWAudioSource oneShotSource)
        {
            
        }

        // Runs when the mode is opened in the ship computer
        public override void EnterMode(string entryID = "", List<ShipLogFact> revealQueue = null)
        {
            
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

        public void PopulateInfos(TrackerCategory category)
        {
            string filepath = APRandomizer.Instance.ModHelper.Manifest.ModFolderPath + "/InGameTracker/LocationInfos/" + GetTrackerInfoFilename(category);
            if (File.Exists(filepath + ".jsonc"))
            {
                string locations = File.ReadAllText(filepath + ".jsonc");
                List<TrackerInfo> trackerInfos = JsonConvert.DeserializeObject<List<TrackerInfo>>(locations);
                foreach (TrackerInfo info in trackerInfos)
                {
                    
                }
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
    }
}
