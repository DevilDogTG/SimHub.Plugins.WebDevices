# ADR-0008: Rename the plugin class to `DDWebDevicesPlugin`

**Date:** 2026-11-16
**Status:** accepted (supersedes ADR-0005)
**Deciders:** DevilDogTG (project owner)

## Context
ADR-0005 named the plugin class `WebDevicesPlugin` and said it must never change. Since then the other
DevDogs plugins settled on one naming rule (written down in the NavigationDash repo's ADR-0005, accepted
2026-11-15): the plugin class, and so the SimHub prefix, is `DD<Name>Plugin` (`DDTruckPlugin`,
`DDSystemInfoPlugin`, `DDNavigationDashPlugin`). On 2026-11-15 the owner asked for this plugin to follow it.

What the class name touches in this plugin, checked against SimHub on the owner's PC:
- **Properties, events, actions:** none are registered, so no dashboard binding breaks.
- **Settings file:** SimHub's `ReadCommonSettings` and `SaveCommonSettings` name the file
  `PluginsData\Common\<class name>.<key>.json`. After the rename SimHub reads
  `DDWebDevicesPlugin.WebDevicesSettings.json`, which does not exist yet, so every device would be lost.
- **Enabled state:** SimHub records plugins by full class name in `PluginsData\PluginsActivation.json`. The
  renamed class is a new plugin to SimHub: it asks once to enable it, and the left-menu pin is set again.
- **The shared host** (ADR-0007) is a static keyed by nothing named, so it is unaffected.

## Options Considered

### Option A: Rename to `DDWebDevicesPlugin` and carry the settings file over
On start, before SimHub reads the settings, copy `WebDevicesPlugin.WebDevicesSettings.json` to the new name
when only the old file exists. The old file is left in place.
- **Pros:** Same naming as the other DevDogs plugins; device configs survive the update.
- **Cons:** Users enable the plugin once more and pin it to the left menu again. A little migration code
  that stays until nobody updates from 1.1.x.

### Option B: Keep `WebDevicesPlugin` (ADR-0005)
- **Pros:** No migration, no re-enable.
- **Cons:** The one DevDogs plugin that breaks the shared naming; not the owner's choice.

### Option C: Rename without a migration
- **Pros:** No migration code.
- **Cons:** Every user loses every device config on update.

## Decision
The plugin class is `DevDogs.WebDevices.DDWebDevicesPlugin`, so its SimHub prefix is `DDWebDevicesPlugin.*`,
as chosen by the owner to match the other DevDogs plugins. On start the plugin copies the settings file
from the old class name when the new one is missing; if both exist, the new one wins.

## Consequences
**Easier:** One naming rule across the DevDogs plugins.
**Harder:** Updating from 1.1.x needs the plugin enabled again in SimHub. The old settings file stays on
disk and is no longer updated.
**Follow-up:** The class name is fixed again from here: properties, events or actions added later carry
the `DDWebDevicesPlugin.*` prefix.
