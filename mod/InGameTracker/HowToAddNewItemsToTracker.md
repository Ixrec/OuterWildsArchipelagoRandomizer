# Registering the Item
To make the item appear in the inventory, you need to go to `TrackerManager.cs` and add a new entry to the `ItemEntries` Dictionary. You'll need to set the key as the item ID, for which you can either use a direct call to the Item enum and convert `ToString()` (recommended), or just use a string directly (useful if you want to register an entry that isn't an obtainable item). You'll then want to set the first two or three arguments in the value, which is an `InventoryItemEntry`. The first argument is the item ID, a copy of the key. The second argument is the display name of the item as it'll appear in the inventory. The third value is a bool that determines if the item is a randomized Archipelago item, which should only be set for dummy items.

```cs
public readonly Dictionary<string, string> ItemEntries = new()
{
    {Item.Coordinates.ToString(), new InventoryItemEntry(Item.Coordinates.ToString(), "Eye of the Universe Coordinates") },
    {Item.LaunchCodes.ToString(), new InventoryItemEntry(Item.LaunchCodes.ToString(), "Launch Codes") },
    {Item.NewItem.ToString(), new InventoryItemEntry(Item.NewItem.ToString(), "A New Item") },
    {"NewDummyItem", new InventoryItemEntry("NewDummyItem", "A Fake Item", false) }...
}
```

The order *does* matter, as it reflects the order that the items will appear in the inventory: if you place your item at the beginning of the dictionary, it'll appear at the beginning of the inventory, and if you place it at the fourth slot of the dictionary, it'll appear in the fourth slot of the inventory.

# Making a Description
To add a description for the item, open the `TrackerDescription.cs` class and edit the `DisplayItemText` method. There is a lengthy switch function at the beginning of the method. Add your own `case("NewItem")`, with a usage of `infos.Add("Line of description");` in the case call. For each entry you want to add, you'll want to use a separate `infos.Add()` call. If you want an entry to only appear under specific conditions, you can contain your `infos.Add()` call in any kind of conditional statement.

```cs
if (!discoveredItem || inventory[result] > 0)
{
    switch (itemID)
    {
        case "Coordinates":
            infos.Add("These are the coordinates of the Eye of the Universe.");
            infos.Add("They will show in the bottom left corner when you're ready to input them.");
            break;
        case "NewItem":
            infos.Add("This is a new item entry that will show in the inventory.");
            if (myInt > 0)
            {
                infos.Add("And here's another entry, that will show on a new line, but only if myInt is greater than zero.");
            }
    }
}
```

That's all you need for most descriptions, but you may want to add descriptions for one item under another entry, like with the signals. You can add entries under any condition you can check against in the class if you wish.

# Making a Thumbnail
To choose an image that'll display when the item is selected, find something in Outer Wilds that represents the item well ([Free Cam](https://outerwildsmods.com/mods/freecam/) by _nebula and xen is recommended), then take a screenshot and paste it into your favorite image editor. Crop the image into a square, then make it grayscale (in Paint.NET, you can do this from Adjustments>Black and White). For best results, resize the image to 512x512, then save it at InGameTracker\Icons, with the name ItemName.png (ItemName being the same name as the key you used when registering the item).