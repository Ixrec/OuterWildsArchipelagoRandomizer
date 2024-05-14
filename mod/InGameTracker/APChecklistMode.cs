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

public class APChecklistMode : ShipLogMode
{
    public List<Tuple<string, bool, bool, bool>> CurrentLocations;

    /// <summary>
    /// List of all locations and associated info for the currently selected category in the tracker
    /// </summary>
    public Dictionary<string, TrackerInfo> Infos;

    public ItemListWrapper ChecklistWrapper;
    public ItemListWrapper SelectionWrapper;
    
    public GameObject SelectionRootObject;
    public GameObject ChecklistRootObject;
    public TrackerManager Tracker;

    public bool IsInChecklist = false;

    private GameObject shipLogPanRoot;
    Dictionary<string, TrackerChecklistData> checklist;

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
    private const string so7 = "#92FEF0";
    private const string completed = "#82BD8E";
    private const string noAccessible = "red";

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
        
        if (Victory.goalSetting == Victory.GoalSetting.SongOfFive)
        {
            victoryCondition = $"<color={so5}>THE SONG OF FIVE</color>";
        }
        else if (Victory.goalSetting == Victory.GoalSetting.SongOfSix)
        {
            victoryCondition = $"<color={so6}>THE SONG OF SIX</color>";
        }
        else
        {
            victoryCondition = $"<color={so7}>THE SONG OF SEVEN</color>";
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
            case 1: category = TrackerCategory.HourglassTwins; break;
            case 2: category = TrackerCategory.TimberHearth; break;
            case 3: category = TrackerCategory.BrittleHollow; break;
            case 4: category = TrackerCategory.GiantsDeep; break;
            case 5: category = TrackerCategory.DarkBramble; break;
            case 6: category = TrackerCategory.OuterWilds; break;
            // We don't need to care about switching to the checklist if an invalid entry is selected
            default: return;
        }
        PopulateInfos(category);
        SelectionWrapper.Close();
        ChecklistWrapper.Open();
        ChecklistWrapper.SetName(CategoryToLongName(category));
        ChecklistWrapper.SetItems(CurrentLocations);
        ChecklistWrapper.SetSelectedIndex(0);
        ChecklistWrapper.UpdateList();
        ChecklistRootObject.name = "ArchipelagoChecklistMode";

