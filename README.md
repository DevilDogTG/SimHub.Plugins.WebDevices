# DevDogs Web Devices

A SimHub plugin that switches the dashboard on every phone and tablet showing SimHub in a browser,
automatically, by game and by what you are doing in it. Each device opens one fixed URL once,
`http://<simhub-pc>:8890/<device-name>/`, and never needs touching again.

## 🚀 Features
- **One URL per device.** Add devices by name in SimHub; each gets its own page to open in the device's browser.
- **Three states per game.** Different dashboards when no game is running, when a game is in its menus
  (loading, main menu, paused) and while driving, with a default for games you have not set up.
- **Instant, touch-free switching.** Changes are pushed to every device as they happen; game switches in
  SimHub do not disconnect anything.
- **Works on old devices.** Tested design targets are an iPad mini 2 (iOS 12, Safari 12) and a Galaxy S8;
  no app to install, just a browser.
- **The DevDogs navigation map too.** With the DevDogs Navigation Dash plugin
  installed, pick **DevDogs Navigation Dash** for any state, such as ETS2 *Driving*, and the same device URL shows
  its map.
- **Managed inside SimHub.** A "Web Devices" tab lists devices with live connection status and edits every
  mapping; export and import the whole configuration as JSON.
- **No network setup.** Uses SimHub's existing firewall rule; no admin rights, URL reservations or certificates.

## 🛠️ Getting Started

