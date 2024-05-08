using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ArchipelagoRandomizer.InGameTracker;

/// <summary>
/// The inventory screen. All the functions here are required, even if empty.
/// </summary>
public class APInventoryMode : ShipLogMode
{
    public ItemListWrapper Wrapper;
    public GameObject RootObject;
    public TrackerManager Tracker;

    private Image Icon => Wrapper.GetPhoto();
    private Text QuestionMark => Wrapper.GetQuestionMark();

    // Runs when the mode is created
    public override void Initialize(ScreenPromptList centerPromptList, ScreenPromptList upperRightPromptList, OWAudioSource oneShotSource)
    {}

    // Runs when the mode is opened in the ship computer
    public override void EnterMode(string entryID = "", List<ShipLogFact> revealQueue = null)
    {
        Tracker.CheckInventory();
        Wrapper.Open();
        Wrapper.SetName("AP Inventory");
        Wrapper.SetItems(Tracker.InventoryItems);
        Wrapper.SetSelectedIndex(0);
        Wrapper.UpdateList();
        RootObject.name = "ArchipelagoInventoryMode";

        SelectItem(Wrapper.GetSelectedIndex());
    }

    // Runs when the mode is closed
    public override void ExitMode()
    {
        foreach (InventoryItemEntry entry in Tracker.ItemEntries.Values)
        {
            entry.SetNew(false);
        }
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
            SelectItem(Wrapper.GetSelectedIndex());
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
        InventoryItemEntry entry = Tracker.ItemEntries.ElementAt(index).Value;
        string itemID = entry.ID;
        Sprite sprite = TrackerManager.GetSprite(itemID);
        // Only item that doesn't exist is the FrequencyOWV which we want to show as obtained regardless
        if (entry.HasOneOrMore())
        {
            if (sprite != null)
            {
                Icon.sprite = sprite;
                Icon.gameObject.SetActive(true);
                QuestionMark.gameObject.SetActive(false);
            }
        }
        else
        {
            Icon.gameObject.SetActive(false);
            QuestionMark.gameObject.SetActive(true);
        }

        APInventoryDescriptions.DisplayItemText(itemID, Wrapper);
    }

    
}
