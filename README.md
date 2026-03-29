# Keyboard Accessibility

This is a Slay the Spire 2 mod that adds additional keyboard accessibility settings.

<p align="center">
<a href="https://discord.gg/k7zfrbNTCU">
<img src="https://github.com/CLorant/readme-social-icons/raw/main/large/filled/discord.svg" />
Questions? Find me on discord.
</a>
</p>

The goal of this mod was to make the game mostly playable by keyboard only, and
primarily with left-hand only. As implemented today, these keyboard shortcuts
are not exhaustive over game functionality, but there's a few options (like
playing potions) that I don't mind clicking around for, so I haven't
implemented them. All keyboard shortcuts here are additive over what's in the
base game.

This mod is primarily developed and tested on MacOS, but should work cross-platform.

This mod hides itself from the built-in mod detection, in part because it was
initially developed within the first 24 hours of release where the mod API was
incomplete, and because it's the only mod I'm using and I don't care to have a
separate game saves for mods, since this does not modify behavior.

# Installation

Download the latest release and copy the KeyboardAccessibility directory into:

* MacOS: `~/Library/Application Support/Steam/steamapps/common/Slay the Spire 2/SlayTheSpire2.app/Contents/MacOS/mods/`
* Linux: `~/.steam/steam/steamapps/common/Slay the Spire 2/mods/`
* Windows: `%ProgramFiles(x86)%\Steam\steamapps\common\Slay the Spire 2\mods\` (I think? Untested)

# Functionality

During combat, there's a setting for whether to auto-play cards when they have
one target. It can be enabled/disabled with F7 or in the Settings->General
menu. Cards that are self-targeted can be played with Space or Enter when
auto-play is disabled.

On card removal screens/the larger card grid, Tab can be used to move between
rows for selection.

Chests can be opened with 1 or Enter.

Other places keyboard shortcuts have been implemented:
* Post-combat rewards
* Neow rewards and event choices
* Map pathing
* Rest site choices

## Settings

The only setting is the auto-play option, found in the menu or togglable with
F7. Settings are persisted to `user://mods/KeyboardAccessibility/config.json`.

# Developing

No effort has been put into making this usable on another machine, but it
should be straightforward for someone experienced. Use `just --list` to see the
few convenience things I did set up.

# Contributing

Contributions are welcome, but it's probably best to open an issue or chat with
me on discord before opening a PR. This is my personal project, if your
contributions are rejected, please accept it and fork if you wish; I'm not able
or interested in debating or accomodating every request. I'm uninterested in
communicating with your agent, the submitter of a PR is expected to be human,
have understood and reviewed and tested all code submitted.
