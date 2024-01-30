# Registering the Item
To make the item appear in the inventory, you need to go to `TrackerManager.cs` and add a new entry to the `ItemEntries` Dictionary. You can either use a direct call to the Item enum aand convert `ToString()` (recommended), or just use a string directly (useful if you want to register an entry that isn't an obtainable item). The key in the entry should match the item's internal name as it is written in the Item enum, and the value should be the name you want to display in the Inventory.

```cs
public readonly Dictionary<string, string> ItemEntries = new()
{
    {Item.Coordinates.ToString(), "Eye of the Universe Coordinates" },
    {Item.LaunchCodes.ToString(), "Launch Codes"},
    {Item.NewItem.ToString(), "A New Item"},
    {"NewDummyItem", "A Fake Item"}...
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