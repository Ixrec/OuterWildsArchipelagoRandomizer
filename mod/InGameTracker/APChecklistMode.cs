using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace ArchipelagoRandomizer.InGameTracker;

using ShipLogDisplayItem = Tuple<string, bool, bool, bool>;

public class APChecklistMode : ShipLogMode
{
    /// <summary>
    /// List of all locations and associated info for the currently selected category in the tracker
    /// </summary>
    private List<(TrackerInfo, ShipLogDisplayItem)> ChecklistState;

    public ItemListWrapper ChecklistWrapper;
    public ItemListWrapper SelectionWrapper;
    
    public GameObject SelectionRootObject;
    public GameObject ChecklistRootObject;
    public TrackerManager Tracker;

    public bool IsInChecklist = false;

    private GameObject shipLogPanRoot;
    Dictionary<string, TrackerChecklistData> LocationNameToChecklistData;

    private Material selectorMaterial;
    private Dictionary<string, GameObject> icons;
    private GameObject selectorInfo;
    private GameObject missionGroup;
    private Text missionStatement;
    private Text missionTags;
    private GameObject bodyGroup;
    private Text bodyName;
    private Text locationsChecked;
    private Text locationsAccessible;
    private Text moreToExplore;

    private string victoryCondition;
    private string apTags;

    // colors for various string tags
    private const string so5 = "orange";
    private const string so6 = "#6C81FE";
    private const string so7 = "#92FEA3";
    private const string completed = "#82BD8E";
    private const string noAccessible = "red";

    private readonly (string, TrackerCategory)[] optionsEntries =
    {
        ("Mission", TrackerCategory.Goal),
        ("Hourglass Twins", TrackerCategory.HourglassTwins),
        ("Timber Hearth", TrackerCategory.TimberHearth),
        ("Brittle Hollow", TrackerCategory.BrittleHollow),
        ("Giant's Deep", TrackerCategory.GiantsDeep),
        ("Dark Bramble", TrackerCategory.DarkBramble),
        ("The Outer Wilds", TrackerCategory.OuterWilds),
        ("The Stranger", TrackerCategory.Stranger),
        ("Dreamworld", TrackerCategory.Dreamworld),
    };
    private List<ShipLogDisplayItem> optionsList;

    private static Dictionary<Victory.GoalSetting, (string, string, string)> goalDisplayMetadata = new Dictionary<Victory.GoalSetting, (string, string, string)> {
        { Victory.GoalSetting.SongOfFive, ("Victory - Song of Five", "Reach the Eye", "DB_VESSEL") },
        { Victory.GoalSetting.SongOfTheNomai, ("Victory - Song of the Nomai", "Reach the Eye after meeting Solanum", "QM_SIXTH_LOCATION") },
        { Victory.GoalSetting.SongOfTheStranger, ("Victory - Song of the Stranger", "Reach the Eye after meeting the Prisoner", "IP_SARCOPHAGUS") },
        { Victory.GoalSetting.SongOfSix, ("Victory - Song of Six", "Reach the Eye after meeting either Solanum or the Prisoner", "DB_VESSEL") },
        { Victory.GoalSetting.SongOfSeven, ("Victory - Song of Seven", "Reach the Eye after meeting both Solanum and the Prisoner", "DB_VESSEL") },
        { Victory.GoalSetting.EchoesOfTheEye, ("Victory - Echoes of the Eye", "Meet the Prisoner and complete the DLC", "IP_SARCOPHAGUS") },
    };

    // Runs when the mode is created
    public override void Initialize(ScreenPromptList centerPromptList, ScreenPromptList upperRightPromptList, OWAudioSource oneShotSource)
    {
        switch (Victory.goalSetting)
        {
            case Victory.GoalSetting.SongOfFive:
                victoryCondition = $"<color={so5}>THE SONG OF FIVE</color>";
                break;
            case Victory.GoalSetting.SongOfSix:
                victoryCondition = $"<color={so6}>THE SONG OF SIX</color>";
                break;
            case Victory.GoalSetting.SongOfSeven:
                victoryCondition = $"<color={so7}>THE SONG OF SEVEN</color>";
                break;
            case Victory.GoalSetting.SongOfTheNomai:
                victoryCondition = $"<color={so6}>THE SONG OF THE NOMAI</color>";
                break;
            case Victory.GoalSetting.SongOfTheStranger:
                victoryCondition = $"<color={so7}>THE SONG OF THE STRANGER</color>";
                break;
            case Victory.GoalSetting.EchoesOfTheEye:
                victoryCondition = $"<color={so7}>THE ECHOES OF THE EYE</color>";
                break;
        }

        var slotData = APRandomizer.SlotData;
        apTags = $"Logsanity: {((long)slotData["logsanity"] != 0 ? "On" : "Off")}\nDeathlink: {((long)slotData["death_link"] != 0 ? "On" : "Off")}";
    }

