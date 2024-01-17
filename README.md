# Outer Wilds Archipelago Randomizer

An [Outer Wilds](https://www.mobiusdigitalgames.com/outer-wilds.html) mod for [the Archipelago multi-game randomizer system](https://archipelago.gg/).

## Status

Public Alpha (as of January 2024).

Recently became playable, though there is no tracker, no DLC integration, and many other desirable features have yet to be started. See [Roadmap](#roadmap).

## Contact

For questions, feedback, or discussion related to the randomizer, please visit the "Outer Wilds" thread in [the Archipelago Discord server](https://discord.gg/8Z65BR2), or message me (`ixrec`) directly on Discord.

## What is an "Archipelago Randomizer", and why would I want one?

Let's say I'm playing Outer Wilds, and my friend is playing Ocarina of Time. When I translate some Nomai text in the High Energy Lab, I find my friend's Hookshot, allowing them to reach several OoT chests they couldn't before. In one of those chests they find my Signalscope, allowing me to scan all the signals in the OW solar system. I scan Chert's Drum signal, and find my friend's Ocarina. This continues until we both find enough of our items to finish our games.

In essence, a multi-game randomizer system like Archipelago allows a group of friends to each bring whatever games they want (if they have an Archipelago mod) and smush them all together into one big cooperative multiplayer experience.

### What This Mod Changes

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

## Installation

### Prerequisites

- Make sure you have the Steam version of Outer Wilds installed
- Install the [Outer Wilds Mod Manager](https://outerwildsmods.com/mod-manager/)
- Install the [core Archipelago tools](https://github.com/ArchipelagoMW/Archipelago/releases) (at least version 0.4.4)
- Go to [the Releases page](https://github.com/Ixrec/OuterWildsArchipelagoRandomizer/releases) of this repository and look at the latest release. There should be three files to download: A .zip, an .apworld and a .yaml.

### Archipelago tools setup

- Go to your Archipelago installation folder. Typically that will be `C:\ProgramData\Archipelago`.
- Put the `outer_wilds.apworld` file in `Archipelago\lib\worlds\`.
- Put the `Outer.Wilds.yaml` file in `Archipelago\Players`. You may leave the `.yaml` unchanged to play on default settings, or use your favorite text editor to read and change the settings in it.

- (**Recommended: Universal Tracker**) Since there's no dedicated tracker yet, I highly recommend setting up Faris' Universal Tracker. See the pinned messages in its Discord thread for details: https://discord.com/channels/731205301247803413/1170094879142051912

#### I've never used Archipelago before. How do I generate a multiworld?

Let's create a randomized "multiworld" with only a single Outer Wilds world in it.

- Make sure `Outer.Wilds.yaml` is the only file in `Archipelago\Players` (subfolders here are fine).
- Double-click on `Archipelago\ArchipelagoGenerate.exe`. You should see a console window appear and then disappear after a few seconds.
- In `Archipelago\output\` there should now be a file with a name like `AP_95887452552422108902.zip`.
- Open https://archipelago.gg/uploads in your favorite web browser, and upload the output .zip you just generated. Click "Create New Room".
- The room page should give you a hostname and port number to connect to, e.g. "archipelago.gg:12345".

For a more complex multiworld, you'd put one `.yaml` file in the `\Players` folder for each world you want to generate. You can have multiple worlds of the same game (each with different settings), as well as several different games, as long as each `.yaml` file has a unique player/slot name. It also doesn't matter who plays which game; it's common for one human player to play more than one game in a multiworld.

### Outer Wilds game mod setup

- In the Mod Manager, click the 3 dots icon, and select "Show OWML Folder". It should open something like `%AppData%\OuterWildsModManager\OWML`.
- Open the `Mods/` subfolder.
- In here, unzip the `Ixrec.ArchipelagoRandomizer.zip` file from the Releases page, and overwrite any previous version you might have.
- The Mod Manager should immediately detect and display it this mod. If it doesn't, click the Refresh button in the top left.
- If this is your first time installing this mod, the Mod Manager will probably complain about missing dependencies. To install them, click on the "fix issues" button (the hammer and wrench icon) in the Mod Manager's "Actions" column.

- (**Optional: Other Mods**) Some other mods that I personally like to play with, and that this randomizer is compatible with, include: "Clock" (exactly what it sounds like), "Cheat and Debug Menu" (for its fast-forward button), and "Suit Log" (access the ship log from your suit).

### Running your modded Outer Wilds

- In the Outer Wilds Mod Manager, click the big green Run Game button. You must launch Outer Wilds through the Mod Manager in order for the mods to be applied.
- In Outer Wilds itself, click "New Random Expedition", and you will be asked for connection info such as the hostname and port number. Unless you edited `Outer.Wilds.yaml`, your slot/player name will be "Hearthian1". And by default, archipelago.gg rooms have no password.

## Roadmap

### 0.2.x Goals/Priorities

- Add more "itemless" locations, likely on the notes and tape recorders left by other astronauts.

- Create some `useful`, `filler` and `trap` items, instead of just "Nothing"s. Ideas include:
	- Oxygen, jetpack fuel, jetpack boost, health, ship durability, etc refills (`filler`) and max upgrades (`useful`)
	- Ship features like autopilot and the landing camera
	- `trap`s for ship damage, fuel leaks, brief forced meditations or ship shutdowns, anglerfish spawns, playing End Times, increased scout launcher recoil

- A "logsanity" setting (all ship log entries are AP locations). This mod's code currently relies on having fixed locations and logic, but many of the features we want will involve changing locations and logic based on the randomizer settings. Of those features, logsanity is the only one with no difficult game design decisions, so I'd like to implement it first as a stepping stone to e.g. DLC integration, random spawn, in-game tracker, etc.

### 0.2.x Stretch Goals

I will probably try to *investigate* these during 0.2.0 development, but I dunno if they'll make it in.

- Additional randomizations that wouldn't affect logic:
	- random planet orbits
	- random Eye coordinates (this one is the most likely because I've done so much of the groundwork for it already)
	- random Dark Bramble layout
	- random ghost matter patches

- Change signalscope logic so each frequency item is required to scan signals in that frequency, as many players seem to expect.

### 0.3.x Goals/Priorities

- Echoes of the Eye DLC integration
	- Possibly randomize the flashlight

- Random player & ship spawn, with spacesuit on, time loop started, and Launch Codes placed in a random location like most other items
	- Random warp pad destinations should go well with this

### Other

- A dedicated tracker. Will probably be added to the in-game ship log, and contributed by GameWyrm, hence this does not have a target release yet.

- Flavor Text and Hints:
	- Edit various NPC conversations to account for you not starting off with many of your vanilla starting tools
	- Edit various Nomai text to account for the Nomai codes progression items
	- Edit the other astronauts' dialogue to offer hints about valuable item locations on their respective planets

- Reducing or removing your starting oxygen, fuel, boost, etc? (would make some of these upgrades progression)

- "rumorsanity" (all the ship log rumors too), "textsanity" (every note, casette tape, Nomai text line, dialogue line, etc) settings?

- More base game progression items: Gravity crystals? The ability to move Nomai orbs?

- "Log Hunt", where the goal is getting N ship logs? Similarly: a Relic Hunt like Outer Relics, or literally by interfacing with Outer Relics?

- A generic API for other OW mods to declare their randomizable stuff???

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
- GameWyrm for contributing this mod's in-game console
- Nicopopxd for creating the Outer Wilds "Manual" for Archipelago
- Flitter for talking me into trying out Archipelago randomizers in the first place
- All the Archipelago contributors who made that great multi-randomizer system
- Everyone at Mobius who made this great game

No relation to [the OW story mod called "Archipelago"](https://outerwildsmods.com/mods/archipelago/)
