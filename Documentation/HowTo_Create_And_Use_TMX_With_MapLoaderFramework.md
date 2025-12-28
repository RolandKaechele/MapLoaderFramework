# Creating TMX Files and Using Them with MapLoaderFramework

## 1. What is a TMX File?

A TMX file is a map file format created by the [Tiled Map Editor](https://www.mapeditor.org/). It is commonly used for 2D tile-based games and contains information about tilesets, layers, objects, and properties.

## 2. How to Create a TMX File

### Step 1: Install Tiled Map Editor

- Download and install Tiled from [https://www.mapeditor.org/](https://www.mapeditor.org/).

### Step 2: Create a New Map

- Open Tiled.
- Go to **File > New**.
- Set map orientation (usually Orthogonal), tile size, and map size.
- Click **OK**.

### Step 3: Add Tilesets

- Go to **Map > New Tileset**.
- Select your tileset image (PNG, etc.).
- Set tile size and spacing as needed.
- Click **OK**.

### Step 4: Design Your Map

- Use the tile, object, and layer tools to design your map.
- Add properties or objects as needed for your game logic.

### Step 5: Save the Map

- Go to **File > Save As**.
- Save the file with a `.tmx` extension.

## 3. Using TMX Files with MapLoaderFramework

### Step 1: Import TMX Files into Unity

- Place your `.tmx` files and any referenced tileset images into your Unity project, typically under `Assets/Resources/InternalMaps` or `Assets/Resources/ExternalMaps`.

### Step 2: Import with SuperTiled2Unity

- Install the [SuperTiled2Unity package](https://seanba.com/supertiled2unity.html) in your Unity project.
- SuperTiled2Unity will automatically import `.tmx` files as prefabs.
- Ensure your TMX files and tileset images are in a folder scanned by SuperTiled2Unity (e.g., `Assets/Resources/InternalMaps`).

### Step 3: Reference TMX Prefabs in MapLoaderFramework

- The MapLoaderFramework expects map prefabs to be named after the map's layout property (usually the TMX filename without extension).
- When you call `LoadMapAndConnections("MapName")`, the framework will look for a prefab named `MapName` in `Resources/InternalMaps` or `Resources/ExternalMaps`.

### Step 4: Connect Maps

- In your map's JSON data, use the `connections` array to specify connected map IDs.
- The framework will load the current map and its direct connections, instantiating the corresponding prefabs.

## 4. Tips

- Keep TMX filenames and layout names consistent.
- Use the Tiled editor's custom properties to add metadata for your game.
- Test map loading in Unity to ensure prefabs are generated and found by the framework.

## 5. Troubleshooting

- If a prefab is not found, check that the TMX file is imported and the prefab exists in the correct Resources folder.
- Ensure all referenced tileset images are present in the project.
- Use Unity's console logs for debug information from MapLoaderFramework.

For more details, see the MapLoaderFramework documentation and SuperTiled2Unity guides.
