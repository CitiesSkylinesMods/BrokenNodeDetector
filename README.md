# BrokenNodeDetector
## Cities Skylines mod for detecting broken nodes


### __First release of Broken Nodes Detector__

A mod for __Cities Skylines__ to temporarily help users to find broken nodes.

Broken nodes makes __[Traffic Manager: President Edition](https://github.com/krzychu124/Cities-Skylines-Traffic-Manager-President-Edition)__ unusable due to despawn of vehicles after creating or upgrading road segment.

__Hit _Ctrl+Zero_ to show mod menu in-game.__

# Features
- Automatically detect broken nodes
- Move camera to broken node

# Changelog
### [0.4](https://github.com/krzychu124/BrokenNodeDetector/compare/0.3...0.4), 03/01/2020

- ghost nodes detector(with auto-remove)
- detector for public transport line and stop issues: 
  - detect empty lines,
  - detect incomplete lines,
  - detect not connected stops,
  - cycle through not connected stops,
  - remove selected stop,
  - remove selected line,
- improved logging

### [0.3](https://github.com/krzychu124/BrokenNodeDetector/compare/0.2...0.3), 15/07/2019

- `NullReferenceException` bug fix (thanks aubergine10) (#2)
- code cleanup

### [0.2](https://github.com/krzychu124/BrokenNodeDetector/compare/0.1...0.2), 22/06/2019
_New feature:_
- cycle through broken nodes (automatically skip removed or invalid). No need to re-run detector


### [0.1](https://github.com/krzychu124/BrokenNodeDetector/releases/tag/0.1), 22/06/2019

- detect infinite loop of node updates,
- click 'Move to next' to teleport camera to broken node.
