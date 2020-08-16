# OrchestrionPlugin
A plugin for goat's [XivLauncher](https://github.com/goaaats/FFXIVQuickLauncher) that adds a simple music player interface to control the in-game BGM, allowing you to set it to any in-game track you want.  The BGM will persist through **most** changes of zone/instance/etc, and usually will stay active until you change it or click Stop.

You can search for tracks by name or by assorted metadata, such as zone, instance or boss name where the track is usually played.

You can also mark tracks as favorites to easily find them again later.

![Usage](https://github.com/ff-meli/OrchestrionPlugin/raw/master/gh/orch.gif)

## Installation
Use /xlplugins in game to bring up the plugins menu, and install directly from there.

Manual installation (not detailed here) is not recommended as it can cause a number of issues and prevent future updates from applying correctly.

## Usage
The "All Songs" tab will display every song in the game.

The "Favorites" tab will display only those tracks that you have marked as favorites.

### Controls
To bring up the player:
* Type `/porch` in the game chat window ('p' for plugin, 'orch' for orchestrion).
* You can also display the player by clicking the "Open Configuration" button in the /xlplugins menu.

To search for a track:
* Just start typing in the search box.  If you know the name, that is best, but many tracks also have metadata about the zone/instance/boss/etc they are from, so you might find it that way too.

To play a song:
* Double-click on the track in the player
* **OR** Select a track and click the Play button.

To restore the 'normal' game BGM:
* Click the Stop button.

To add a song as a favorite:
* Right-click on the song in the player, and select "Add to favorites" from the dropdown menu that appears.

To remove a song from your favorites:
* Right-click on the favorited song in the player (either tab), and select "Remove from favorites" from the dropdown menu that appears.

## (Mini) FAQ

### Why are the song numbers skipping around?  They don't even start at 1!
Those numbers are the internal ids used by the game.  Many numbers do not correspond to playable tracks, and so I don't display them in the player.

### It's so hard to find certain tracks!  Can you add/change/remove (some specific info)?
In short, no.

All the song information in the player is taken from [this spreadsheet](https://docs.google.com/spreadsheets/d/14yjTMHYmuB1m5-aJO8CkMferRT9sNzgasYq02oJENWs/edit#gid=0), which I do not maintain.

You might be able to ask in the CMTool discord if there is information you think should be added etc, as it is (was?) maintained from there.  But I am not involved in that process.

### Some new in-game music is out and I can't find it!
If the tracks are new, it is possible that either the spreadsheet has not been updated by the community yet, or that I have not pulled in the latest updates to it.

### I have a suggestion/issue/concern!
Mention it in the XL discord, or create an issue here.

## Credits
* goat, for the launcher and dalamud, without which none of this would be possible.
* MagowDeath#1763 for maintaining [the community spreadsheet](https://docs.google.com/spreadsheets/d/14yjTMHYmuB1m5-aJO8CkMferRT9sNzgasYq02oJENWs/edit#gid=0) with all of the song data that is used in this plugin.
* Many thanks to [Caraxi](https://github.com/Caraxi/) for keeping things working and updated while I was away!
* Too many discord people to name, for helping out with things and offering suggestions.
