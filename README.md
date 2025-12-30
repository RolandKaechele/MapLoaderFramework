# MapLoaderFramework Documentation

MapLoaderFramework is a modular Unity framework for loading, managing, and integrating 2D map data (TMX maps support) extensible workflows and easy integration into your game or application projects.

![Overview](Documentation/overview.png)

## Installation as a Unity Plugin

You can install the MapLoaderFramework as a Unity plugin in two ways:


**A. Import as a Unity Package (.unitypackage):**

1. In Unity, go to `Assets > Import Package > Custom Package...`.
2. Select the `MapLoaderFramework.unitypackage` file (exported from the framework project).
3. Import all required files and folders (typically everything under `MapLoaderFramework/`).
4. Follow the Quick Start and Integration Steps below.


**B. Clone from Git Repository (UPM or Assets):**

1. Open your Unity project folder.

2. Recommended: **UPM (Unity Package Manager) via Git**
   - This is the preferred method for most users. It allows easy updates, dependency management, and keeps your project clean.
   - Steps:
     1. Open `Packages/manifest.json` in your project.
     2. Add the following line to the `dependencies` section:

        ```json
        "com.rolandkaechele.maploaderframework": "https://github.com/RolandKaechele/MapLoaderFramework.git"
        ```

     3. Save the file and Unity will fetch the package from GitHub automatically.

   - **Why use UPM via Git?**
     - Easy to update: Just change the git URL or commit hash.
     - Keeps your Assets folder uncluttered.
     - Supports semantic versioning and dependency resolution.
     - Can be used with private forks or branches.

3. Alternative: **Direct Clone to Assets**
   - Use this only if you need to modify the framework directly or UPM is not suitable for your workflow.
   - Steps:
     1. Clone the repository directly into your `Assets/MapLoaderFramework` folder:

        ```sh
        git clone https://github.com/RolandKaechele/MapLoaderFramework.git Assets/MapLoaderFramework
        ```

     2. Unity will automatically import the scripts and assets.

4. Follow the Quick Start and Integration Steps below.


**C. Manual Copy:**

1. Copy the entire `MapLoaderFramework/` folder (with all subfolders) into your project's `Assets/` directory (not into `Packages`).
2. Unity will automatically import the scripts and assets.
3. Follow the Quick Start and Integration Steps below.


### Automatic Folder and Template Setup

After installation, the `postinstall.js` script will:

- Ensure all required folders (Editor, Resources, InternalMaps, ExternalMaps, Scripts) exist under your project's `Assets` directory.
- Optionally prompt you to copy template/example files from the `MapLoaderFramework/Examples` folder to your `Assets` folders. If a file already exists, you will be asked whether to overwrite it.

This makes it easy to get started with example content or a clean folder structure.


## About This Documentation

This folder contains usage guides, API documentation, and integration steps for the MapLoaderFramework.

### Contents

 - **Getting Started**: See [Integration_Guide.md](Documentation/Integration_Guide.md) for installation and quick start.
 - **Folder Structure**: See [Integration_Guide.md](Documentation/Integration_Guide.md).
 - **Example Files & Templates**: See the `Examples` folder in the package for template/example files you can copy into your project. For details, see [Example_Files.md](Documentation/Example_Files.md).
 - **API Reference**: See [API_Reference.md](Documentation/API_Reference.md) for main classes, map connection types, and data structures.
 - **Integration Steps**: See [Integration_Guide.md](Documentation/Integration_Guide.md).
 - **Extending the Framework**: See [Extending_the_Framework.md](Documentation/Extending_the_Framework.md) for custom logic, events, and scripting.
 - **HowTo: Create And Use TMX With MapLoaderFramework**: See [HowTo_Create_And_Use_TMX_With_MapLoaderFramework.md](Documentation/HowTo_Create_And_Use_TMX_With_MapLoaderFramework.md)
 - **FAQ**: See [FAQ.md](Documentation/FAQ.md) for frequently asked questions and answers about MapLoaderFramework.

## Map Connections and Extensibility

MapLoaderFramework supports two types of map connections:

- **Direct map connections** (edge-to-edge, e.g., doors, paths) managed by MapLoader and defined in each map's JSON as `MapConnection` objects.
- **Warp event connections** (teleports, special transitions) managed by MapWarpLoader and defined as `MapWarpConnection` objects.

The maximum depth for loading directly connected maps is controlled by the `mapConnectionDepth` field in the MapLoaderFramework Inspector (default: 2).

You can extend the framework by subscribing to map change notifications, adding new fields or warp event types to map JSON, and using Lua or C# scripts for custom logic. See the Integration Guide and API Reference for details.

## Main Scripts and Data Structures

- All core framework scripts are in `Assets/MapLoaderFramework/Runtime/`.
- Your own or mod scripts go in `Assets/Scripts/` (Editor) or `Application.persistentDataPath/Scripts` (Build/modding).
- See the API Reference for a full list and descriptions of all main runtime scripts and data structures.

## Map Change Notification System

MapLoaderFramework provides an event system for notifying other components when a map's raw JSON changes (e.g., after loading or merging internal/external maps). Components can subscribe to receive updates for any map:

```csharp
// Subscribe to rawJson updates
mapLoaderFrameworkInstance.SubscribeToRawJson((mapId, rawJson) => {
       Debug.Log($"Map {mapId} updated. New rawJson: {rawJson}");
});

// Unsubscribe when no longer needed
mapLoaderFrameworkInstance.UnsubscribeFromRawJson(callback);

// Get current rawJson for a map
string currentRawJson = mapLoaderFrameworkInstance.GetRawJson("myMapId");
```

This ensures your systems (UI, logic, etc.) are always informed of map changes.


## Support & Repository

Project repository: [https://github.com/RolandKaechele/MapLoaderFramework](https://github.com/RolandKaechele/MapLoaderFramework)


## License

This framework is released under the MIT License.