    // Runs when the mode is opened in the ship computer
    public override void EnterMode(string entryID = "", List<ShipLogFact> revealQueue = null)
    {
        IsInChecklist = false;
        if (selectorInfo == null)
        {
            CreateTrackerUI();
        }
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
        int changeIndex;

        if (IsInChecklist)
        {
            changeIndex = ChecklistWrapper.UpdateList();
            if (changeIndex != 0)
            {
                SelectChecklistItem(ChecklistWrapper.GetSelectedIndex());
            }
            if (OWInput.IsNewlyPressed(InputLibrary.cancel))
            {
                ChecklistWrapper.Close();
                OpenSelectionMode();
            }
        }
        else
        {
            changeIndex = SelectionWrapper.UpdateList();

            if (changeIndex != 0)
            {
                SelectSelectionItem(SelectionWrapper.GetSelectedIndex());
            }
            if (OWInput.IsNewlyPressed(InputLibrary.menuConfirm))
            {
                OpenChecklistPage(SelectionWrapper.GetSelectedIndex());
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
        TrackerCategory category;
        switch (index)
        {
            case 0: category = TrackerCategory.Goal; break;
            case 1: category = TrackerCategory.HourglassTwins; break;
            case 2: category = TrackerCategory.TimberHearth; break;
            case 3: category = TrackerCategory.BrittleHollow; break;
            case 4: category = TrackerCategory.GiantsDeep; break;
            case 5: category = TrackerCategory.DarkBramble; break;
            case 6: category = TrackerCategory.OuterWilds; break;
            // TODO: story mods break this assumption
            case 7: category = TrackerCategory.Stranger; break;
            case 8: category = TrackerCategory.Dreamworld; break;
            // We don't need to care about switching to the checklist if an invalid entry is selected
            default: return;
        }

        LocationNameToChecklistData = Tracker.logic.GetLocationChecklist(category);
        ChecklistState = PopulateChecklistState(category, LocationNameToChecklistData);
        SelectionWrapper.Close();
        ChecklistWrapper.Open();
        ChecklistWrapper.SetName(CategoryToLongName(category));
        ChecklistWrapper.SetItems(ChecklistState.Select(trackerAndDisplayInfo => trackerAndDisplayInfo.Item2).ToList());
        ChecklistWrapper.SetSelectedIndex(0);
        ChecklistWrapper.UpdateList();
        ChecklistRootObject.name = "ArchipelagoChecklistMode";

        IsInChecklist = true;
        SelectChecklistItem(0);
    }

    private void SelectChecklistItem(int index)
    {
        TrackerInfo info = ChecklistState.ElementAt(index).Item1;
        TrackerLocationData data = null;
        if (info.locationModID != null)
            data = Tracker.logic.GetLocationDataByInfo(info);
        else
        {
            string goalLocation = goalDisplayMetadata[Victory.goalSetting].Item1;
            data = Tracker.logic.TrackerLocations[goalLocation];
        }
        TrackerChecklistData locData = LocationNameToChecklistData[data.name];
        ChecklistWrapper.GetPhoto().sprite = GetShipLogImage(info.thumbnail);
        ChecklistWrapper.GetPhoto().gameObject.SetActive(true);
        ChecklistWrapper.GetQuestionMark().gameObject.SetActive(false);
        ChecklistWrapper.DescriptionFieldClear();
        ChecklistWrapper.DescriptionFieldGetNextItem().DisplayText(info.description);
        if (locData.hintText != "" && !locData.hasBeenChecked)
            ChecklistWrapper.DescriptionFieldGetNextItem().DisplayText(locData.hintText);
        if (info.locationModID != null)
            ChecklistWrapper.DescriptionFieldGetNextItem().DisplayText("<color=#8DCEFF>Full name: " + data.name + "</color>");

        foreach (var text in Tracker.logic.GetLogicDisplayStrings(data))
            ChecklistWrapper.DescriptionFieldGetNextItem().DisplayText(text);
    }

    public List<(TrackerInfo, ShipLogDisplayItem)> PopulateChecklistState(TrackerCategory category, Dictionary<string, TrackerChecklistData> locationNameToChecklistData)
    {
        // This is a special case where we will only ever have one item in the list
        if (category == TrackerCategory.Goal) {
            if (!goalDisplayMetadata.ContainsKey(Victory.goalSetting))
                throw new ArgumentException($"Victory.goalSetting had an invalid value of {Victory.goalSetting}");
            var goalMetadata = goalDisplayMetadata[Victory.goalSetting];
            string goalEventName = goalMetadata.Item1;
            TrackerInfo info = new();
            info.description = goalMetadata.Item2;
            info.thumbnail = goalMetadata.Item3;

            string displayName = Regex.Replace(goalEventName, "Victory - ", "");

            if (APRandomizer.APSession.DataStorage.GetClientStatus(APRandomizer.APSession.ConnectionInfo.Slot) == Archipelago.MultiClient.Net.Enums.ArchipelagoClientState.ClientGoal)
                return new List<(TrackerInfo, ShipLogDisplayItem)>{ (info, new($"<color=white>[X] {displayName}</color>", false, false, false)) };
            else if (locationNameToChecklistData[goalEventName].isAccessible)
                return new List<(TrackerInfo, ShipLogDisplayItem)> { (info, new($"<color=lime>[ ] {displayName}</color>", false, false, false)) };
            else
                return new List<(TrackerInfo, ShipLogDisplayItem)> { (info, new($"<color=red>[ ] {displayName}</color>", false, false, false)) };
        }

        // we'll be "sorting" (well, partitioning?) the locations into these three groups
        List<(TrackerInfo, ShipLogDisplayItem)> checkedState = new();
        List<(TrackerInfo, ShipLogDisplayItem)> accessibleState = new();
        List<(TrackerInfo, ShipLogDisplayItem)> inaccessibleState = new();

        string filepath = APRandomizer.Instance.ModHelper.Manifest.ModFolderPath + "/InGameTracker/LocationInfos/" + GetTrackerInfoFilename(category);
        if (File.Exists(filepath + ".jsonc"))
        {
            HashSet<TrackerInfo> trackerInfosInCategory = new();

            List<TrackerInfo> trackerInfos = JsonConvert.DeserializeObject<List<TrackerInfo>>(File.ReadAllText(filepath + ".jsonc"));
            foreach (TrackerInfo info in trackerInfos)
                trackerInfosInCategory.Add(info);

            if (APRandomizer.SlotEnabledLogsanity() && File.Exists(filepath + "_SL.jsonc"))
            {
                trackerInfos = JsonConvert.DeserializeObject<List<TrackerInfo>>(File.ReadAllText(filepath + "_SL.jsonc"));
                foreach (TrackerInfo info in trackerInfos)
                    trackerInfosInCategory.Add(info);
            }

            // generate the ShipLogDisplayItem for each TrackerInfo, and add each pair to checklistState
            foreach (TrackerInfo info in trackerInfosInCategory)
            {
                if (info.isDLCOnly && !APRandomizer.SlotEnabledEotEDLC()) continue;

                // TODO add hints and confirmation of checked locations
                if (Enum.TryParse<Location>(info.locationModID, out Location loc))
                {
                    if (!LocationNames.locationToArchipelagoId.ContainsKey(loc))
                    {
                        APRandomizer.OWMLModConsole.WriteLine($"Unable to find Location {loc}!", OWML.Common.MessageType.Warning);
                        continue;
                    }
                    if (!locationNameToChecklistData.ContainsKey(Tracker.logic.GetLocationDataByInfo(info).name))
                    {
                        APRandomizer.OWMLModConsole.WriteLine($"Unable to find the location {Tracker.logic.GetLocationDataByInfo(info).name} in the given checklist!", OWML.Common.MessageType.Error);
                        continue;
                    }
                    TrackerChecklistData data = locationNameToChecklistData[Tracker.logic.GetLocationDataByInfo(info).name];
                    long id = LocationNames.locationToArchipelagoId[loc];
                    bool locationChecked = data.hasBeenChecked;
                    string name = Tracker.logic.GetLocationByID(id).name;
                    // Shortens the display name by removing "Ship Log", the region prefix, and the colon from the name
                    name = Regex.Replace(name, ".*:.{1}", "");

                    var hasHint = !string.IsNullOrEmpty(data.hintText);
                    if (locationChecked)
                        checkedState.Add((info, new($"<color=white>[X] {name}</color>", false, false, hasHint)));
                    else if (data.isAccessible)
                        accessibleState.Add((info, new($"<color=lime>[ ] {name}</color>", false, false, hasHint)));
                    else
                        inaccessibleState.Add((info, new($"<color=red>[ ] {name}</color>", false, false, hasHint)));
                }
                else
                {
                    APRandomizer.OWMLModConsole.WriteLine($"Unable to find location {info.locationModID} for the checklist! Skipping.", OWML.Common.MessageType.Warning);
                }
            }
        }
        else APRandomizer.OWMLModConsole.WriteLine($"Unable to locate file at {filepath + ".jsonc"}!", OWML.Common.MessageType.Error);

        // here we define the sort/partition order we want the user to see
        return accessibleState.Concat(inaccessibleState).Concat(checkedState).ToList();
    }

    private Dictionary<TrackerCategory, string> categoryToFilenamePrefix = new Dictionary<TrackerCategory, string> {
        { TrackerCategory.HourglassTwins, "HT" },
        { TrackerCategory.TimberHearth, "TH" },
        { TrackerCategory.BrittleHollow, "BH" },
        { TrackerCategory.GiantsDeep, "GD" },
        { TrackerCategory.DarkBramble, "DB" },
        { TrackerCategory.OuterWilds, "OW" },
        { TrackerCategory.Stranger, "ST" },
        { TrackerCategory.Dreamworld, "DW" },
    };

    private string GetTrackerInfoFilename(TrackerCategory category)
    {
        if (categoryToFilenamePrefix.TryGetValue(category, out var filenamePrefix))
            return filenamePrefix;
        else
            APRandomizer.OWMLModConsole.WriteLine($"Unable to parse {category} into a filename prefix, leaving blank", OWML.Common.MessageType.Error);
        return "";
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
    private void CreateTrackerUI()
    {
        selectorInfo = GameObject.Instantiate(APRandomizer.Assets.LoadAsset<GameObject>("APTracker"), SelectionRootObject.transform);
        RectTransform rect = selectorInfo.GetComponent<RectTransform>();
        rect.localScale = Vector3.one;
        rect.anchorMax = Vector3.one;
        rect.anchoredPosition = Vector3.zero;
        rect.anchoredPosition3D = Vector3.zero;

        icons = new();
        foreach (Transform child in selectorInfo.transform.Find("Tracker/PlanetHolder"))
        {
            icons.Add(child.name, child.gameObject);
        }
        missionGroup = selectorInfo.transform.Find("Tracker/MissionText").gameObject;
        missionStatement = missionGroup.transform.Find("MissionStatement").GetComponent<Text>();
        missionStatement.text = victoryCondition;
        missionTags = missionGroup.transform.Find("MissionTags").GetComponent<Text>();
        missionTags.text = apTags;
        bodyGroup = selectorInfo.transform.Find("Tracker/TrackerText").gameObject;
        bodyName = bodyGroup.transform.Find("BodyName").GetComponent<Text>();
        locationsChecked = bodyGroup.transform.Find("LocationsChecked").GetComponent<Text>();
        locationsAccessible = bodyGroup.transform.Find("LocationsAccessible").GetComponent<Text>();
        moreToExplore = bodyGroup.transform.Find("MoreToExplore").GetComponent<Text>();
        selectorMaterial = icons["PlanetTH"].GetComponent<Image>().material;
    }

    private void OpenSelectionMode()
    {
        optionsList = new();
        foreach (var (text, category) in optionsEntries)
        {
            if (category == TrackerCategory.Stranger || category == TrackerCategory.Dreamworld)
                if (!APRandomizer.SlotEnabledEotEDLC())
                    continue;

            bool hasAvailableChecks = false;
            bool hasHint = false;
            if (category == TrackerCategory.Goal)
            {
                string goalLocation = goalDisplayMetadata[Victory.goalSetting].Item1;
                hasAvailableChecks = Tracker.logic.IsAccessible(Tracker.logic.TrackerLocations[goalLocation]);
            }
            else
            {
                hasAvailableChecks = Tracker.logic.GetAccessibleCount(category) - Tracker.logic.GetCheckedCount(category) > 0;
                hasHint = Tracker.logic.GetHasHint(category);
            }
            optionsList.Add(new(text, false, hasAvailableChecks, hasHint));
        }

        SelectionWrapper.Open();
        SelectionWrapper.SetName("AP Tracker");
        SelectionWrapper.SetItems(optionsList);
        SelectionWrapper.SetSelectedIndex(0);
        SelectionWrapper.UpdateList();
        SelectionRootObject.name = "ArchipelagoSelectorMode";
        IsInChecklist = false;
        SelectSelectionItem(SelectionWrapper.GetSelectedIndex());
    }

    private void SelectSelectionItem(int index)
    {
        // toggle visibility of icons so only the currently selected planet or emblem is visible
        for (int i = 0; i < icons.Count; i++)
        {
            icons.ElementAt(i).Value.SetActive(i == index);
        }
        if (index == 0)
        {
            missionGroup.SetActive(true);
            bodyGroup.SetActive(false);
        }
        else
        {
            missionGroup.SetActive(false);
            bodyGroup.SetActive(true);
            float checkedLocs;
            float accessLocs;
            float allLocs;
            TrackerCategory category = TrackerCategory.All;
            switch (index)
            {
                case 1: category = TrackerCategory.HourglassTwins; break;
                case 2: category = TrackerCategory.TimberHearth; break;
                case 3: category = TrackerCategory.BrittleHollow; break;
                case 4: category = TrackerCategory.GiantsDeep; break;
                case 5: category = TrackerCategory.DarkBramble; break;
                case 6: category = TrackerCategory.OuterWilds; break;
                // TODO: story mods break this assumption
                case 7: category = TrackerCategory.Stranger; break;
                case 8: category = TrackerCategory.Dreamworld; break;
            }
            checkedLocs = Tracker.logic.GetCheckedCount(category);
            accessLocs = Tracker.logic.GetAccessibleCount(category);
            allLocs = Tracker.logic.GetTotalCount(category);
            float accessiblePercentage = accessLocs / allLocs;
            float checkedPercentage = checkedLocs / allLocs;
            selectorMaterial.SetFloat("_PercentAccessible", accessiblePercentage);
            selectorMaterial.SetFloat("_PercentComplete", checkedPercentage);
            bodyName.text = CategoryToName(category);
            locationsChecked.text = $"{checkedLocs} Checked / {allLocs} Total";
            locationsAccessible.text = $"{(int)(accessLocs - checkedLocs)} Accessible / {allLocs - checkedLocs} Remaining";
            string exploreText;
            if (checkedLocs >= allLocs) exploreText = $"<color={completed}>There's no more to explore here.\nGood job!</color>";
            else if (checkedLocs < accessLocs) exploreText = $"<color={so5}>There's more to explore here.</color>";
            else exploreText = $"<color={noAccessible}>There's more to explore here, but you need more items.</color>";
            moreToExplore.text = exploreText;
        }
    }

    #endregion

    private string CategoryToName(TrackerCategory category)
    {
        switch (category)
        {
            case TrackerCategory.All: return "All";
            case TrackerCategory.Goal: return "Mission";
            case TrackerCategory.HourglassTwins: return "The Hourglass Twins";
            case TrackerCategory.TimberHearth: return "Timber Hearth";
            case TrackerCategory.BrittleHollow: return "Brittle Hollow";
            case TrackerCategory.GiantsDeep: return "Giant's Deep";
            case TrackerCategory.DarkBramble: return "Dark Bramble";
            case TrackerCategory.OuterWilds: return "The Outer Wilds";
            case TrackerCategory.Stranger: return "The Stranger";
            case TrackerCategory.Dreamworld: return "Dreamworld";
        }
        return "NULL";
    }

    private string CategoryToLongName(TrackerCategory category)
    {
        switch (category)
        {
            case TrackerCategory.TimberHearth: return "Timber Hearth & Attlerock";
            case TrackerCategory.BrittleHollow: return "Brittle Hollow & Hollow's Lantern";
            case TrackerCategory.GiantsDeep: return "Giant's Deep & Orbital Probe Cannon";
            default: return CategoryToName(category);
        }
    }
}
