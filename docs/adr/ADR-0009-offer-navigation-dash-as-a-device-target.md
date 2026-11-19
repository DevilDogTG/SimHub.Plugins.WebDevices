# ADR-0009: Offer DevDogs Navigation Dash as a device target

**Date:** 2026-11-19
**Status:** proposed
**Deciders:** DevilDogTG (project owner)

## Context
DevDogs Navigation Dash (plugin `DDNavigationDashPlugin`) serves a map page from its own HTTP server, default
port 8893, at `/`. The owner wants a device to keep its one WebDevices address and show that map while driving,
and SimHub dashboards otherwise. Of the options offered (a ready-made "Navigation Dash" choice, or a generic
"type a URL" choice), the owner chose the ready-made one (2026-11-19).

Until now every target was a SimHub dashboard: a folder name under `DashTemplates`, framed from SimHub's web
port (ADR-0002). The navigation page differs in three ways:
- **Another server.** Its port comes from the SimHub property `DDNavigationDashPlugin.Dash.Url`
  (`http://<LAN IP>:<port>/`). The device page uses its own `location.hostname`, as for SimHub dashboards.
- **It can be missing or not ready.** `DDNavigationDashPlugin.Status.Ready` is false until the truck plugin is
  present and the map data is loaded; `DDNavigationDashPlugin.Status` says why.
- **A reload loses what the driver did on it** (pan, zoom, a pinned destination), so it must not reload when
  ETS2 pauses, as SimHub dashboards already do not (the router keeps an unchanged target).

## Options Considered

### Option A: A reserved code in the existing dashboard fields
Store `devdogs:navigation-dash` in the same fields as SimHub codes; the pickers show it as
"DevDogs Navigation Dash".
- **Pros:** Works per state and per game with no new settings; the resolver's fallback chain is unchanged;
  `:` cannot appear in a Windows folder name, so no `DashTemplates` code can collide with it.
- **Cons:** The pickers need a display-name mapping; the code is a string that must never change.

### Option B: A separate "kind" field next to every dashboard field
- **Pros:** Explicit in the settings file.
- **Cons:** Doubles every field (system idle, default and per-game idle/driving), the editor and the resolver.

### Option C: A generic "custom URL" target
- **Pros:** Any page could be framed.
- **Cons:** Not the owner's choice; the user would type the address and port, which change with the PC.

## Decision
Option A. The target carries its own port (SimHub's web port, or the port parsed from `Dash.Url`, 8893 when it
cannot be read) and path (`/` for the map). While Navigation Dash is not installed or not ready, the device shows
a short message with the reason instead of the map, and the map appears by itself once it is ready (owner's
choice: "short message"). The choice is listed in the pickers only when Navigation Dash is installed when the
settings tab opens (owner's choice); a device that uses it while it is missing gets a warning, not an error.

## Consequences
**Easier:** One device address shows the map and SimHub dashboards; the map stays loaded across pause and menus
when chosen for both.
**Harder:** The device page now takes the port from each target, not one SimHub port. A page left open from an
older version frames SimHub's port until it is reloaded.
**Follow-up:** No debounce on `Status.Ready`: a flicker while driving would reload the map. Revisit if seen.
The code `devdogs:navigation-dash` is fixed from here.
