# Outer Wilds Archipelago Randomizer

An [Outer Wilds](https://www.mobiusdigitalgames.com/outer-wilds.html) mod for [the Archipelago multi-game randomizer system](https://archipelago.gg/).

## Status

In active development (as of December 2023). Nothing playable yet.

## Contact

For questions, feedback, or discussion related to the randomizer,
please visit the "Outer Wilds" thread in [the Archipelago Discord server](https://discord.gg/8Z65BR2),
or message me (`ixrec`) directly on Discord.

## Installation

Not a thing yet. See [Running From Source](#running-from-source) if you're brave or technical.

## Detailed Status and Roadmap

Randomizers in the Archipelago sense, which we might call "Metroidvania-style randomizers",
rely on the base game having several progression-blocking items.
In Outer Wilds, the progression is mostly blocked by player knowledge rather than items,
so to make a good randomizer we need to find some non-items to "item-ify" or some starting items to take away.
Most of this code is about item-ifying parts of the vanilla game.

### Planned MVP "Items"

- ~~the Nomai Translator~~
- ~~the Signalscope~~
	- ~~and its frequencies and signals~~
- ~~the Scout~~
- ~~Ghost Matter Wavelength (to make the camera start showing ghost matter again)~~
- ~~Nomai Warp Codes (a.k.a. "Teleporter Knowledge", except you won't need to wait for planets to align)~~
- ~~Warp Core Installation Codes~~
- ~~Rule of Quantum Imaging~~
- ~~Rule of Quantum Entanglement~~
- ~~Quantum Shrine Door Codes (a.k.a "Rule of the Sixth Location")~~
- ~~Tornado Aerodynamic Adjustments (a.k.a. "Tornado Knowledge")~~
- ~~Silent Running Mode (a.k.a. "Anglerfish Knowledge")~~
- ~~Electrical Insulation (a.k.a. "Jellyfish Knowledge")~~
- ~~Coordinates~~

### Planned MVP Locations

- ~~dozens of ship log facts, variously triggered by~~
	- ~~translating Nomai text walls~~
	- ~~reaching a place like the Ash Twin Project or Giant's Deep's core~~
- ~~scanning the sources of each Signalscope frequency and signal~~

### MVP Blockers

- ~~item-ify a few more starting tools and knowledge~~
- ~~read and write save data~~, including checking for discrepancies
- ~~create an in-game console for displaying Archipelago messages~~
- teach this mod the Archipelago client protocol, probably using an existing AP client library for .NET
- write an apworld with items, locations and all the "logic" rules for which items are needed to reach which locations

### Post-MVP Roadmap

Immediately after MVP, I expect to be busy with playtesting and gathering feedback.
After that settles, I will most likely work on one or more of the following sub-projects, depending on what players consider most lacking:

- Flavor Text and Hints:
	- Edit various NPC conversations to account for you not starting off with many of your vanilla starting tools
	- Edit various Nomai text to account for the Nomai codes progression items
	- Edit the other astronauts' dialogue to offer hints about valuable item locations on their respective planets

- More `useful`, `filler` and `trap` items. Ideas include:
	- Oxygen, jetpack fuel, jetpack boost, health, ship durability, etc refills (`filler`) and max upgrades (`useful`)
	- Ship features like autopilot and the landing camera
	- `trap`s for ship damage, fuel leaks, brief forced meditations or ship shutdowns, anglerfish spawns, playing End Times

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

- Make sure you have a `git` or Github client
- Make sure you have the Steam version of Outer Wilds installed
- Install the [Outer Wilds Mod Manager](https://outerwildsmods.com/mod-manager/)
- Install [Visual Studio Community 2022](https://visualstudio.microsoft.com/vs/community/)

### Building and Running

- In the Mod Manager, click the 3 dots icon, and select "Show OWML Folder". It should open something like `%AppData%\OuterWildsModManager\OWML`.
- Open the `Mods/` subfolder.
- In here, create a subfolder for the built mod to live. The name can be anything, but `Ixrec.ArchipelagoRandomizer` fits OWML's usual format.
- Now `git clone` this repository
- Inside your local clone, open `mod/ArchipelagoRandomizer.sln` with Visual Studio. Simply double-clicking it should work.
- Open `mod/ArchipelagoRandomizer.csproj.user` in any text editor (including Visual Studio itself), and make sure its `OutputPath` matches the OWML folder you created earlier.
- Tell Visual Studio to build the solution. Click "Build" then "Build Solution", or press Ctrl+Shift+B.
- A file called `ArchipelagoRandomizer.dll` should have appeared in the OWML folder
- In the Outer Wilds Mod Manager, make sure your locally built mod shows up, and is checked. Then simply click the big green "Run Game" button.

## Credits

- Scipio, qwint, Snow, Rever, and many others in the "Archipelago" Discord server for feedback, discussion and encouragement
- GameWyrm, JohnCorby, Trifid, viovayo, and others from the "Outer Wilds Modding" Discord server for help learning how to mod Unity games in general and Outer Wilds in particular
- All the Archipelago contributors who made that great multi-randomizer system
- Everyone at Mobius who made this great game
- Flitter for talking me into trying out Archipelago randomizers in the first place

No relation to [the OW story mod called "Archipelago"](https://outerwildsmods.com/mods/archipelago/)
