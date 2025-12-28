# MapLoaderFramework Extending the Framework

Guidelines for adding custom logic, assets, or map formats:

- Implement C# interfaces or events for custom map logic.
- Add new asset types to Resources/ as needed.
- Extend map JSON structure for new features.
- Use Lua scripts for advanced story or gameplay events.

## Enhancing Map JSON Structure with ExternalMaps

You can enhance or override map data after release by placing updated JSON files in the `Assets/ExternalMaps` folder. The framework will automatically merge these external files with the internal map data at runtime, or fully override the internal map if you specify the `"override": true` flag in the external JSON.

**How it works:**

- Internal map JSON is loaded first (from `Assets/InternalMaps`).
- If an external map JSON exists (in `Assets/ExternalMaps`):
       - If it contains `"override": true`, the internal map is completely replaced by the external map.
       - Otherwise, its fields will override or extend the internal map data (merge mode).
- Simple fields (like `name`, `music`, etc.) are replaced if present in the external file.
- List fields (like `items`, `sfx`, `connections`) are appended, allowing you to add new content.

**Example: Internal vs. External JSON (Merge Mode)**

*Assets/InternalMaps/forest.json:*

```json
{
       "id": "forest",
       "name": "Forest",
       "music": "forest_theme.mp3",
       "items": [ { "id": "potion", "name": "Potion", "quantity": 1 } ]
}
```

*Assets/ExternalMaps/forest.json:* (for Editor testing)
*Application.persistentDataPath/ExternalMaps/forest.json:* (for builds/modding)

```json
{
       "id": "forest",
       "name": "Enchanted Forest",
       "items": [ { "id": "elixir", "name": "Elixir", "quantity": 2 } ]
}
```

**Result at runtime:**

```
id = "forest" // from internal or external (should match)
name = "Enchanted Forest" // overridden
music = "forest_theme.mp3" // from internal
items = [ { "id": "potion", ... }, { "id": "elixir", ... } ] // merged
```

**Example: Full Override**

*Assets/InternalMaps/overwrite_test.json:*

```json
{
       "id": "MAP_OVERWRITE_TEST",
       "name": "Original Overwrite Test",
       "music": "MUS_ORIGINAL_OVERWRITE"
       // ...
}
```

*Assets/ExternalMaps/overwrite_test.json:* (for Editor testing)
*Application.persistentDataPath/ExternalMaps/overwrite_test.json:* (for builds/modding)

```json
{
       "override": true,
       "id": "MAP_OVERWRITE_TEST",
       "name": "Overwritten Overwrite Test",
       "music": "MUS_OVERWRITTEN_OVERWRITE"
       // ...
}
```

**Result at runtime:**

```
name = "Overwritten Overwrite Test" // fully replaced
music = "MUS_OVERWRITTEN_OVERWRITE" // fully replaced
// ...
```

This allows you to update, expand, or fully replace your maps without rebuilding the game. See `MapLoaderFramework.cs` for merge and override logic details.


## Listening for Map Changes (Event-Based Extension)

You can extend the framework by subscribing to map change notifications. This allows your custom systems to react whenever a map's raw JSON is loaded or updated:

```csharp
mapLoaderFrameworkInstance.SubscribeToRawJson((mapId, rawJson) => {
       // React to map changes here
});
```

Unsubscribe when no longer needed:

```csharp
mapLoaderFrameworkInstance.UnsubscribeFromRawJson(callback);
```

See the main README and API Reference for extension points and best practices.
