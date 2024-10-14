## Contribution Policy

Genearally speaking, contributions are welcome. But since there's always the possibility I'd end up rejecting a change, please talk to me if you're unsure before investing significant effort.

### Bugfixes

If something is obviously incorrect and easy to fix, just go ahead and open a pull request for it. If you're not sure, it never hurts to ask me first whether a behavior is intentional.

Some [open issues](https://github.com/Ixrec/OuterWildsArchipelagoRandomizer/issues) may be for known bugs that I haven't fixed, because they're for opt-in/off by default features I'm not personally passionate about.

### General Features

For features, please talk to me before doing significant implementation work so we can make sure it fits well with the rest of the randomizer, hasn't been considered and rejected in the past, and so we can discuss what's hard-required to ship it and what's okay to leave for later. In particular, if it's a feature you'd like to see enabled by default (opt-out rather than opt-in), the bar will be pretty high.

Most [open issues](https://github.com/Ixrec/OuterWildsArchipelagoRandomizer/issues) represent features that have been suggested in the past, and that I would be happy to see implemented, but am not motivated to implement myself. These are safe bets to pick up.

## Story Mod Integrations

This is the specific kind of contribution I've gone out of my way to make as easy as possible, since more high-quality Outer Wilds story mods keep coming out and most of them should need little to no "custom" code to integrate.

First, if you haven't already, actually play the randomizer at least once. You should have a solid grasp on the concepts of an "Archipelago item", an "Archipelago location" and "logic" before continuing, as well as the randomizer's existing items so you can write logic rules using them. Most of the work to integrate a new story mod is filling in a bunch of metadata for each AP location in that mod.

Second, if you haven't already, make sure you can [run the mod from source](#running-from-source).

The randomizer mod and these docs are designed to make it as easy as possible to integrate new story mods, provided the items and locations you want them to have fit into these well-understood categories:
- AP locations for ship log facts
- AP items for Signalscope frequencies
- AP locations for scanning a Signalscope signal

Less common but still easy to do (because LocationTrigger.cs already has code for these) are:
- AP locations for "conversations" with NPCs, signs, tape recorders, etc
- AP locations for "recovery points" like fuel tanks and your ship's medkit
- AP locations for "DialogueConditionTrigger"s that affect NPC dialogue later
- AP locations for picking up in-game items
- AP locations for translating Nomai text

Anything else would be considered a "custom" item or location. Custom items/locations are certainly possible, but I can't offer as much guidance on them here, and they may not be worth the effort, especially for a first draft/prototype of a new story mod integration. It's likely worth talking to me in person if you're unsure whether you need or want a custom item/location, or unsure how to implement one.

The one other randomizer feature you need to be familiar with now is the `logsanity` option, which adds locations for every non-rumor ship log fact. Many randomizers have options to change how many locations are produced, and this is the main one for Outer Wilds. The locations you normally get for the base game and (if enabled) DLC and story mods even when `logsanity` is `false` are called "default locations". Importantly, a default location might be triggered by anything. The locations that get added when `logsanity` is `true` are naturally called "logsanity locations", but these have a strict one-to-one mapping to ship log fact ids (as you'll soon see in the code) and can't be triggerd by anything else.

It's entirely up to you what the default locations for your story mod should be, but the goal should be to choose locations that will have a variety of logic rules / blocking items, so that randomizing your story mod multiple times won't make players go through the content the same way each time. The logsanity locations should cover all of that mod's ship log facts, but even that isn't a strict requirement and you may skip some (for example, the HN2 integration ignores the bonus ship logs for playing HN1 first). You could even choose not to offer any logsanity locations at first. As a general rule of thumb, a story mod's default location count should be roughly half of its logsanity location / ship log fact count.

Conceptually, each AP location needs:
- an id number
- a name
- a logic rule, i.e. which item(s) are needed to "check" this location
- a trigger condition in the mod
	- most commonly: default locations triggered by ship log facts need to be in LocationTrigger.cs's `logFactToDefaultLocation` map
- a description and a ship log entry id for the in-game tracker (the entry id is used to get a thumbnail image)

Similarly, each frequency AP item needs:
- an id number
- a name
- to be in the logic for scanning the corresponding signal locations
- entries in the mod's frequency/signal lists/maps

To demonstrate exactly how this is all done in practice, study these two example pull requests for integrating the Fret's Quest story mod:
- TODO
- TODO

If all of this makes sense so far, then "just" open two similar pull requests for your mod, and I'll take a look at them.

If you're the maintainer of the story mod you want to integrate, you may be wondering why you have to edit the randomizer's code, instead of the randomizer "talking to the story mod" through `.TryGetModApi<IMyOWMod>("me.MyOWMod")`. The short answer is that no mod-to-mod communication can help with item/location metadata, because that metadata is needed *at generation time*, i.e. before anyone is running Outer Wilds or any of its mods. This separation of generation time from runtime is one reason the Archipelago system can scale to hundreds of games and thousands of slots/players in a single multiworld. That said, a mod interface may still be helpful for custom item/location implementations.

### Spoiler Policy

- AP item names, AP location names, AP options and option values should **avoid all Outer Wilds spoilers** because they can be seen and will be discussed by non-Outer Wilds players.
  - Consider the location name `"QM: Explore the Sixth Location"`. A clearer name might be `"QM: Meet Solanum"`, but that's a spoiler.
  - For the "option names and values" part, consider `goal`. The values are e.g. `song_of_five` and `song_of_the_nomai` to avoid spoilers about Solanum or the Prisoner. But the *descriptions* of those values do mention Solanum and the Prisoner.
- AP option *descriptions* should **avoid story mod spoilers** because they will be read only by Outer Wilds randomizer players, by that includes OW players who haven't played any story mods.
  - Consider the `enable_ac_mod` description. I do have to document that it's compatible with `randomize_warp_platforms`, but I avoid mentioning *why* Astral Codec cares about warp platforms.
- In-game tracker descriptions should **use spoilers liberally** because they will only be seen by players who opted into that content (DLC or story mod), and thus should have already played it unrandomized. Their main purpose is to remind a veteran player about details they've likely forgotten, so they shouldn't mince words.
  - Taking the Sixth Location example from before: that location's tracker descriptions make it clear you're supposed to talk to someone to get this AP location. And if logsanity is on, the descriptions for the other Sixth Location logs even tell you which symbols to combine to get each log fact from her.

## Running From Source

### Prerequisites

In addition to the prerequisites from [Installation](README.md#installation):

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