        IsInChecklist = true;
        SelectChecklistItem(0);
    }

    private void SelectChecklistItem(int index)
    {
        TrackerInfo info = Infos.ElementAt(index).Value;
        TrackerLocationData data = Tracker.logic.GetLocationByName(info);
        TrackerChecklistData locData = checklist[data.name];
        ChecklistWrapper.GetPhoto().sprite = GetShipLogImage(info.thumbnail);
        ChecklistWrapper.GetPhoto().gameObject.SetActive(true);
        ChecklistWrapper.GetQuestionMark().gameObject.SetActive(false);
        ChecklistWrapper.DescriptionFieldClear();
        ChecklistWrapper.DescriptionFieldGetNextItem().DisplayText(info.description);
        if (locData.hintText != "" && !locData.hasBeenChecked)
        {
            ChecklistWrapper.DescriptionFieldGetNextItem().DisplayText(locData.hintText);
        }
        ChecklistWrapper.DescriptionFieldGetNextItem().DisplayText("<color=#8DCEFF>Full name: " + data.name + "</color>");
        ChecklistWrapper.DescriptionFieldGetNextItem().DisplayText(Tracker.logic.GetLocationLogicString(data));
        ChecklistWrapper.DescriptionFieldGetNextItem().DisplayText(Tracker.logic.GetRegionLogicString(data.region));
    }

    public void PopulateInfos(TrackerCategory category)
    {
        Infos = new Dictionary<string, TrackerInfo>();
        string filepath = APRandomizer.Instance.ModHelper.Manifest.ModFolderPath + "/InGameTracker/LocationInfos/" + GetTrackerInfoFilename(category);
        if (File.Exists(filepath + ".jsonc"))
        {
            List<TrackerInfo> trackerInfos = JsonConvert.DeserializeObject<List<TrackerInfo>>(File.ReadAllText(filepath + ".jsonc"));
            foreach (TrackerInfo info in trackerInfos)
            {
                Infos.Add(info.locationModID, info);
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
                            Infos.Add(info.locationModID, info);
                        }
                    }
                }
            }
            checklist = Tracker.logic.GetLocationChecklist(category);
            GenerateLocationChecklist(category);
        }
        else APRandomizer.OWMLModConsole.WriteLine($"Unable to locate file at {filepath + ".jsonc"}!", OWML.Common.MessageType.Error);
    }

    public void GenerateLocationChecklist(TrackerCategory category)
    {
        CurrentLocations = new();
        Dictionary<string, TrackerChecklistData> checklistDatas = Tracker.logic.GetLocationChecklist(category);
        foreach (TrackerInfo info in Infos.Values)
        {
            // TODO add hints and confirmation of checked locations
            if (Enum.TryParse<Location>(info.locationModID, out Location loc))
            {
                if (!LocationNames.locationToArchipelagoId.ContainsKey(loc))
                {
                    APRandomizer.OWMLModConsole.WriteLine($"Unable to find Location {loc}!", OWML.Common.MessageType.Warning);
                    continue;
                }
                if (!checklistDatas.ContainsKey(Tracker.logic.GetLocationByName(info).name))
                {
                    APRandomizer.OWMLModConsole.WriteLine($"Unable to find the location {Tracker.logic.GetLocationByName(info).name} in the given checklist!", OWML.Common.MessageType.Error);
                    continue;
                }
                TrackerChecklistData data = checklistDatas[Tracker.logic.GetLocationByName(info).name];
                long id = LocationNames.locationToArchipelagoId[loc];
                bool locationChecked = data.hasBeenChecked;
                string name = Tracker.logic.GetLocationByID(id).name;
                // Shortens the display name by removing "Ship Log", the region prefix, and the colon from the name
                name = Regex.Replace(name, ".*:.{1}", "");

                string colorTag;
                if (locationChecked) colorTag = "white";
                else if (data.isAccessible) colorTag = "lime";
                else colorTag = "red";

                CurrentLocations.Add(new($"<color={colorTag}>[{(locationChecked ? "X" : " ")}] {name}</color>", false, false, !string.IsNullOrEmpty(data.hintText)));
            }
            else
            {
                APRandomizer.OWMLModConsole.WriteLine($"Unable to find location {info.locationModID} for the checklist! Skipping.", OWML.Common.MessageType.Warning);
            }
        }
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
        for (int i = 0; i < optionsEntries.Length; i++)
        {
            var entry = optionsEntries[i];

            TrackerCategory category = (TrackerCategory)i;
            bool hasAvailableChecks = false;
            bool hasHint = false;
            if (i != 0)
            {
                hasAvailableChecks = Tracker.logic.GetAccessibleCount(category) - Tracker.logic.GetCheckedCount(category) > 0;
                hasHint = Tracker.logic.GetHasHint(category);
            }
            optionsList.Add(new(entry, false, hasAvailableChecks, hasHint));
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
                case 1:
                    category = TrackerCategory.HourglassTwins;
                    break;
                case 2:
                    category = TrackerCategory.TimberHearth;
                    break;
                case 3:
                    category = TrackerCategory.BrittleHollow;
                    break;
                case 4:
                    category = TrackerCategory.GiantsDeep;
                    break;
                case 5:
                    category = TrackerCategory.DarkBramble;
                    break;
                case 6:
                    category = TrackerCategory.OuterWilds;
                    break;
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
            case TrackerCategory.HourglassTwins: return "The Hourglass Twins";
            case TrackerCategory.TimberHearth: return "Timber Hearth";
            case TrackerCategory.BrittleHollow: return "Brittle Hollow";
            case TrackerCategory.GiantsDeep: return "Giant's Deep";
            case TrackerCategory.DarkBramble: return "Dark Bramble";
            case TrackerCategory.OuterWilds: return "The Outer Wilds";
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