### Prerequisites
- Windows with [SimHub](https://www.simhubdash.com/) (developed against v9.13.2).
- Phones or tablets on the same network as the SimHub PC, with a browser (Safari on iOS 12 or later,
  Chrome on Android).
- SimHub's own web dashboards working on those devices (`http://<simhub-pc>:8888/`).

### Installation
1. Download `DevDogs.WebDevices-<version>.zip` from the releases page.
2. Close SimHub.
3. Copy `DevDogs.WebDevices.dll` and `DevDogs.WebDevices.Core.dll` into the SimHub folder
   (usually `C:\Program Files (x86)\SimHub`).
4. Start SimHub and enable **DevDogs Web Devices** when asked.

Updating from 1.1.x: SimHub asks to enable the plugin again, because its class name changed in 1.2.0, and
**Web Devices** has to be put back in the left menu. Your devices and settings are carried over.

### Usage
1. Open **Web Devices** in SimHub's left menu.
2. On the **Devices** tab, click **Add device** and give it a name such as `ipad-left` (lowercase letters, digits, `-` and `_`).
3. Pick dashboards:
   - **When no game is running**: shown while no game is detected.
   - **Any other game**: *In menus* and *Driving* for every game without its own row.
   - **Per game**: pick a game (or **Add current game**) and set its *In menus* and *Driving* dashboards.
     An empty box uses what **Any other game** shows; its dimmed text says what the device shows instead.

   Each dashboard box works like SimHub's own picker: it shows the dashboard's preview and title, and opens a list
   with a search box (name, category or author; arrow keys and Enter work too). The list is grouped by the
   categories you set in SimHub's Dash Studio, with *Uncategorized* last, and holds the same dashboards SimHub's web
   page offers: overlays and dashboards made for one kind of device are left out. A device already set to one of
   those keeps it, marked *hidden in SimHub's list*. **×** empties a box. **DevDogs Navigation Dash** has its own
   group, *DevDogs Plugins*, at the top when that plugin is installed: see
   [Navigation Dash on a device](#navigation-dash-on-a-device).
4. Click **Save** on the tab. Changes apply immediately; no restart is needed. Each tab (**Devices**,
   **General**) has its own **Save** and **Revert**, so unsaved changes on one tab are kept when you save the other.
5. On the device, open the URL shown under **Open on device** (use **Copy**). On iOS, *Share → Add to Home
   Screen* gives a full-screen, SimHub-branded launcher.

Set the device's own screen timeout to "never" (or use a kiosk browser): over plain HTTP, browsers cannot
keep the screen awake by themselves.

### Navigation Dash on a device
Pick **DevDogs Navigation Dash** in any dashboard box (it is listed only when the DevDogs Navigation Dash plugin is
installed in SimHub). The device then shows Navigation Dash's map, from that plugin's own port (8893 by default,
read from SimHub), instead of a SimHub dashboard.
- **Pick it for both *In menus* and *Driving*** of a truck game to keep the map, and whatever you did on it, when
  the game pauses or you open its menu. A device whose choice does not change is never reloaded.
- **Until the map can be shown** (Navigation Dash still loading map data, or not installed) the device shows a short
  message saying why, and the map appears by itself when it is ready.
- **The map needs WebGL 2:** Chrome or Edge on Android 11 or later. Older devices, such as an iPad mini 2, show
  Navigation Dash's "device not supported" page instead; keep SimHub dashboards on those.
- Touch works on the map as usual (drag, pinch, rotate, press and hold). Keeping the screen awake is unchanged: set
  the device's own screen timeout.

## 🔀 How switching works

| State | When | Dashboard used (first one set wins) |
|---|---|---|
| **No game running** | No game process and no telemetry | *When no game is running* |
| **In menus** | Game running but loading, in its menus or paused | game *In menus* → *Any other game · In menus* → whatever *Driving* would show |
| **Driving** | Telemetry flowing, not in menus, not paused | game *Driving* → *Any other game · Driving* → *When no game is running* |

- Switching to *No game running* waits 3 s by default, so restarting a game does not flash every device.
  Both delays are on the **General** tab.
- A device whose dashboard does not change is not reloaded.
- Games are matched by SimHub's game code (custom games appear by name in the tab).

## 🩺 Troubleshooting

| Problem | What to check |
|---|---|
| Device page does not load | Same network as the PC? The **Status** line should say *Listening on port 8890*. If it reports the port is in use, reserved by Windows or one of SimHub's own web ports, choose another **Device port**. |
| SimHub's own dashboards stop working after installing | The **Device port** must not be SimHub's web or HTTPS port (8888 / 8889 by default, set in SimHub's web server settings). The plugin refuses those ports; older settings may still name one: change **Device port**, save, and restart SimHub. |
| "… is not configured in SimHub" | The name in the URL must match a saved device. The page picks the device up by itself once it is added and saved. |
| Page loads but stays black | SimHub's own web server must be reachable: try `http://<simhub-pc>:8888/` on the device; check **SimHub web port**. |
| Black screen with "Reconnecting…" or "Waiting for SimHub…" in the corner | The device lost the connection (Wi-Fi drop, SimHub closed or restarting). It recovers by itself. The screen goes black after 3 s offline so devices left on do not burn in. |
| Game not detected | SimHub must switch to the game (its current game shows in **Status**). Process detection for custom games does not work for programs running as administrator. |
| A dashboard is missing from the list, or in the wrong group | The list is read when SimHub opens the tab: reopen it (or restart SimHub) after adding dashboards or changing categories in Dash Studio. Overlays are never listed. |
| "DevDogs Navigation Dash is not installed in SimHub" | Install and enable the DevDogs Navigation Dash plugin, or pick another dashboard for that state. |
| "DevDogs Navigation Dash is not ready" | Navigation Dash is loading its map data; the smaller line gives its reason. The map appears by itself. |
| Map shows "device not supported" | The device's browser has no WebGL 2 (see [Navigation Dash on a device](#navigation-dash-on-a-device)); use a SimHub dashboard on it. |
| Menus show the driving dashboard | Some games never report "in menus". ETS2/ATS report their Esc menu as paused, which counts as in menus; sitting in the truck before driving counts as driving. |

Every switch, connection and raw game flag is logged with a `[WebDevices]` prefix in SimHub's log
(`SimHub\Logs\SimHub.txt`).

## 🏗️ Architecture
- **`DevDogs.WebDevices.Core`** (netstandard2.0, no SimHub dependency): settings and validation, game-state
  detection, dashboard resolution, switch debouncing, the device registry, a minimal HTTP/1.1 server with
  Server-Sent Events, the device page, and the settings-tab view models.
- **`DevDogs.WebDevices`** (.NET Framework 4.8, WPF): the SimHub plugin. Feeds SimHub's game data into a
  shared host that survives SimHub re-creating plugins on every game switch, and provides the settings tab.
- The device page frames SimHub's own dashboard page (`:8888/Dash`), or DevDogs Navigation Dash's page from its own
  port, full screen and swaps it when told to.

Design decisions are recorded in [`docs/adr/`](docs/adr/).

## 🤝 Contributing

### Build and test
Needs the .NET 10 SDK and SimHub installed (its assemblies are referenced from the install folder; pass
`-p:SimHubPath=<folder>` if it is not in the default place).
```bash
dotnet build DevDogs.WebDevices.slnx
```
```bash
dotnet test tests/DevDogs.WebDevices.Core.Tests
```

### Try a build in SimHub
Close SimHub first; the script builds and copies the plugin into the SimHub folder.
```bash
pwsh ./build/Install-DevBuild.ps1
```

### Try the device page without a game
The dev host serves device pages from the real server with game states typed in (`idle`, `menu <game>`,
`drive <game>`, `pause <game>`, `reload`, `quit`) and Navigation Dash's status set by hand (`nav ready [port]`,
`nav wait [reason]`, `nav none`). SimHub must be running for the dashboards themselves. Navigation Dash's own dev
host also uses port 8891: run this one on another port next to it.
```bash
dotnet run --project tools/DevDogs.WebDevices.DevHost -- --port 8891
```

### Package a release
```bash
pwsh ./build/New-ReleasePackage.ps1 -Version 0.1.0
```

## 📄 License
No license has been chosen yet; all rights reserved until one is added.
