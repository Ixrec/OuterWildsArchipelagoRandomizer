# Outer Wilds Archipelago Randomizer

An [Outer Wilds](https://www.mobiusdigitalgames.com/outer-wilds.html) mod for [the Archipelago multi-game randomizer system](https://archipelago.gg/).

## Status

Alpha (as of January 2024). Recently became playable, though there is no tracker, no DLC integration, and many other desirable features have yet to be started. See [Roadmap](#roadmap).

## Contact

For questions, feedback, or discussion related to the randomizer,
please visit the "Outer Wilds" thread in [the Archipelago Discord server](https://discord.gg/8Z65BR2),
or message me (`ixrec`) directly on Discord.

## Installation

### Prerequisites

- Make sure you have the Steam version of Outer Wilds installed
- Install the [Outer Wilds Mod Manager](https://outerwildsmods.com/mod-manager/)
- Install the [core Archipelago tools](https://github.com/ArchipelagoMW/Archipelago/releases) (at least version 0.4.4)

### Setting Up and Running the Randomizer

- Go to [the Releases page](https://github.com/Ixrec/OuterWildsArchipelagoRandomizer/releases) of this repository and look at the latest release. There should be three files to download: A .zip, an .apworld and a .yaml.

- In the Mod Manager, click the 3 dots icon, and select "Show OWML Folder". It should open something like `%AppData%\OuterWildsModManager\OWML`.
- Open the `Mods/` subfolder.
- In here, unzip the `Ixrec.ArchipelagoRandomizer.zip` file from the Releases page, and overwrite any previous version you might have. The Mod Manager should immediately detect and display it.
- If this is your first time installing this mod, the Mod Manager will probably complain about missing dependencies. To install them, click on the "fix issues" button (the hammer and wrench icon) in the Mod Manager's "Actions" column.

- (**Optional: Other Mods**) Some other mods that I personally like to play with, and that this randomizer is compatible with, include: "Clock" (exactly what it sounds like), "Cheat and Debug Menu" (for its fast-forward button), and "Suit Log" (access the ship log from your suit).

- Go to your Archipelago installation folder. Typically that will be `C:\ProgramData\Archipelago`.
- Put the `outer_wilds.apworld` file in `Archipelago\lib\worlds\`.
- Put the `Outer.Wilds.yaml` file in `Archipelago\Players`.

- (**Recommended: Universal Tracker**) Since there's no dedicated tracker yet, I highly recommend setting up Faris' Universal Tracker. See the pinned messages in its Discord thread for details: https://discord.com/channels/731205301247803413/1170094879142051912

<details>
<summary>I've never used Archipelago before. How do I generate a multiworld?</summary>

Let's create a randomized "multiworld" with only a single Outer Wilds world in it.

- Once again, go to your Archipelago installation folder. Typically that will be `C:\ProgramData\Archipelago`.
- Make sure `Outer.Wilds.yaml` is the only file in `Archipelago\Players` (subfolders here are fine).
- Double-click on `ArchipelagoGenerate.exe`. You should see a console window appear and then disappear after a few seconds.
- In `Archipelago\output\` there should now be a file with a name like `AP_95887452552422108902.zip`.
- Open https://archipelago.gg/uploads in your favorite web browser, and upload the output .zip you just generated. Click "Create New Room".
- The room page should give you a hostname and port number to connect to, e.g. "archipelago.gg:12345".
</details>

- In the Outer Wilds Mod Manager, click the big green Run Game button.
- In Outer Wilds itself, click "New Randomized Expedition", and you will be asked for connection info such as the hostname and port number. Unless you edited `Outer.Wilds.yaml`, your slot/player name will be "Hearthian1". And by default, archipelago.gg rooms have no password.

## What This Mod Changes

Randomizers in the Archipelago sense—which are sometimes called "Metroidvania-style" or "progression-based" randomizers—rely on the base game having several progression-blocking items you must find in order to complete the game.
In Outer Wilds progression is usually blocked by player knowledge rather than items, so to make a good randomizer we have to:

- Take away some of your starting equipment: the Translator, the Signalscope, the Scout, and the Ghost Matter Wavelength upgrade for your camera all become items that must be found.
- Turn some of that player knowledge into items: Nomai Warp Codes replace "teleporter knowledge" (the teleporters won't work without the codes), Silent Running Mode replaces "anglerfish knowledge" (they have much better hearing now), and similarly for Tornado Aerodynamic Adjustments, Electrical Insulation, Imaging Rule, Entanglement Rule, Shrine Door Codes, Warp Core Installation Manual and finally Coordinates.
- On top of the items the vanilla game does have: the Launch Codes, Signalscope frequencies, and Signalscope signals.

In randomizer terms: "items" are placed at randomly selected "locations" (while ensuring the game can still be completed). Most of the locations in this randomizer are:

- scanning the sources of each Signalscope frequency and signal
- revealing Ship Log facts, usually by:
	- translating important Nomai text
	- reaching an important place such as the Ash Twin Project

## Roadmap

This mod has only just released, so I expect to be busy with playtesting and gathering feedback for a while.
After that settles, I will most likely work on one or more of the following sub-projects, depending on what players consider most lacking:

- Flavor Text and Hints:
	- Edit various NPC conversations to account for you not starting off with many of your vanilla starting tools
	- Edit various Nomai text to account for the Nomai codes progression items
	- Edit the other astronauts' dialogue to offer hints about valuable item locations on their respective planets

- More `useful`, `filler` and `trap` items. Ideas include:
	- Oxygen, jetpack fuel, jetpack boost, health, ship durability, etc refills (`filler`) and max upgrades (`useful`)
	- Ship features like autopilot and the landing camera
	- `trap`s for ship damage, fuel leaks, brief forced meditations or ship shutdowns, anglerfish spawns, playing End Times, increased scout launcher recoil

- A dedicated tracker. Not yet decided whether this will be an in-game ship log modification, or an external [poptracker](https://github.com/black-sliver/PopTracker) pack, or both.

- More kinds of randomization that don't affect logic:
	- random planet orbits
	- random Eye coordinates
	- random Dark Bramble layout
	- random ghost matter patches

- Consider various other suggestions (much of this we might decide against, or move to "long-term goals")
	- More base game progression items: the flashlight? The ability to move Nomai orbs? Gravity crystals?
	- Reducing or removing your starting oxygen, fuel, boost, etc? (would make some of these upgrades progression)
	- "logsanity" (all ship log entries), "rumorsanity" (all the ship log rumors too), "textsanity" (every note, casette tape, Nomai text line, dialogue line, etc) settings?
	- "Log Hunt", where the goal is getting N ship logs? Similarly: a Relic Hunt like Outer Relics, or literally by interfacing with Outer Relics?
	- A generic API for other OW mods to declare their randomizable stuff???

### Long-Term Goals

- More advanced kinds of randomization that heavily impact logic:
	- random player spawn, with spacesuit on and time loop started
	- random ship spawn, requiring shipless exploration of one or more warp-connected planets until you find both it and your Launch Codes
	- random warp pad destinations
	- random cloaked planets, making them unreachable without the correct warp pad

- Incorporate the Echoes of the Eye DLC, of course

## Running From Source

### Prerequisites

In addition to the prerequisites from [Installation](#installation):

- Make sure you have a `git` or Github client
- Install [Visual Studio Community 2022](https://visualstudio.microsoft.com/vs/community/)

### Building and Running the OW Mod

- In the Mod Manager, click the 3 dots icon, and select "Show OWML Folder". It should open something like `%AppData%\OuterWildsModManager\OWML`.
- Open the `Mods/` subfolder.
- In here, create a subfolder for the built mod to live. The name can be anything, but `Ixrec.ArchipelagoRandomizer` fits OWML's usual format.
- Now `git clone` this repository
- Inside your local clone, open `mod/ArchipelagoRandomizer.sln` with Visual Studio. Simply double-clicking it should work.
- Open `mod/ArchipelagoRandomizer.csproj.user` in any text editor (including Visual Studio itself), and make sure its `OutputPath` matches the OWML folder you created earlier.
- Tell Visual Studio to build the solution. Click "Build" then "Build Solution", or press Ctrl+Shift+B.
- Several files should appear in the OWML folder, including an `ArchipelagoRandomizer.dll`
- In the Outer Wilds Mod Manager, make sure your locally built mod shows up, and is checked. Then simply click the big green "Run Game" button.

### The .apworld and .yaml files

- `git clone` my Archipelago fork at https://github.com/Ixrec/Archipelago
- Copy the `worlds/outer_wilds` folder from your local clone over to the `lib/worlds/` folder inside your Archipelago installation folder
  - Optionally: If you need to send this to someone else, such as the host of your player group, you may zip the folder and rename the extension from `.zip` to `.apworld`. That's all an "apworld file" is, after all.
- Run ArchipelagoLauncher.exe in your Archipelago installation folder and select "Generate Template Settings" to create a sample Outer Wilds.yaml file

## Credits

- Axxroy, Groot, Hopop, qwint, Rever, Scipio, Snow, and others in the "Archipelago" Discord server for feedback, discussion and encouragement
- GameWyrm, JohnCorby, Trifid, viovayo, and others from the "Outer Wilds Modding" Discord server for help learning how to mod Unity games in general and Outer Wilds in particular
- Nicopopxd for creating the Outer Wilds "Manual" for Archipelago
- Flitter for talking me into trying out Archipelago randomizers in the first place
- All the Archipelago contributors who made that great multi-randomizer system
- Everyone at Mobius who made this great game

No relation to [the OW story mod called "Archipelago"](https://outerwildsmods.com/mods/archipelago/)
