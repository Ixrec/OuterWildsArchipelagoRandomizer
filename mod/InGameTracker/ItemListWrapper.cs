using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ArchipelagoRandomizer.InGameTracker;

public class ItemListWrapper
{
    private readonly ICustomShipLogModesAPI _api;
    private readonly MonoBehaviour _itemList;

    public ItemListWrapper(ICustomShipLogModesAPI api, MonoBehaviour itemList)
    {
        _api = api;
        _itemList = itemList;
    }

    /// <summary>
    /// Displays the item list UI the Ship Log computer screen. The elements are displayed using animations. You could use this method for example in the EnterMode of your custom mode.
    /// </summary>
    public void Open()
    {
        _api.ItemListOpen(_itemList);
    }

    /// <summary>
    /// Hides the item list UI, also with animations. You could use this method for example in the ExitMode of your custom mode.
    /// </summary>
    public void Close()
    {
        _api.ItemListClose(_itemList);
    }

    /// <summary>
    /// If there are at least two items, takes the user input to navigate the list, changing the selected item in that case. 
    /// The returned int value indicates how much the index of the selected item changed (-1 means that the selection was changed to the item above, 0 that the selection wasn't changed, and 1 that the selection was changed to the item below). 
    /// You could use that value for example to know if you should display things in the description field or change the photo image.
    /// </summary>
    /// <returns></returns>
    public int UpdateList()
    {
        return _api.ItemListUpdateList(_itemList);
    }

    /// <summary>
    /// Similar to UpdateList but this just updates the UI, it doesn't take the user input to navigate the list (and doesn't return a value).
    /// </summary>
    public void UpdateListUI()
    {
        _api.ItemListUpdateListUI(_itemList);
    }

    /// <summary>
    /// Changes the text showed above the list of items, by default it displays the empty string "". You could change it to display the name of your mode for example.
    /// </summary>
    /// <param name="nameValue"></param>
    public void SetName(string nameValue)
    {
        _api.ItemListSetName(_itemList, nameValue);
    }

    /// <summary>
    /// Sets the items of the list, each of them is represented by a Tuple with 4 elements that are used to display the item in the list.
    /// String is name of the list.
    /// First bool indicates if the green down arrow should be shown
    /// Second bool indicates whether the green exclamation point should be shown
    /// Third bool indicates whether the the orange asterisk should be shown
    /// </summary>
    /// <param name="items"></param>
    public void SetItems(List<Tuple<string, bool, bool, bool>> items)
    {
        _api.ItemListSetItems(_itemList, items);
    }

    /// <summary>
    /// Returns the zero-based index of the selected index (0 is the first, 1 is the second and so on). 
    /// Note that, for example, if this returns 6 it doesn't mean that this is the seventh element currently displayed counting from above, because of the scrolling. 
    /// In fact, all items with index >= 4 will be at the fifth displayed item when selected.
    /// </summary>
    /// <returns></returns>
    public int GetSelectedIndex()
    {
        return _api.ItemListGetSelectedIndex(_itemList);
    }

    /// <summary>
    /// Changes the index of the selected index. For example, when the user enters to your mode, you may want the item list to be positioned at a particular index.
    /// </summary>
    /// <param name="index"></param>
    public void SetSelectedIndex(int index)
    {
        _api.ItemListSetSelectedIndex(_itemList, index);
    }

    /// <summary>
    /// Returns the Image component of the object used to display images available if the item list was configured with usePhoto = true. The object is disabled by default, you may enable it and set its sprite to any image you want.
    /// </summary>
    /// <returns></returns>
    public Image GetPhoto()
    {
        return _api.ItemListGetPhoto(_itemList);
    }

    /// <summary>
    /// Returns the Text component of the object used to display a text (by default an orange question mark that in vanilla is used for rumored entries) at the same space where the photo is, 
    /// available if the item list was configured with usePhoto = true. 
    /// The object is disabled by default, you may enable if and set its sprite to any text you want (it doesn't have to necessarily be a question mark, it could be anything).
    /// </summary>
    /// <returns></returns>
    public Text GetQuestionMark()
    {
        return _api.ItemListGetQuestionMark(_itemList);
    }

    /// <summary>
    /// Clears the shared description field (ShipLogEntryDescriptionField) used by other item lists and the vanilla Rumor Mode and Map Mode. 
    /// It's similar to the vanilla SetText method of ShipLogEntryDescriptionField but instead of clearing all but one fact items, it clears them all.
    /// </summary>
    public void DescriptionFieldClear()
    {
        _api.ItemListDescriptionFieldClear(_itemList);
    }

    /// <summary>
    /// Displays the next ShipLogFactListItem with an empty string (intended to be changed by you). 
    /// It also returns that newly displayed ShipLogFactListItem, that you would use to display any text you want (you could use the DisplayText method of the item for example).
    /// </summary>
    /// <returns></returns>
    public ShipLogFactListItem DescriptionFieldGetNextItem()
    {
        return _api.ItemListDescriptionFieldGetNextItem(_itemList);
    }

    /// <summary>
    /// Makes the "Mark on HUD" rectangle object active or inactive depending on the parameter (this object starts disabled by default). 
    /// This root includes the border, background and the screen prompts that are displayed in the lower part of the photo or question mark square (and so it could cover part of the photo), 
    /// all these elements are made visible or invsible using this method.
    /// </summary>
    /// <param name="enable"></param>
    public void MarkHUDRootEnable(bool enable)
    {
        _api.ItemListMarkHUDRootEnable(_itemList, enable);
    }

    /// <summary>
    /// Returns the ScreenPromptList of the "Mark on HUD" rectangle, initially empty (no prompts), so you could add your prompts.
    /// </summary>
    /// <returns></returns>
    public ScreenPromptList MarkHUDGetPromptList()
    {
        return _api.ItemListMarkHUDGetPromptList(_itemList);
    }

    /// <summary>
    /// Returns the list of all ShipLogEntryListItem used to display the items in order from top to bottom. 
    /// All UI items are returned, including the ones that aren't currently used to display elements, and in fact 14 items are always returned even if the description field is used 
    /// (that only allows 7 items to be displayed at most), because the others items are never destroyed when creating the list (this detail isn't probably relevant to you but just in case).
    /// </summary>
    /// <returns></returns>
    public List<ShipLogEntryListItem> GetItemsUI()
    {
        return _api.ItemListGetItemsUI(_itemList);
    }

    /// <summary>
    /// Returns the index of the UI item used to display the item with the given index, or -1 if the item with index isn't currently displayed (because of scrolling). This could be combined with GetItemsUI. 
    /// For example, itemListWrapper.GetItemsUI()[itemListWrapper.GetIndexUI(itemListWrapper.GetSelectedIndex())] returns the ShipLogEntryListItem of the currently selected item.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public int GetIndexUI(int index)
    {
        return _api.ItemListGetIndexUI(_itemList, index);
    }
}