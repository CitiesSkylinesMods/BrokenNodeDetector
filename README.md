# Broken Nodes Detector

Locates invisible problems with your transport system and helps fix them.

![Mod Panel](https://user-images.githubusercontent.com/1386719/72827820-63b60400-3c73-11ea-9f24-740eb4dd8da7.png)

Press **Ctrl+Zero** in-game to display the mod panel _(configurable in mod options)_.

## Fix broken nodes

The [broken node bug](https://github.com/CitiesSkylinesMods/TMPE/issues/277) causes vehicles to despawn when they reach a certain segment of road or rail.

* Click **Run detector** to scan the city
* It will find one problem at a time and highlight it on the map
* Either bulldoze it or move it slightly to repair
* Repeat the process until no problems found

## Fix ghost nodes

Ghost nodes can cause "Array Index" errors and problems with pathfinding.

* Click **Remove Ghost Nodes** to find and remove any ghost nodes

## Fix transport routes and stops

Broken transport routes can prevent vehicles from spawning or prevent cims from reaching their destinations.

* Click **Detect disconnected PT stops** to scan the city
* Problems are listed one at a time; click **Find next** to see each one
* Click **Remove** to delete a broken line or stop

## Changelog

### [0.5](https://github.com/CitiesSkylinesMods/BrokenNodeDetector/compare/0.4...0.5), 15/09/2020

- configurable shortcut for opening/closing mod main menu
- migrated patches from RedirectionFramework to CitiesHarmony API

### [0.4](https://github.com/CitiesSkylinesMods/BrokenNodeDetector/compare/0.3...0.4), 03/01/2020

- ghost nodes detector(with auto-remove)
- detector for public transport line and stop issues:
  - detect empty lines,
  - detect incomplete lines,
  - detect not connected stops,
  - cycle through not connected stops,
  - remove selected stop,
  - remove selected line,
- improved logging

### [0.3](https://github.com/CitiesSkylinesMods/BrokenNodeDetector/compare/0.2...0.3), 15/07/2019

- `NullReferenceException` bug fix (thanks aubergine10) (#2)
- code cleanup

### [0.2](https://github.com/CitiesSkylinesMods/BrokenNodeDetector/compare/0.1...0.2), 22/06/2019

- cycle through broken nodes (automatically skip removed or invalid); no need to re-run detector

### [0.1](https://github.com/CitiesSkylinesMods/BrokenNodeDetector/releases/tag/0.1), 22/06/2019

- detect infinite loop of node updates,
- click 'Move to next' to teleport camera to broken node.
