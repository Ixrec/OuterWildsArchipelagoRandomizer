# Adding Items to the Inventory Menu

The Ship Log tracker includes an inventory mode, allowing the player to see what items they have collected, which items are new, and where any hinted items might be. Some setup is required to make an item show up.

## Registering the Item

To make the item appear in the inventory, you need to go to `TrackerManager.cs` and add a new `InventoryItemEntry` to the `_ItemEntries` `List`.
The entry's ID should be either an `Item` enum value, or (for entries that don't correspond to an AP item) a string.
The second argument is the display name of the item as it'll appear in the inventory.

```cs
private static List<InventoryItemEntry> _ItemEntries = new()
{
    new InventoryItemEntry(Item.Coordinates, "Eye of the Universe Coordinates"),
    new InventoryItemEntry(Item.LaunchCodes, "Launch Codes"),
    new InventoryItemEntry(Item.NewItem, "A New Item") },
    new InventoryItemEntry("NewNonItemEntry", "A Non-Item") }...
}
```

The order *does* matter, as it reflects the order that the items will appear in the inventory:
if you place your item at the beginning of the dictionary, it'll appear at the beginning of the inventory,
and if you place it at the fourth slot of the dictionary, it'll appear in the fourth slot of the inventory.

## Making a Description

To add a description for the item, open the `TrackerDescription.cs` class and edit the `DisplayItemText` method.
There is a lengthy switch function at the beginning of the method.
Add your own `case Item.NewItem`, with a usage of `infos.Add("Line of description");` in the case call.
For each entry you want to add, you'll want to use a separate `infos.Add()` call.
If you want an entry to only appear under specific conditions,
you can contain your `infos.Add()` call in any kind of conditional statement.

```cs
if (!discoveredItem || inventory[result] > 0)
{
    switch (itemID)
    {
        case Item.Coordinates:
            infos.Add("These are the coordinates of the Eye of the Universe.");
            infos.Add("They will show in the bottom left corner when you're ready to input them.");
            break;
        case Item.NewItem:
            infos.Add("This is a new item entry that will show in the inventory.");
            if (myInt > 0)
            {
                infos.Add("And here's another entry, that will show on a new line, but only if myInt is greater than zero.");
            }
    }
}
```

That's all you need for most descriptions, but you may want to add descriptions for one item under another entry, like with the signals.
You can add entries under any condition you can check against in the class if you wish.

## Making a Thumbnail

To choose an image that'll display when the item is selected, find something in Outer Wilds that represents the item well
([Free Cam](https://outerwildsmods.com/mods/freecam/) by _nebula and xen is recommended),
then take a screenshot and paste it into your favorite image editor.
Crop the image into a square, then make it grayscale (in Paint.NET, you can do this from Adjustments>Black and White).
For best results, resize the image to 512x512, then save it at InGameTracker\Icons,
with the name ItemName.png (ItemName being the same name as the key you used when registering the item).

# Adding a Location to the Location Tracker

The Ship Log Tracker includes a Location Tracker mode, displaying a list of major regions and all the locations within them.

## Setting up a Location

The way the Tracker determines which major region a location is in is by looking at the name value for the location in locations.jsonc and looking at the prefix (first two characters) of the name:

* Hourglass Twins: **"ET"** (Ember Twin), **"AT"** (Ash Twin), **"CT"** (Cave Twin), or **"TT"** (Tower Twin)
* Timber Hearth: **"TH"** (Timber Hearth), **"AR"** (Attlerock), or **"TM"** (Timber Moon)
* Brittle Hollow: **"BH"** (Brittle Hollow), **"HL"** (Hollow's Lantern), or **"VM"** (Volcanic Moon)
* Giant's Deep: **"GD"** (Giant's Deep), **"OP"**, or **"OR"** (both for Orbital Probe Cannon)
* Dark Bramble: **"DB"** (Dark Bramble)

Anything else gets thrown into the generic "Outer Wilds" region.
Thus, when naming a location, it should include the relevant prefix.

## Making the Location Show Up in the Tracker

To add a location to the Tracker, open the .jsonc file within the LocationInfos file that corresponds to the correct prefix, and if the location is a Ship Log location, you'll need to open the version of the file that ends in `_SL`.
Create a new entry with the following format:

```json
    {
        "locationModID": "NAME_AS_IN_LOCATION.CS",
        "description": "This is the text description of the location as it will appear within the Tracker description field",
        "thumbnail": "SHIP_LOG_FACT_WITH_RELEVANT_THUMBNAIL"
    }
```

* `locationModID` is the ID of the location as it appears within location.cs (*not* the .jsonc file).
* `description` is a text description telling the player where they need to go to check the location.
It should be specific enough to tell the player where they need to look to get the location check, for example telling them to talk to an NPC or check a scroll wall.
* `thumbnail` is a a Ship Log fact thumbnail that shows the relevant area that the location is in. 
To find the list of ship log facts, they can be found in location.cs: they start with `SLF__` and end with `_X#`.
Remove the prefix and suffix to get the Ship Log Fact ID, so `SLF__TH_VILLAGE_X1` becomes `TH_VILLAGE`.
(Note: I have found one exception, `SLF__WHS_X#` is actually for the `WHITE_HOLE_STATION` fact)

Once you have created the entry, it should automatically show up in the Tracker (if not, check for errors in the OWML log.)