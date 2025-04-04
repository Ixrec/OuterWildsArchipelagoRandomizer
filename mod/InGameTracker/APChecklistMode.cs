using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    Dictionary<string, TrackerChecklistData> LocationNameToChecklistData;

    private Material selectorMaterial;
    private Dictionary<string, GameObject> icons;
    private Image modLogoImage;
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

    private readonly (string, TrackerCategory)[] staticChecklistCategories =
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
    private List<ShipLogDisplayItem> checklistCategoryItems;
    private List<TrackerCategory> displayedCategories = null;

    private static Dictionary<Victory.GoalSetting, (string, string, bool, bool, string)> goalDisplayMetadata = new Dictionary<Victory.GoalSetting, (string, string, bool, bool, string)> {
        { Victory.GoalSetting.SongOfFive, ("Victory - Song of Five", "Reach the Eye", false, false, "DB_VESSEL") },
        { Victory.GoalSetting.SongOfTheNomai, ("Victory - Song of the Nomai", "Reach the Eye after meeting Solanum", true, false, "QM_SIXTH_LOCATION") },
        { Victory.GoalSetting.SongOfTheStranger, ("Victory - Song of the Stranger", "Reach the Eye after meeting the Prisoner", false, true, "IP_SARCOPHAGUS") },
        { Victory.GoalSetting.SongOfSix, ("Victory - Song of Six", "Reach the Eye after meeting either Solanum or the Prisoner", true, true, "DB_VESSEL") },
        { Victory.GoalSetting.SongOfSeven, ("Victory - Song of Seven", "Reach the Eye after meeting both Solanum and the Prisoner", true, true, "DB_VESSEL") },
        { Victory.GoalSetting.EchoesOfTheEye, ("Victory - Echoes of the Eye", "Meet the Prisoner and complete the DLC", false, false, "IP_SARCOPHAGUS") },
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
        TrackerCategory category = displayedCategories[index];

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
        Image image = ChecklistWrapper.GetPhoto();
        bool isGoalItem = (info.locationModID == null);
        if (!isGoalItem)
        {
            data = Tracker.logic.GetLocationDataByInfo(info);
            image.sprite = GetShipLogImage(info.thumbnail);
        }
        else
        {
            string goalLocation = goalDisplayMetadata[Victory.goalSetting].Item1;
            data = Tracker.logic.TrackerLocations[goalLocation];

            bool hasCoords = APRandomizer.SaveData.itemsAcquired.ContainsKey(Item.Coordinates) && APRandomizer.SaveData.itemsAcquired[Item.Coordinates] > 0;
            if (hasCoords && Coordinates.shipLogCoordsSprite != null)
                image.sprite = Coordinates.shipLogCoordsSprite;
            else
                image.sprite = GetShipLogImage(info.thumbnail);
        }
        image.gameObject.SetActive(true);
        TrackerChecklistData locData = LocationNameToChecklistData[data.name];
        ChecklistWrapper.GetQuestionMark().gameObject.SetActive(false);
        ChecklistWrapper.DescriptionFieldClear();
        ChecklistWrapper.DescriptionFieldGetNextItem().DisplayText(info.description);
        if (isGoalItem)
            ChecklistWrapper.DescriptionFieldGetNextItem().DisplayText("Once you have the 'Coordinates' AP item, the actual coordinates can be viewed here, " +
                "or in the 'Eye of the Universe Coordinates' entry of the AP Inventory, in addition to the coordinates prompt that appears in the Vessel.");
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
            if (goalMetadata.Item3) // show whether you've met Solanum
                if (Victory.hasMetSolanum())
                    info.description += "\n- <color=lime>You have already met Solanum</color>";
                else
                    info.description += "\n- <color=red>You have not yet met Solanum</color>";
            if (goalMetadata.Item4) // show whether you've met Prisoner
                if (Victory.hasMetPrisoner())
                    info.description += "\n- <color=lime>You have already met the Prisoner</color>";
                else
                    info.description += "\n- <color=red>You have not yet met the Prisoner</color>";
            info.thumbnail = goalMetadata.Item5;

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

            if (APRandomizer.SlotEnabledLogsanity() && File.Exists(filepath + "_SLF.jsonc"))
            {
                trackerInfos = JsonConvert.DeserializeObject<List<TrackerInfo>>(File.ReadAllText(filepath + "_SLF.jsonc"));
                foreach (TrackerInfo info in trackerInfos)
                    trackerInfosInCategory.Add(info);
            }

            // generate the ShipLogDisplayItem for each TrackerInfo, and add each pair to checklistState
            foreach (TrackerInfo info in trackerInfosInCategory)
            {
                // unfortunately DLC-ness doesn't map cleanly onto tracker categories because of the 2 DLC-only tape recorders
                if (info.isDLCOnly && !APRandomizer.SlotEnabledEotEDLC()) continue;
                if (APRandomizer.SlotEnabledDLCOnly())
                    if (!(category == TrackerCategory.Stranger || category == TrackerCategory.Dreamworld) && !info.isDLCOnly)
                        continue;

                if (category == TrackerCategory.HearthsNeighbor && !(APRandomizer.SlotData.ContainsKey("enable_hn1_mod") && (long)APRandomizer.SlotData["enable_hn1_mod"] > 0)) continue;

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
        if (category > TrackerCategory.Dreamworld)
            return StoryModMetadata.TrackerCategoryToModMetadata[category].trackerLocationInfosFilePrefix;
        if (categoryToFilenamePrefix.TryGetValue(category, out var filenamePrefix))
            return filenamePrefix;
        else
            APRandomizer.OWMLModConsole.WriteLine($"Unable to parse {category} into a filename prefix, leaving blank", OWML.Common.MessageType.Error);
        return "";
    }
    
    // gets the ship log image for the associated entry ID
    private Sprite GetShipLogImage(string entryId)
    {
        if (entryId.EndsWith(".png"))
        {
            return TrackerManager.GetSprite(entryId);
        }

        if (string.IsNullOrEmpty(entryId))
        {
            return TrackerManager.GetSprite("PLACEHOLDER");
        }

        Sprite sprite = Locator.GetShipLogManager().GetEntry(entryId)?.GetSprite();

        if (!sprite)
        {
            return TrackerManager.GetSprite("OTHER_SYSTEM_PLACEHOLDER");
        }
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

        var planetHolder = selectorInfo.transform.Find("Tracker/PlanetHolder");
        icons = new();
        foreach (Transform child in planetHolder)
            icons.Add(child.name, child.gameObject);
        GameObject modLogo = new GameObject("APRandomizer_ModLogo");
        modLogo.transform.SetParent(planetHolder, false);
        modLogo.transform.localScale = new Vector3(2, 2, 2); // there must be a better way to adjust image size, but nothing else has worked yet
        modLogoImage = modLogo.AddComponent<Image>();

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
        modLogoImage.material = selectorMaterial;
    }

    private void OpenSelectionMode()
    {
        checklistCategoryItems = new();
        displayedCategories = new();

        List<(string, TrackerCategory)> checklistCategories = staticChecklistCategories.ToList();
        foreach (var (category, mod) in StoryModMetadata.TrackerCategoryToModMetadata)
            checklistCategories.Add((mod.trackerCategoryName, category));

        foreach (var (text, category) in checklistCategories)
        {
            // this is how we hide categories with 0 locations
            if (Tracker.logic.GetTotalCount(category) == 0) continue;
            displayedCategories.Add(category);

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
            checklistCategoryItems.Add(new(text, false, hasAvailableChecks, hasHint));
        }

        SelectionWrapper.Open();
        SelectionWrapper.SetName("AP Tracker");
        SelectionWrapper.SetItems(checklistCategoryItems);
        SelectionWrapper.SetSelectedIndex(0);
        SelectionWrapper.UpdateList();
        SelectionRootObject.name = "ArchipelagoSelectorMode";
        IsInChecklist = false;
        SelectSelectionItem(SelectionWrapper.GetSelectedIndex());
    }

    private Dictionary<TrackerCategory, string> categoryToIcon = new Dictionary<TrackerCategory, string> {
        { TrackerCategory.Goal, "OuterWildsVentures" },
        { TrackerCategory.HourglassTwins, "PlanetHT" },
        { TrackerCategory.TimberHearth, "PlanetTH" },
        { TrackerCategory.BrittleHollow, "PlanetBH" },
        { TrackerCategory.GiantsDeep, "PlanetGD" },
        { TrackerCategory.DarkBramble, "PlanetDB" },
        { TrackerCategory.OuterWilds, "PlanetOW" },
        { TrackerCategory.Stranger, "PlanetStranger" },
        { TrackerCategory.Dreamworld, "PlanetDreamworld" },
    };

    private void SelectSelectionItem(int index)
    {
        TrackerCategory category = displayedCategories[index];

        string iconToShow = categoryToIcon.ContainsKey(category) ? categoryToIcon[category] : null;
        foreach (var (name, go) in icons)
            go.SetActive(name == iconToShow);

        modLogoImage.enabled = (category > TrackerCategory.Dreamworld);
        if (modLogoImage.enabled)
            modLogoImage.sprite = TrackerManager.GetSprite(StoryModMetadata.TrackerCategoryToModMetadata[category].trackerCategoryImageFile);

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
            default: return StoryModMetadata.TrackerCategoryToModMetadata[category].trackerCategoryName;
        }
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
