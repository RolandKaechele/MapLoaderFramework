# MapLoaderFramework API Reference

This document provides detailed information about the main classes, methods, and extension points in the MapLoaderFramework.

## Main Classes

- **MapLoaderFramework**: Main loader class for managing map loading and switching.
- **MapData**: Data structure representing a map (parsed from JSON).
- **ScriptManager**: Handles loading and executing Lua scripts (MoonSharp).

## Extension Points

- Use C# interfaces, events, or script hooks for custom logic.


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
