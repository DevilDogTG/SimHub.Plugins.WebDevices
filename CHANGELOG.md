# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project uses
[Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added
- **DevDogs Navigation Dash** as a choice in every dashboard box, listed when the DevDogs Navigation Dash plugin is
  installed: the device shows its map from that plugin's own port. Chosen for both *In menus* and *Driving*, the map
  is not reloaded when the game pauses. Until Navigation Dash is ready (or while it is not installed) the device
  shows a short message with the reason. The map needs a WebGL 2 browser (Chrome or Edge on Android 11+).
- Dev host commands `nav ready [port]`, `nav wait [reason]` and `nav none`.
- A dashboard picker like SimHub's own in every dashboard box: preview and title, a search box (name, category or
  author) and a list grouped by your Dash Studio categories, with DevDogs Navigation Dash in its own *DevDogs Plugins*
  group at the top. Empty boxes say what the device shows instead.

### Fixed
- Changing **SimHub web port** now reloads open devices from the new port straight away.

### Changed
- Dashboards show by their SimHub title rather than their folder name (*ATS-DASH-ROUTER-ADV* for `ATS-DASH`); the
  settings still store the folder name, so existing devices are unchanged.
- The dashboard list leaves out overlays and device-only dashboards, as SimHub's own web page does. Devices already
  using one keep it.
- The plugin class is now `DDWebDevicesPlugin`, named like the other DevDogs plugins. SimHub sees it as a
  new plugin: after updating, enable **DevDogs Web Devices** again when asked and put **Web Devices** back
  in the left menu. Devices and settings are carried over from 1.1.x on first start; the old settings file
  is left in place.
- The plugin's author is now DevilDogTG; the company stays DevDogs.
- The repository is now `DevilDogTG/SimHub.Plugins.WebDevices`, named like the other DevDogs plugin repositories.
  Old GitHub links and clones redirect.

## [1.1.0] - 2026-11-15

First public release (1.0.x were local test builds).

### Added
- Per-device URLs `http://<simhub-pc>:8890/<device-name>/` that show SimHub web dashboards full screen.
- Automatic dashboard switching per game and per state: no game running, in menus (loading, menus,
  paused) and driving, with defaults for unmapped games and a fallback chain for empty settings.
- Switch delays, so a game restart does not flash every device (3 s before "no game running" by default).
- Live push to devices over Server-Sent Events, with polling fallback, automatic reconnect and a
  "Reconnecting…" notice. Brief drops keep the dashboard on screen; after 3 s the screen goes black
  with only a small, dim, slowly drifting notice in the corner, so devices left on do not burn in.
- Device connections survive SimHub re-creating plugins on every game switch.
- "Web Devices" settings tab in SimHub: live status above a **Devices** tab (devices with connection
  status, current dashboard and copyable URLs, per-game mappings with SimHub's game list and "Add current
  game", dashboard pickers) and a **General** tab (ports, delays, JSON export and import), each with its
  own validation and Save/Revert.
- "Not configured" page for unknown device names, picked up automatically once the device is added.
- The device server never takes SimHub's own web or HTTPS port (read from SimHub's settings): the
  default device port is 8890, since 8889 is SimHub's HTTPS port, and the **General** tab and status line
  explain a clash instead of breaking SimHub's web server.
- `[WebDevices]` log lines for switches, connections and SimHub's raw game flags.
- Developer tools: dev install script, release package script and a dev host for testing the device page
  without a game.

[Unreleased]: https://github.com/DevilDogTG/SimHub.Plugins.WebDevices/compare/v1.1.0...HEAD
[1.1.0]: https://github.com/DevilDogTG/SimHub.Plugins.WebDevices/releases/tag/v1.1.0
