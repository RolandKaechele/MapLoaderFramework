# MapLoaderFramework API Reference

This document provides detailed information about the main classes, methods, extension points, and warp event handling in the MapLoaderFramework.


## Main Classes

- **MapLoaderFramework**: Main loader class for managing map loading, switching, and event notification.
- **MapLoaderManager**: Entry point for loading maps from UI or scripts. Also exposes mod management and chapter loading.
- **MapLoader**: Handles standard map loading, instantiation, placement, and cleanup for direct map connections (edge-to-edge, e.g., doors, paths).
- **MapData**: Data structure representing a map (parsed from JSON), including both direct connections and warp events.
- **MapWarpLoader**: Handles all logic related to warp event maps (instantiation, placement, cleanup).
- **MapWarpConnection**: Data structure representing a warp event connection (teleport, warp, etc.) between maps.
- **ModManager**: Discovers, enables, and disables mod folders at runtime; exposes per-file-type enumerables for maps, scripts, mini-games, and DLC packs.
- **ModManifest**: Deserialised representation of a `mod_manifest.json` file; carries `map_files`, `script_files`, `minigame_files`, and `dlc_pack_files` lists.
- **ScriptManager**: Handles loading and executing Lua scripts (MoonSharp).

## Extension Points

- Use C# interfaces, events, or script hooks for custom logic.
- Extend map JSON with new fields or warp event types.
- Implement custom logic for warp event handling by extending or subscribing to MapWarpLoader.
- Add opt-in bridge components for other managers (MiniGameManager, DlcManager, AudioManager, CutsceneManager, etc.) using scripting-define guards (`#if DEFINE`).
  - See [Extending the Framework](Extending_the_Framework.md) for the full bridge pattern.


## Map Connections

Maps can be connected in two ways:

### 1. Direct Map Connections (Edge-to-Edge)

Direct connections represent standard adjacency, such as doors, hallways, or paths between maps. These are managed by `MapLoader` and represented in each `MapData` as a list of `MapConnection` objects:

- **MapConnection** fields:
        - `mapId`: ID of the directly connected map.
        - `direction`: Direction of the connection (e.g., "up", "down", "left", "right").

### 2. Warp Event Map Connections

Warp event connections allow maps to define special teleports or transitions to other maps. These are managed by `MapWarpLoader` and represented in each `MapData` as a list of `MapWarpConnection` objects:

- **MapWarpConnection** fields:
        - `id`: Unique identifier for the warp event.
        - `src_x`, `src_y`: Source coordinates of the warp.
        - `dest_map`: Destination map ID.
        - `dest_x`, `dest_y`: Destination coordinates.

See the [Class Diagram (PlantUML)](MapLoaderFramework_ClassDiagram.puml) for relationships.

## Diagrams

- [Class Diagram (PlantUML)](MapLoaderFramework_ClassDiagram.puml)
- [Load Sequence Diagram (PlantUML)](MapLoaderFramework_LoadSequence.puml)

See the main README and Integration Guide for integration steps and usage examples. All map files should be placed in `Assets/InternalMaps` or `Assets/ExternalMaps`.


## ModManager API

| Method | Return | Description |
| ------ | ------ | ----------- |
| `DiscoverMods()` | void | Scans the `Mods/` folder and populates the mod registry |
| `EnableMod(modId)` | void | Enables a discovered mod and fires `OnModsChanged` |
| `DisableMod(modId)` | void | Disables a mod and fires `OnModsChanged` |
| `GetDiscoveredMods()` | `IEnumerable<ModManifest>` | All discovered mod manifests |
| `GetEnabledModMapFiles()` | `IEnumerable<(string filePath, string modId)>` | Map JSON files from all currently enabled mods |
| `GetEnabledModScriptFiles()` | `IEnumerable<(string filePath, string modId)>` | Lua script files from all currently enabled mods |
| `GetEnabledModMiniGameFiles()` | `IEnumerable<(string filePath, string modId)>` | MiniGame JSON files from enabled mods (`minigames/` subfolder) |
| `GetEnabledModDlcPackFiles()` | `IEnumerable<(string filePath, string modId)>` | DLC pack JSON files from enabled mods (`dlcpacks/` subfolder) |
| `OnModsChanged` | `event Action` | Fired whenever the enabled set changes |

### ModManifest Fields

| Field | Type | Description |
| ----- | ---- | ----------- |
| `mod_id` | string | Unique identifier |
| `name` | string | Display name |
| `author` | string | Author name |
| `version` | string | Mod version |
| `description` | string | Short description |
| `enabled` | bool | Whether this mod is active |
| `map_files` | `List<string>` | Map JSON filenames in `maps/` subfolder |
| `script_files` | `List<string>` | Lua script filenames in `scripts/` subfolder |
| `minigame_files` | `List<string>` | MiniGame JSON filenames in `minigames/` subfolder |
| `dlc_pack_files` | `List<string>` | DLC pack JSON filenames in `dlcpacks/` subfolder |
| `min_game_version` | string | Minimum required game version |
| `dependencies` | `List<string>` | ModIds that must be enabled first |

## Map Change Notification API

The following API allows other components to receive updates when a map's raw JSON changes:

### Events

- `event Action<MapData> OnMapLoaded` — Fired on every root map change (direct loads, chapter loads, warp navigation). Use this to react to any map transition (audio, UI, etc.).
- `event Action<int, int> OnChapterChanged` — Fired when `LoadChapter(n)` is called; parameters are (previousChapter, newChapter).
- `event Action<string, string> OnRawJsonUpdated` — Triggered when a map's rawJson is loaded or updated. Parameters: `(mapId, rawJson)`.

### Methods

- `void SubscribeToRawJson(Action<string, string> callback)` — Subscribe to receive rawJson updates for all maps.
- `void UnsubscribeFromRawJson(Action<string, string> callback)` — Unsubscribe from rawJson updates.
- `string GetRawJson(string mapId)` — Get the current rawJson for a loaded map by id.
