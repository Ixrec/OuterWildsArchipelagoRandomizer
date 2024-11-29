/**
 * Courtesy of the Ship Log Slide Reel Player Plus mod
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ArchipelagoRandomizer.InGameTracker;

public abstract class ItemListWrapper
{
    protected MonoBehaviour _itemList;

    public abstract void Open();
    public abstract void Close();
    public abstract int UpdateList();
    public abstract void UpdateListUI();
    public abstract void SetName(string nameValue);
    public abstract void SetItems(List<Tuple<string, bool, bool, bool>> items);
    public abstract int GetSelectedIndex();
    public abstract void SetSelectedIndex(int index);
    public abstract Image GetPhoto();
    public abstract Text GetQuestionMark();
    public abstract void DescriptionFieldClear();
    public abstract ShipLogFactListItem DescriptionFieldGetNextItem();
    public abstract List<ShipLogEntryListItem> GetItemsUI();
    public abstract int GetIndexUI(int index);
}

public class ShipLogItemListWrapper : ItemListWrapper
{
    private readonly ICustomShipLogModesAPI _api;

    public ShipLogItemListWrapper(ICustomShipLogModesAPI api, MonoBehaviour itemList)
    {
        _itemList = itemList;
        _api = api;
    }

    public override void Open()
    {
        _api.ItemListOpen(_itemList);
    }

    public override void Close()
    {
        _api.ItemListClose(_itemList);
    }

    public override int UpdateList()
    {
        return _api.ItemListUpdateList(_itemList);
    }

    public override void UpdateListUI()
    {
        _api.ItemListUpdateListUI(_itemList);
    }

    public override void SetName(string nameValue)
    {
        _api.ItemListSetName(_itemList, nameValue);
    }

    public override void SetItems(List<Tuple<string, bool, bool, bool>> items)
    {
        _api.ItemListSetItems(_itemList, items);
    }

    public override int GetSelectedIndex()
    {
        return _api.ItemListGetSelectedIndex(_itemList);
    }

    public override void SetSelectedIndex(int index)
    {
        _api.ItemListSetSelectedIndex(_itemList, index);
    }

    public override Image GetPhoto()
    {
        return _api.ItemListGetPhoto(_itemList);
    }

    public override Text GetQuestionMark()
    {
        return _api.ItemListGetQuestionMark(_itemList);
    }

    public override void DescriptionFieldClear()
    {
        _api.ItemListDescriptionFieldClear(_itemList);
    }

    public override ShipLogFactListItem DescriptionFieldGetNextItem()
    {
        return _api.ItemListDescriptionFieldGetNextItem(_itemList);
    }

    public override List<ShipLogEntryListItem> GetItemsUI()
    {
        return _api.ItemListGetItemsUI(_itemList);
    }

    public override int GetIndexUI(int index)
    {
        return _api.ItemListGetIndexUI(_itemList, index);
    }

    public void MarkHUDRootEnable(bool enable)
    {
        _api.ItemListMarkHUDRootEnable(_itemList, enable);
    }

    public ScreenPromptList MarkHUDGetPromptList()
    {
        return _api.ItemListMarkHUDGetPromptList(_itemList);
    }
}

public class SuitLogItemListWrapper : ItemListWrapper
{
    private readonly ISuitLogAPI _api;

    public SuitLogItemListWrapper(ISuitLogAPI api, MonoBehaviour itemList)
    {
        _itemList = itemList;
        _api = api;
    }

    public override void Open()
    {
        _api.ItemListOpen(_itemList);
    }

    public override void Close()
    {
        _api.ItemListClose(_itemList);
    }

    public override int UpdateList()
    {
        return _api.ItemListUpdateList(_itemList);
    }

    public override void UpdateListUI()
    {
        throw new NotImplementedException();
    }

    public override void SetName(string nameValue)
    {
        _api.ItemListSetName(_itemList, nameValue);
    }

    public override void SetItems(List<Tuple<string, bool, bool, bool>> items)
    {
        _api.ItemListSetItems(_itemList, items);
    }

    public override int GetSelectedIndex()
    {
        return _api.ItemListGetSelectedIndex(_itemList);
    }

    public override void SetSelectedIndex(int index)
    {
        _api.ItemListSetSelectedIndex(_itemList, index);
    }

    public override Image GetPhoto()
    {
        return _api.ItemListGetPhoto(_itemList);
    }

    public override Text GetQuestionMark()
    {
        return _api.ItemListGetQuestionMark(_itemList);
    }

    public override void DescriptionFieldClear()
    {
        _api.ItemListDescriptionFieldClear(_itemList);
    }

    public override ShipLogFactListItem DescriptionFieldGetNextItem()
    {
        return _api.ItemListDescriptionFieldGetNextItem(_itemList);
    }

    public override List<ShipLogEntryListItem> GetItemsUI()
    {
        return _api.ItemListGetItemsUI(_itemList);
    }

    public override int GetIndexUI(int index)
    {
        return _api.ItemListGetIndexUI(_itemList, index);
    }

    public void DescriptionFieldOpen()
    {
        _api.ItemListDescriptionFieldOpen(_itemList);
    }

    public void DescriptionFieldClose()
    {
        _api.ItemListDescriptionFieldClose(_itemList);
    }
}
