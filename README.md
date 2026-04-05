# MapLoaderFramework

MapLoaderFramework is a modular Unity framework for loading, managing, and integrating 2D map data (TMX/JSON). It supports dynamic map connections, warp events, Lua scripting, mod support, episodic/chapter-based loading, localization, and additive scene management — all without recompiling your game.

![Overview](Documentation/overview.png)

## Features

- **TMX/JSON map loading** — loads maps from `InternalMaps` (bundled) and `ExternalMaps` (runtime-writable, e.g. mods or DLC)
- **Map connections** — edge-to-edge connections and warp/teleport events; configurable recursive depth
- **Modding system** — drop mod folders into `Mods/`, each with a `mod_manifest.json`; enable/disable mods at runtime without recompiling
- **Chapter / episode loading** — group maps by `chapter_id`; call `LoadChapter(n)` to load all maps for that episode
- **Additive scene loading** — mark a map as `"is_additive": true` to load it alongside the current scene (e.g. a persistent interior)
- **Transition callback** — assign any fade/cutscene implementation to `MapLoaderFramework.TransitionCallback`; natively integrated with [CutsceneManager](https://github.com/RolandKaechele/CutsceneManager) via `MapLoaderBridge`
- **Localization** — embed `localized_names` arrays in map JSON for any number of languages; switch at runtime
- **Lua scripting** — sandboxed MoonSharp Lua scripts in `Scripts/` for event-driven map logic
- **Inspector integration** — all registries, loaded maps, warp events, and Lua scripts are visible in the Unity Inspector
- **StateManager integration** — `Loading` state is pushed at load start and popped on map loaded by StateManager's `MapLoaderBridge` (consumed via `STATEMANAGER_MLF`)
- **DOTween Pro integration** — hooks `TransitionCallback` to wrap every map and chapter switch in a full-screen `Image.DOFade` (fade-to-black → load → fade-to-clear) (activated via `MAPLOADER_DOTWEEN`)
- **Odin Inspector integration** — `SerializedMonoBehaviour` base for full Inspector serialization of complex types; runtime-display fields marked `[ReadOnly]` in Play Mode (activated via `ODIN_INSPECTOR`)


## Installation

### A. UPM via Git (recommended)

1. Open `Packages/manifest.json` in your project.
2. Add to the `dependencies` section:

   ```json
   "com.rolandkaechele.maploaderframework": "https://github.com/RolandKaechele/MapLoaderFramework.git"
   ```

3. Save — Unity fetches the package automatically and keeps it updatable.

### B. Clone into Assets

```sh
git clone https://github.com/RolandKaechele/MapLoaderFramework.git Assets/MapLoaderFramework
```

### C. Import as `.unitypackage`

`Assets › Import Package › Custom Package…` — select the exported `.unitypackage`.

### D. Manual copy

Copy the `MapLoaderFramework/` folder into `Assets/`.

### Post-install setup

Run `postinstall.js` (Node.js required) to create the required folder structure under `Assets/`:

```
Assets/
  InternalMaps/     ← bundled map JSON files
  ExternalMaps/     ← runtime/modded map JSON files
  Scripts/          ← Lua event scripts
  Mods/             ← mod subfolders (each with mod_manifest.json)
  Resources/
    InternalMaps/   ← SuperTiled2Unity prefabs for internal maps
    ExternalMaps/   ← SuperTiled2Unity prefabs for external maps
```


## Quick Start

1. Add an empty GameObject to your scene named `MapLoader`.
2. Attach **MapLoaderFramework** — it auto-adds `MapLoaderManager`, `AutoMapLoader`, and other helpers.
3. Set `defaultMapName` on `AutoMapLoader` to the id of your starting map.
4. Place your map JSON files in `Assets/InternalMaps/` and your SuperTiled2Unity prefabs in `Assets/Resources/InternalMaps/`.
5. Press Play.


## Map JSON Format

```json
{
  "id": "town_square",
  "name": "Town Square",
  "layout": "LAYOUT_TOWN_SQUARE",
  "music": "town_theme",
  "map_type": "outdoor",
  "show_map_name": true,
  "chapter_id": 1,
  "gameplay_type": "exploration",
  "is_additive": false,
  "map_variant": "present",
  "localization_key": "map.town_square",
  "localized_names": [
    { "lang": "en", "value": "Town Square" },
    { "lang": "de", "value": "Stadtplatz" }
  ],
  "unlock_condition": "",
  "mod_id": "",
  "audio": {
    "backgroundMusic": "town_theme.ogg",
    "ambientSounds": ["birds.ogg", "wind.ogg"]
  },
  "connections": [
    { "mapId": "cave_entrance", "direction": "down" }
  ],
  "warp_events": [
    { "id": "warp_to_inn", "src_x": 5, "src_y": 3, "dest_map": "inn_interior", "dest_x": 2, "dest_y": 1 }
  ]
}
```

| Field | Type | Description |
| -- | -- | -- |
| `id` | string | Unique map identifier |
| `layout` | string | SuperTiled2Unity prefab name (without extension) |
| `chapter_id` | int | Groups maps into chapters/episodes; 0 = always available |
| `gameplay_type` | string | `"exploration"`, `"combat"`, `"stealth"`, `"minigame"`, `"cutscene"`, `"space"`, `"underwater"`, `"racing"` |
| `is_additive` | bool | Load alongside current scene instead of replacing it |
| `map_variant` | string | `"present"`, `"past"`, `"future"` — for timeline variants |
| `localization_key` | string | Key for external localization lookup |
| `localized_names` | array | Inline translated name entries `{ "lang", "value" }` |
| `unlock_condition` | string | Flag/condition that must be true to access this map |
| `mod_id` | string | Set automatically by ModManager for mod-provided maps |


## Modding

Place mod subfolders inside `Assets/Mods/` (editor) or `Application.persistentDataPath/Mods/` (build):

```
Mods/
  my_extra_maps/
    mod_manifest.json
    maps/
      extra_dungeon.json
    scripts/
      extra_events.lua
    minigames/          ← NEW: MiniGameManager JSON definitions
      bonus_race.json
    dlcpacks/           ← NEW: DlcManager pack definitions
      vip_pack.json
```

**`mod_manifest.json` example:**

```json
{
  "mod_id": "my_extra_maps",
  "name": "Extra Dungeon Pack",
  "author": "Community",
  "version": "1.0.0",
  "description": "Adds a bonus dungeon.",
  "enabled": true,
  "map_files": ["extra_dungeon.json"],
  "script_files": ["extra_events.lua"],
  "minigame_files": ["bonus_race.json"],
  "dlc_pack_files": ["vip_pack.json"],
  "min_game_version": "1.0.0",
  "dependencies": []
}
```

Enable/disable mods at runtime via `MapLoaderManager`:

```csharp
mapLoaderManager.EnableMod("my_extra_maps");
mapLoaderManager.DisableMod("my_extra_maps");

// List all discovered mods
foreach (var mod in mapLoaderManager.GetDiscoveredMods())
    Debug.Log($"{mod.name} — enabled: {mod.enabled}");
```

Mod maps that share an `id` with a base-game map will overwrite it. Dependency load order is resolved automatically.


## Chapter / Episode Loading

```csharp
// Load all maps belonging to chapter 3
mapLoaderManager.LoadChapter(3);

// Query which maps belong to a chapter
var maps = mapLoaderManager.GetMapsForChapter(3);
```

`LoadChapter` calls `TransitionCallback(displayName, doLoad)` before swapping maps if the callback is set; otherwise the load happens immediately.

Subscribe to chapter change events on `MapLoaderManager` (or directly on `MapLoaderFramework`):

```csharp
// Preferred: subscribe on the public facade
mapLoaderManager.OnChapterChanged += (previous, current) =>
    Debug.Log($"Chapter {previous} → {current}");

// Alternative: subscribe on the inner component
mapLoaderFramework.OnChapterChanged += (previous, current) =>
    Debug.Log($"Chapter {previous} → {current}");
```


## Fade Transitions

`MapLoaderFramework` exposes a delegate that any external system can hook to drive the fade:

```csharp
// Signature:  Action<string displayName, Action doLoad>
mapLoaderFramework.TransitionCallback = (displayName, doLoad) =>
{
    // fade out, show displayName, call doLoad(), fade in
    myFader.FadeOutAndIn(onMiddle: doLoad);
};
```

When `TransitionCallback` is not set the map swap happens immediately with no animation.

### Recommended: CutsceneManager integration

Install [CutsceneManager](https://github.com/RolandKaechele/CutsceneManager), add the scripting define `CUTSCENEMANAGER_MLF`, and attach `MapLoaderBridge` to any GameObject. `MapLoaderBridge.Awake()` wires `TransitionCallback` automatically using `FadeController`.

### Recommended: AudioManager integration

Install [AudioManager](https://github.com/RolandKaechele/AudioManager), add the scripting define `AUDIOMANAGER_MLF`, and attach `MapLoaderAudioBridge` to any GameObject. It subscribes to `OnMapLoaded` and automatically crossfades to each map's `audio.backgroundMusic` and `audio.ambientSounds`.

`OnMapLoaded` fires on every root map change — including `LoadChapter()`, direct `LoadMap()` calls, and warp-event navigation:

```csharp
mapLoaderFramework.OnMapLoaded += mapData =>
    Debug.Log($"Map loaded: {mapData.id}, BGM: {mapData.audio?.backgroundMusic}");
```

## Map Change Notifications

### Chapter events

```csharp
mapLoaderFramework.OnChapterChanged += (previous, current) =>
    Debug.Log($"Chapter {previous} → {current}");
```

`OnChapterChanged` fires only when `LoadChapter()` is called.

### Map load events

```csharp
mapLoaderFramework.OnMapLoaded += mapData =>
    Debug.Log($"Map loaded: {mapData.id}");
```

`OnMapLoaded` fires on **every** root map change — `LoadChapter()`, direct `LoadMap()` calls, and warp-event navigation. Prefer this event for anything that needs to react to any map transition (e.g. audio changes).

The currently loaded map id is also available as a property at any time:

```csharp
string currentMap = mapLoaderManager.CurrentMapId; // null until first map loads
```

### Visited map tracking

`MapRegistryEntry.hasBeenVisited` is set to `true` the first time a map loads. Read it directly from the registry, or use `SaveManager.HasVisited(mapId)` when the `SAVEMANAGER_MLF` bridge is active.

### Raw JSON notifications

```csharp
mapLoaderFramework.SubscribeToRawJson((mapId, rawJson) =>
    Debug.Log($"Map {mapId} updated"));

mapLoaderFramework.UnsubscribeFromRawJson(callback);

string json = mapLoaderFramework.GetRawJson("town_square");
```


## Lua Scripting

Place `.lua` files in `Assets/Scripts/` (editor) or `persistentDataPath/Scripts/` (build). Scripts are loaded and executed in a sandboxed MoonSharp environment (no `os`, `io`, `require`, or `debug` globals).

Trigger a script from a GameObject:

1. Attach `LuaScriptTrigger`.
2. Set `scriptFileName` to your `.lua` filename.
3. Call `TriggerScript()` from a UI button or event.


## Runtime API Summary

| Class | Key Members |
| -- | -- |
| `MapLoaderManager` | `LoadMap(name)`, `LoadChapter(id)`, `GetMapsForChapter(id)`, `GetAvailableMaps()`, `EnableMod(id)`, `DisableMod(id)`, `GetDiscoveredMods()` |
| `MapLoaderManager` (events) | `OnChapterChanged event Action<int,int>`, `OnMapLoaded event Action<MapData>` — forwarded from `MapLoaderFramework`; subscribe here instead of reaching through to the inner component |
| `MapLoaderManager` (properties) | `CurrentMapId` — id of the most-recently loaded root map; `TransitionCallback` property — get/set proxy to `MapLoaderFramework.TransitionCallback` |
| `MapLoaderFramework` | `LoadMapAndConnections(name)`, `LoadChapter(id)`, `GetMapsForChapter(id)`, `PreloadAllMaps()`, `GetRawJson(id)`, `SubscribeToRawJson(cb)`, `OnChapterChanged`, `OnMapLoaded`, `CurrentMapId` |
| `ModManager` | `DiscoverMods()`, `EnableMod(id)`, `DisableMod(id)`, `GetEnabledModMapFiles()`, `GetEnabledModScriptFiles()`, `GetEnabledModMiniGameFiles()`, `GetEnabledModDlcPackFiles()` |
| `MapLoaderFramework` (`TransitionCallback`) | `public Action<string, Action>` — assign to drive fade-out → load → fade-in from any external system |
| `AutoMapLoader` | Loads `defaultMapName` on Start |
| `MapLoadTrigger` | `TriggerLoad()` — call from UI or events |
| `MapDropdownLoader` | Populates a Dropdown with available maps |
| `LuaScriptTrigger` | `TriggerScript()` — executes a named Lua file |


## Optional Integrations

MapLoaderFramework does not define its own scripting symbols. Other modules activate their bridge to this package using `*_MLF` defines on their own side:

| Define | Module | Effect |
| ------ | ------ | ------ |
| `SAVEMANAGER_MLF` | SaveManager | Auto-save on chapter change |
| `EVENTMANAGER_MLF` | EventManager | Fire `map.loaded` / `chapter.changed` events |
| `AUDIOMANAGER_MLF` | AudioManager | Auto-play map music/ambient |
| `LOCALIZATIONMANAGER_MLF` | LocalizationManager | Register inline map names |
| `DIALOGUEMANAGER_MLF` | DialogueManager | Auto-play map intro dialogue |
| `INVENTORYMANAGER_MLF` | InventoryManager | Auto-grant map items on first visit |
| `DLCMANAGER_MLF` | DlcManager | Gate restricted maps behind DLC |
| `CUTSCENEMANAGER_MLF` | CutsceneManager | Fade transitions on map load |
| `MINIGAMEMANAGER_MLF` | MiniGameManager | Abort active mini-game on map load |
| `GAMEMANAGER_MLF` | GameManager | Load chapters via MapLoader |
| `TITLESCREEN_MLF` | TitleScreenManager | Load gameplay scene via MapLoader |
| `BOOTMANAGER_MLF` | BootStartupManager | Load TitleScreen via MapLoader |
| `STATEMANAGER_MLF` | StateManager | Push/pop `Loading` state on map load |
| `ANIMATIONMANAGER_MLF` | AnimationManager | Stop active animation on chapter change |
| `UIMANAGER_MLF` | UiManager | Show/hide loading panel on chapter change |
| `INPUTMANAGER_MLF` | InputManager | Block/unblock input during chapter loads |
| `CAMERAMANAGER_MLF` | CameraManager | Reset camera to default on chapter change |
| `AIMANAGER_MLF` | AiManager | Deregister all agents and reset alert level on chapter change |
| `ENEMYMANAGER_MLF` | EnemyManager | Abort active wave and clear all live instances on chapter change |

### Odin Inspector (`ODIN_INSPECTOR`)

Requires `ODIN_INSPECTOR` define and the Odin Inspector Asset Store package. MapLoaderManager inherits from `SerializedMonoBehaviour` for full Inspector serialization; runtime-display fields are marked `[ReadOnly]` in Play Mode.


## Documentation

- [Integration Guide](Documentation/Integration_Guide.md)
- [API Reference](Documentation/API_Reference.md)
- [Example Files](Documentation/Example_Files.md)
- [Extending the Framework](Documentation/Extending_the_Framework.md)
- [HowTo: Create And Use TMX](Documentation/HowTo_Create_And_Use_TMX_With_MapLoaderFramework.md)
- [FAQ](Documentation/FAQ.md)


## Repository

[https://github.com/RolandKaechele/MapLoaderFramework](https://github.com/RolandKaechele/MapLoaderFramework)

## License

MIT License
