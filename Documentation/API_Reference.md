# MapLoaderFramework API Reference

This document provides detailed information about the main classes, methods, extension points, and warp event handling in the MapLoaderFramework.


## Main Classes

- **MapLoaderFramework**: Main loader class for managing map loading, switching, and event notification.
- **MapLoaderManager**: Entry point for loading maps from UI or scripts.
- **MapLoader**: Handles standard map loading, instantiation, placement, and cleanup for direct map connections (edge-to-edge, e.g., doors, paths).
- **MapData**: Data structure representing a map (parsed from JSON), including both direct connections and warp events.
- **MapWarpLoader**: Handles all logic related to warp event maps (instantiation, placement, cleanup).
- **MapWarpConnection**: Data structure representing a warp event connection (teleport, warp, etc.) between maps.
- **ScriptManager**: Handles loading and executing Lua scripts (MoonSharp).

## Extension Points

- Use C# interfaces, events, or script hooks for custom logic.
- Extend map JSON with new fields or warp event types.
- Implement custom logic for warp event handling by extending or subscribing to MapWarpLoader.


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

## Map Change Notification API

The following API allows other components to receive updates when a map's raw JSON changes:

### Events

- `event Action<string, string> OnRawJsonUpdated` Triggered when a map's rawJson is loaded or updated. Parameters: `(mapId, rawJson)`.

### Methods

- `void SubscribeToRawJson(Action<string, string> callback)` Subscribe to receive rawJson updates for all maps.
- `void UnsubscribeFromRawJson(Action<string, string> callback)` Unsubscribe from rawJson updates.
- `string GetRawJson(string mapId)` Get the current rawJson for a loaded map by id.

**Usage Example:**

```csharp
mapLoaderFrameworkInstance.SubscribeToRawJson((mapId, rawJson) => {
                Debug.Log($"Map {mapId} updated. New rawJson: {rawJson}");
});
```
