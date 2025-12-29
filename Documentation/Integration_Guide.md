# MapLoaderFramework Integration Guide

This guide explains how to add the MapLoaderFramework to a new or existing Unity project, including setup, folder structure, and runtime integration.

## Folder Structure

> **Note:** For actual game content, create these folders in your project's `Assets` directory, not inside the `MapLoaderFramework` folder. This keeps framework code and game assets separate and makes your project easier to maintain and update.

- `Editor/`: (Optional) Custom Unity editor scripts for framework extensions.
- `Resources/`: Default assets, prefabs, or templates for framework use.
- `Assets/InternalMaps/`: Internal map files (bundled with build).
- `Assets/ExternalMaps/`: External map files (for testing user/content updates in the Editor only).
    - **In builds, user maps are loaded from** `Application.persistentDataPath/ExternalMaps` **for real modding support.**
- `Assets/MapLoaderFramework/Runtime/`: Internal framework C# scripts (do not modify).
- `Assets/Scripts/`: Your own or mod scripts (e.g., Lua, C# for your game logic).
    - **In builds, user/mod scripts can also be loaded from** `Application.persistentDataPath/Scripts` **if your game supports runtime scripting.**

### **Automatic Setup:**


When you install or update the MapLoaderFramework package, a postinstall script will automatically create the following folders under your project's `Assets` directory if they do not already exist:

- `Assets/Editor/`
- `Assets/Resources/`
- `Assets/InternalMaps/`
- `Assets/ExternalMaps/`
- `Assets/Scripts/`

> **Note** This is handled by the `postinstall.js` script referenced in the package's `package.json`.


#### Manually Creating Folders (Optional)

If you need to create the required folders manually (for example, if the postinstall script did not run automatically, such as when the package was copied from a zip file), you can run the script yourself:

1. Open a terminal and navigate to the `MapLoaderFramework` package directory:
    
    ```sh
       cd Assets/MapLoaderFramework
    ```
    
    . Run the postinstall script using Node.js:
    
    ```sh
    node postinstall.js
    ```

This will create all necessary folders under your project's `Assets` directory if they do not already exist.



## Integration Steps

1. Import the MapLoaderFramework into your Unity project.
2. Place your map JSON files in `Assets/InternalMaps/` (for built-in maps).
    - To test user/content updates in the Editor, place them in `Assets/ExternalMaps/`.
    - **In builds, user/content maps must be placed in** `Application.persistentDataPath/ExternalMaps` **(created at runtime if needed).**
3. Add Lua scripts to `Assets/Scripts/` for story events or custom logic.

4. In your starting Unity scene, create an empty GameObject and name it `MapLoaderFramework`.
    Add the `MapLoaderFramework` component to this GameObject. When you do this, all other required MapLoaderFramework runtime scripts (such as `MapLoaderManager`, `AutoMapLoader`, `MapDropdownLoader`, and `MapLoadTrigger`) will be automatically added to the same GameObject if not already present. 

    > Note: Some of these scripts (except `MapLoaderManager` and `AutoMapLoader`) are initially disabled and must be enabled in the Inspector if you want to use them.
5. (Recommended) To specify which map loads first, set the `defaultMapName` field in the `AutoMapLoader` component on the `MapLoaderFramework` GameObject. This will automatically load the specified map at startup.
6. Alternatively, you can load the initial map from your own script, UI, or event by calling `MapLoaderManager.LoadMap(mapName)`.
7. Use the loader scripts in `MapLoaderFramework/Runtime/` to load and switch maps as needed (e.g., via UI or events).
8. Add assets (prefabs, audio, etc.) to `Assets/Resources/` as needed.


## Subscribing to Map Change Notifications

To keep your systems (UI, logic, etc.) in sync with map changes, subscribe to the rawJson event:

```csharp
mapLoaderFrameworkInstance.SubscribeToRawJson((mapId, rawJson) => {
    Debug.Log($"Map {mapId} updated. New rawJson: {rawJson}");
});
```

Unsubscribe when no longer needed:

```csharp
mapLoaderFrameworkInstance.UnsubscribeFromRawJson(callback);
```

You can also get the current rawJson for any loaded map:

```csharp
string currentRawJson = mapLoaderFrameworkInstance.GetRawJson("myMapId");
```



## Runtime Integration Scripts

- **MapLoaderFramework.cs**: Core component that manages the framework and auto-adds all other runtime scripts to the GameObject. Provides the main entry point for map loading and connections.
    - **Map Connection Depth:** The maximum depth for loading connected maps is configurable via the `mapConnectionDepth` field in the Inspector. This controls how many levels of connected maps are loaded recursively when you call `LoadMapAndConnections`. The default is 2, but you can adjust this value in the Inspector to suit your project needs.
- **AutoMapLoader.cs**: Script to automatically load a default map at scene start. Assign the default map name (the map to load first) and MapLoaderManager in the Inspector. You can customize or extend this script for your own startup logic.
- **MapDropdownLoader.cs**: Lets you select and load maps from a Unity UI Dropdown. Assign a Dropdown and MapLoaderManager in the Inspector. **This script is initially disabled and must be enabled in the Inspector if you want to use it.**
- **MapLoadTrigger.cs**: Allows map loading to be triggered from UI, animation, or other scripts. Call `TriggerLoad()` to load the specified map. **This script is initially disabled and must be enabled in the Inspector if you want to use it.**


**Script file locations:**

- Framework C# scripts: `Assets/MapLoaderFramework/Runtime/`
- Your own or mod scripts: `Assets/Scripts/` (Editor) or `Application.persistentDataPath/Scripts` (Build/modding)

All framework scripts are in `MapLoaderFramework/Runtime/`. These scripts are automatically added to the GameObject when you attach the MapLoaderFramework component, so manual attachment is not required.

## See Also

For a list of Unity packages required by MapLoaderFramework, see [Required Unity Packages](Required_Unity_Packages.md).

## Example Project Folder Tree

```
Assets/
├── Editor/                        # Custom Unity editor scripts
├── Resources/                     # Default assets, prefabs, templates
├── InternalMaps/                  # Internal map JSON files (bundled with build)
│   └── example_internal_map.json
├── ExternalMaps/                  # For Editor testing only (user maps)
│   └── example_map.json
├── Scripts/                       # Your own or mod scripts (Lua, C#)
│   └── example_event.lua
├── MapLoaderFramework/
│   ├── Runtime/                   # Internal framework C# scripts (do not modify)
│   ├── Documentation/
│   └── ...
└── ... (other project folders)

# In builds, user maps/scripts are loaded from:
#   Application.persistentDataPath/ExternalMaps   (user maps/mods)
#   Application.persistentDataPath/Scripts        (user/mod scripts, if supported)
```

- Place your own map JSON files in `InternalMaps/` and `ExternalMaps/`.
- Place Lua scripts for events in `Scripts/`.
- The `MapLoaderFramework/` folder contains the framework code and documentation.


### Advanced Example: Realistic Project Folder Tree (with plugins and third-party assets)

A more complete example for larger Unity projects with plugins and third-party assets:

```
├───Editor
├───ExternalMaps                  # For Editor testing only (user maps)
├───InternalMaps                  # Internal map JSON files (bundled with build)
├───MapLoaderFramework
│   ├───Documentation
│   ├───Editor
│   ├───Examples
│   │   ├───Editor
│   │   ├───ExternalMaps
│   │   ├───InternalMaps
│   │   ├───Resources
│   │   │   ├───ExternalMaps
│   │   │   └───InternalMaps
│   │   └───Scripts
│   └───Runtime                  # Internal framework C# scripts (do not modify)
├───Plugins
│   └───MoonSharp
├───Resources
│   ├───ExternalMaps
│   └───InternalMaps
├───Scenes
├───Scripts                      # Your own or mod scripts (Lua, C#)
└───SerializedDictionary
    ├───.images
    ├───Editor
    │   ├───Assets
    │   └───Scripts
    │       ├───Data
    │       ├───KeyListGenerators
    │       │   └───Implementors
    │       ├───Search
    │       │   └───Matchers
    │       ├───Settings
    │       ├───States
    │       └───Utility
    ├───Runtime
    │   ├───LookupTables
    │   └───Scripts
    └───Samples~
        └───Usage

# In builds, user maps/scripts are loaded from:
#   Application.persistentDataPath/ExternalMaps   (user maps/mods)
#   Application.persistentDataPath/Scripts        (user/mod scripts, if supported)
```

- This structure shows how plugins (like MoonSharp), third-party assets (like SerializedDictionary), and framework subfolders can be organized in a real Unity project.
- Adjust as needed for your own dependencies and project scale.

### How the Folder Structure Appears in a Game Export

When you build and export your Unity game, only certain folders and files are included in the final build. Here's what you can expect:

- The `Assets/` folder itself is not present in the exported game. Instead, Unity compiles assets into platform-specific formats and bundles them into the build.
- Resources placed in `Resources/`, `InternalMaps/`, and other referenced folders are included in the build if they are used or referenced by your scenes or scripts.
- The `ExternalMaps/` folder in `Assets/` is not included by default. If you want to support user-generated content or modding, you must create this folder at runtime in `Application.persistentDataPath` and load maps from there.
- Editor-only folders (like `Editor/`) and documentation are never included in the build.
- Plugins (like MoonSharp) and third-party assets are included only if their scripts or assets are used in your game.

**Example: Windows Standalone Build Output**


```
MyGameBuild/
├── MyGame.exe
├── MyGame_Data/
│   ├── Managed/           # Compiled C# assemblies (including plugins)
│   ├── Resources/         # Resources loaded at runtime
│   ├── StreamingAssets/   # (If used) for files you want to keep as-is
│   └── ...
```

- To allow players to add or modify maps or scripts after export, instruct them to place their files in the following folders (created automatically by Unity as needed):

    - **Windows:**
        - `%APPDATA%/../LocalLow/<CompanyName>/<ProductName>/ExternalMaps` (maps/mods)
        - `%APPDATA%/../LocalLow/<CompanyName>/<ProductName>/Scripts` (scripts, if supported)
    - **macOS:**
        - `~/Library/Application Support/<CompanyName>/<ProductName>/ExternalMaps`
        - `~/Library/Application Support/<CompanyName>/<ProductName>/Scripts`
    - **Linux:**
        - `~/.config/unity3d/<CompanyName>/<ProductName>/ExternalMaps`
        - `~/.config/unity3d/<CompanyName>/<ProductName>/Scripts`

    These locations correspond to `Application.persistentDataPath` in Unity. The framework will automatically check these locations in builds for user content.
- Internal maps and resources are bundled and not directly visible as files.

For more details, see the Unity manual on [Build Pipeline](https://docs.unity3d.com/Manual/BuildPlayerPipeline.html) and [StreamingAssets](https://docs.unity3d.com/Manual/StreamingAssets.html).


