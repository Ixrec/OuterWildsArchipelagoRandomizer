# Outer Wilds Archipelago Randomizer

![Logo](ModLogo.png)

An [Outer Wilds](https://www.mobiusdigitalgames.com/outer-wilds.html) mod for [the Archipelago multi-game randomizer system](https://archipelago.gg/).

## Status

Feature Complete / Maintenance Mode (since October 2024).

All features I wanted to implement have been shipped, including a full in-game tracker, alternate spawns, DLC integration, and multiple story mod integrations. It's been extensively playtested, and all known bugs with the randomizer itself have been fixed (the remaining problems are all believed to be base game bugs, especially the infamous "end-of-loop crash").

I will now be moving on to other projects as my primary focus, but I still intend to be responsive to any bug reports, PRs opened here, or inquiries about contributing to this randomizer.

## Contact

For questions, feedback, or discussion related to the randomizer, please visit the "Outer Wilds" thread in [the Archipelago Discord server](https://discord.gg/8Z65BR2), or message me (`ixrec`) directly on Discord.

## What is an "Archipelago Randomizer", and why would I want one?

Let's say I'm playing Outer Wilds, and my friend is playing Ocarina of Time. When I translate some Nomai text in the High Energy Lab, I find my friend's Hookshot, allowing them to reach several OoT chests they couldn't before. In one of those chests they find my Signalscope, allowing me to scan all the signals in the OW solar system. I scan Chert's Drum signal, and find my friend's Ocarina. This continues until we both find enough of our items to finish our games.

In essence, a multi-game randomizer system like Archipelago allows a group of friends to each bring whatever games they want (if they have an Archipelago mod) and smush them all together into one big cooperative multiplayer experience.

Some people have even made Youtube videos of their randomizer playthroughs. Of the ones I've seen, Quothe's and Skurry's are the best at demonstrating what a typical Outer Wilds randomizer playthrough is like (without the "multi-game" aspect of Archipelago):

[<img src="https://img.youtube.com/vi/6SZ-xhBVjq0/hqdefault.jpg" width="300"/>](https://www.youtube.com/watch?v=6SZ-xhBVjq0) [<img src="https://img.youtube.com/vi/ppsY9X_Mox8/hqdefault.jpg" width="300"/>](https://www.youtube.com/watch?v=ppsY9X_Mox8)

### What This Mod Changes

Randomizers in the Archipelago sense—which are sometimes called "Metroidvania-style" or "progression-based" randomizers—rely on the base game having several progression-blocking items you must find in order to complete the game.
In Outer Wilds progression is usually blocked by player knowledge rather than items, so to make a good randomizer we take away some of your starting equipment, and turn much of that player knowledge into items. Here are several examples:

- Translator
- Scout
- Signalscope frequencies
- Nomai Warp Codes (replaces "the player knowing how warp pads work" with a button prompt)
- Silent Running Mode (the fish have better hearing without this)
- Tornado Aerodynamic Adjustments
- Coordinates (since coordinates are randomized by default, you need this to finish)
- Autopilot
- Oxygen/Fuel Capacity Upgrades
- Oxygen/Fuel Refills
- Marshmallows
- and more

In randomizer terms: "items" are placed at randomly selected "locations" (while ensuring the game can still be completed). Most of the locations in this randomizer are:

- Notes, tape recorders and fuel tanks left by other Hearthians
- Translating pieces of Nomai text (e.g. the High Energy Lab experiment)
- Reaching important in-game places (e.g. the core of Giant's Deep)
- Scanning each Signalscope signal source
- Revealing facts in the Ship Log (which is usually also one of the above)

Complete lists and descriptions of all the items and locations in the randomizer can be found in-game in the ship log. Or in randomizer terms: we added an "in-game tracker" to the ship log. We strongly recommend using this tracker on your first randomizer playthrough to learn what you're supposed to be doing.

## Installation

### Prerequisites

- Make sure you have Outer Wilds installed
- Install the [Outer Wilds Mod Manager](https://outerwildsmods.com/mod-manager/)
- Install the core Archipelago tools (at least version 0.5.0, but preferably the latest version) from [Archipelago's Github Releases page](https://github.com/ArchipelagoMW/Archipelago/releases). On that page, scroll down to the "Assets" section for the release you want, click on the appropriate installer for your system to start downloading it (for most Windows users, that will be the file called `Setup.Archipelago.X.Y.Z.exe`), then run it.
- Go to [the Releases page](https://github.com/Ixrec/OuterWildsArchipelagoRandomizer/releases) of this repository and look at the latest release. There should be three files: A .zip, an .apworld and a .yaml. Download the .apworld and the .yaml.

### Archipelago tools setup

- Go to your Archipelago installation folder. Typically that will be `C:\ProgramData\Archipelago`.
- Put the `Outer.Wilds.yaml` file in `Archipelago\Players`. You may leave the `.yaml` unchanged to play on default settings, or use your favorite text editor to read and change the settings in it.
- Double click on the `outer_wilds.apworld` file. Archipelago should display a popup saying it installed the apworld. Optionally, you can double-check that there's now an `outer_wilds.apworld` file in `Archipelago\custom_worlds\`.

#### I've never used Archipelago before. How do I generate a multiworld?

Let's create a randomized "multiworld" with only a single Outer Wilds world in it.

- Make sure `Outer.Wilds.yaml` is the only file in `Archipelago\Players` (subfolders here are fine).
- Double-click on `Archipelago\ArchipelagoGenerate.exe`. You should see a console window appear and then disappear after a few seconds.
- In `Archipelago\output\` there should now be a file with a name like `AP_95887452552422108902.zip`.
- Open https://archipelago.gg/uploads in your favorite web browser, and upload the output .zip you just generated. Click "Create New Room".
- The room page should give you a hostname and port number to connect to, e.g. "archipelago.gg:12345".

For a more complex multiworld, you'd put one `.yaml` file in the `\Players` folder for each world you want to generate. You can have multiple worlds of the same game (each with different options), as well as several different games, as long as each `.yaml` file has a unique player/slot name. It also doesn't matter who plays which game; it's common for one human player to play more than one game in a multiworld.

### Modding and Running Outer Wilds

- In the Outer Wilds Mod Manager, click on "Get Mods", search for "Archipelago Randomizer", and once you see this mod listed, click the install button to the right of it (if you were wondering about the .zip file we didn't download earlier, that's what the Mod Manager is installing).
- (**Optional: Other Mods**) Some other mods that I personally like to play with, and that this randomizer is compatible with, include: "Clock" (exactly what it sounds like), "Cheat and Debug Menu" (for its fast-forward button), and "Suit Log" (access the ship log from your suit).
- Now click the big green Run Game button. Note that you must launch Outer Wilds through the Mod Manager in order for the mods to be applied; launching from Steam won't work.
- Once you're at the main menu of Outer Wilds itself, click "New Random Expedition", and you will be asked for connection info such as the hostname and port number. Unless you edited `Outer.Wilds.yaml` (or used multiple `.yaml`s), your slot/player name will be "Hearthian1". And by default, archipelago.gg rooms have no password.

#### What if I want to run a pre-release version for testing, or downgrade to an older version of this mod (so I can finish a longer async)?

<details>
<summary>Click here to show instructions</summary>

To use a pre-release version:

- In the Mod Manager, go to the "Get Mods" section (**not** "Installed Mods")
- Search for "Archipelago Randomizer", click the 3 dots icon next to this mod, and select the "Use Prerelease ..." option

To downgrade to an older version, you'll need to install a `Ixrec.ArchipelagoRandomizer.zip` manually. This repo's Releases page has all the mod `.zip`s for past releases (and the current release `.zip`, which is what the Mod Manager normally downloads for you).

- In the Mod Manager, click the 3 dots icon at the top of the window, and select the "Install From" option
- In this popup, make sure the "Install From" mode on top is set to "URL"
- Go to [this repo's Releases page](https://github.com/Ixrec/OuterWildsArchipelagoRandomizer/releases) and copy the link address to one of the `Ixrec.ArchipelagoRandomizer.zip` files from a previous release. For example, the 0.1.1 .zip link would be "https://github.com/Ixrec/OuterWildsArchipelagoRandomizer/releases/download/v0.1.1/Ixrec.ArchipelagoRandomizer.zip".
- Back in the Mod Manager popup, paste this link into the "URL" entry below, and click Install.

Either way, the Mod Manager should immediately display the version number of the mod version you installed. Be careful not to click the Fix Issues button until you want to go back to the latest stable mod version.
</details>

## Mod Compatibility

Outer Wilds story mods whose content has been fully integrated into this randomizer:

- [Astral Codec](https://outerwildsmods.com/mods/astralcodec/)
- [The Outsider](https://outerwildsmods.com/mods/theoutsider/)
- [Hearth's Neighbor](https://outerwildsmods.com/mods/hearthsneighbor/)
- [Hearth's Neighbor 2: Magistarium](https://outerwildsmods.com/mods/hearthsneighbor2magistarium/)
- [Fret's Quest](https://outerwildsmods.com/mods/fretsquest/)

Outer Wilds quality of life/tooling/etc mods that are known to work without issue:

- Clock
- Cheat and Debug Menu
- Suit Log
- Unity Explorer
- Light Bramble (thanks Rever for testing this), although it makes the "Silent Running Mode" item pointless
- Time Saver (thanks Jade for testing this)

Outer Wilds mods that have been tried, but are known to have issues (this information might not be kept up to date, as I don't/can't test these myself):

- NomaiVR (thanks Snout for testing this): Mostly works. Trying to grab the Translator or Signalscope *before donning the suit* will softlock, but this is fine once you're in the suit. The in-game console does not work reliably, so using the AP Text Client instead is recommended.
- Quantum Space Buddies: Awkward but can *probably* be made to work. I believe you would have to use one of the "... Random Expedition" main menu buttons to connect to your AP server, immediately quit back to the main menu, then use either of QSB's main menu buttons to load the game with multiplayer. Please tell us if you can test this properly.

## Contributing Features, Bugfixes, More Story Mods, etc

See [CONTRIBUTING.md](CONTRIBUTING.md).

## Credits

- GameWyrm for contributing this mod's in-game console, in-game tracker, and the banner art
- Smaller direct code/content contributors, including: thestrangepie (suitless logic), hopop201 (location names/descriptions), dgarroDC (bug fix)
- Amada, Axxroy, DCBomB, Groot, Hopop, Onemario, qwint, Rever, Scipio, Snow, and others in the "Archipelago" Discord server for feedback, discussion and encouragement
- clubby789, dgarroDC, GameWyrm, glitchewski, JohnCorby, nebula, Trifid, viovayo, xen and others from the "Outer Wilds Modding" Discord server for help learning how to mod Unity games in general and Outer Wilds in particular, and creating the other OW mods that this randomizer relies on or is often played with
- Nicopopxd for creating the Outer Wilds "Manual" for Archipelago
- Flitter for talking me into trying out Archipelago randomizers in the first place
- All the Archipelago contributors who made that great multi-randomizer system
- Everyone at Mobius who made this great game

No relation to [the OW story mod called "Archipelago"](https://outerwildsmods.com/mods/archipelago/)
