## Contribution Policy

Genearally speaking, contributions are welcome. But since there's always the possibility I'd end up rejecting a change, please talk to me if you're unsure before investing significant effort.

### Bugfixes

If something is obviously incorrect and easy to fix, just go ahead and open a pull request for it. If you're not sure, it never hurts to ask me first whether a behavior is intentional.

Some [open issues](https://github.com/Ixrec/OuterWildsArchipelagoRandomizer/issues) may be for known bugs that I haven't fixed, because they're for opt-in/off by default features I'm not personally passionate about.

### General Features

For features, please talk to me before doing significant implementation work so we can make sure it fits well with the rest of the randomizer, hasn't been considered and rejected in the past, and so we can discuss what's hard-required to ship it and what's okay to leave for later. In particular, if it's a feature you'd like to see enabled by default (opt-out rather than opt-in), the bar will be pretty high.

Most [open issues](https://github.com/Ixrec/OuterWildsArchipelagoRandomizer/issues) represent features that have been suggested in the past, and that I would be happy to see implemented, but am not motivated to implement myself. These are safe bets to pick up.

### Story Mod Integrations

This is the specific kind of contribution I've gone out of my way to make as easy as possible, since more high-quality Outer Wilds story mods keep coming out and most of them should need little to no "custom" code to integrate.

But before we can talk about how to do that, first make sure you've played the randomizer at least once, and that you've set up running from source as documented in the next section. Instructions for actually integration a story mod are in the section after that.

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

## Story Mod Integration

TODO
