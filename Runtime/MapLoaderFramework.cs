using UnityEngine;
using System;
using MapLoaderFramework.Runtime;
using System.Linq;
using System.Diagnostics;

namespace MapLoaderFramework.Runtime
{

    /// <summary>
    /// Core framework component for MapLoaderFramework.
    ///
    /// When added to a GameObject, this component will automatically attach all other MapLoaderFramework runtime scripts
    /// (such as MapLoaderManager, AutoMapLoader, MapDropdownLoader, MapLoadTrigger) to the same GameObject if they are not already present.
    ///
    /// It also provides the main entry point for loading maps and their connections via <see cref="LoadMapAndConnections"/>.
    /// </summary>
	[AddComponentMenu("MapLoaderFramework/MapLoader Framework")]
    [DisallowMultipleComponent]
    public class MapLoaderFramework : MonoBehaviour
    {
        // --- Static dictionary for absolute map positions (cleared on PreloadAllMaps) ---
        private static System.Collections.Generic.Dictionary<string, Vector3> _mapAbsolutePositions;

        // Track instantiated map prefabs by map id
        private System.Collections.Generic.Dictionary<string, GameObject> instantiatedPrefabs = new System.Collections.Generic.Dictionary<string, GameObject>();
        // Registry for loaded maps: id -> MapRegistryEntry
        [Serializable]
        public class MapRegistryEntry
        {
            public string id;
            public string name;
            public string filePath;
            public bool prefabInstantiated = false;
            public bool isLoaded = false;
        }

        // Internal map registry
        private System.Collections.Generic.Dictionary<string, MapRegistryEntry> mapRegistry = new System.Collections.Generic.Dictionary<string, MapRegistryEntry>();

        // Inspector-visible list of loaded maps (read-only)
        [SerializeField] private System.Collections.Generic.List<MapData> loadedMapsInspector = new System.Collections.Generic.List<MapData>();
        // Inspector-visible list of found Lua scripts (read-only)
        [SerializeField] private System.Collections.Generic.List<string> foundLuaScriptsInspector = new System.Collections.Generic.List<string>();
        public System.Collections.Generic.IReadOnlyList<string> FoundLuaScripts => foundLuaScriptsInspector;


        /// <summary>
        /// Preloads all map JSON files from InternalMaps and ExternalMaps, saving their id and JSON content in the registry and inspector list.
        /// </summary>
        public void PreloadAllMaps()
        {
            loadedMapsInspector.Clear();
            mapRegistry.Clear();
            // Clear absolute position tracking on preload
            _mapAbsolutePositions = new System.Collections.Generic.Dictionary<string, Vector3>();
            string projectPath = Application.dataPath;
            string internalDir = System.IO.Path.Combine(projectPath, "InternalMaps");
            // Use persistentDataPath/ExternalMaps in builds, Assets/ExternalMaps in editor
            string externalDir = null;
            #if UNITY_EDITOR
            externalDir = System.IO.Path.Combine(projectPath, "ExternalMaps");
            #else
            externalDir = System.IO.Path.Combine(Application.persistentDataPath, "ExternalMaps");
            #endif
            string[] mapDirs = { internalDir, externalDir };
            UnityEngine.Debug.Log($"[MapLoaderFramework] Project path: {projectPath}");
            UnityEngine.Debug.Log($"[MapLoaderFramework] Internal maps directory: {internalDir}");
            UnityEngine.Debug.Log($"[MapLoaderFramework] External maps directory: {externalDir}");
            foreach (var dir in mapDirs)
            {
                UnityEngine.Debug.Log($"[MapLoaderFramework] Checking directory: {dir}");
                if (!System.IO.Directory.Exists(dir)) {
                    UnityEngine.Debug.Log($"[MapLoaderFramework] Directory does not exist: {dir}");
                    continue;
                }
                var files = System.IO.Directory.GetFiles(dir, "*.json", System.IO.SearchOption.AllDirectories);
                UnityEngine.Debug.Log($"[MapLoaderFramework] Found {files.Length} map file(s) in {dir}");
                foreach (var file in files)
                {
                    UnityEngine.Debug.Log($"[MapLoaderFramework] Preloading map from {file}");
                    try
                    {
                        string json = System.IO.File.ReadAllText(file);
                        var mapData = JsonUtility.FromJson<MapData>(json);
                        if (mapData != null && !string.IsNullOrEmpty(mapData.id))
                        {
                            mapData.rawJson = json;
                            UnityEngine.Debug.Log($"[MapLoaderFramework] Found map id: {mapData.id}, name: {mapData.name}");
                            var entry = new MapRegistryEntry {
                                id = mapData.id,
                                name = mapData.name,
                                filePath = file
                            };
                            mapRegistry[mapData.id] = entry;
                            loadedMapsInspector.RemoveAll(m => m.id == mapData.id);
                            loadedMapsInspector.Add(mapData);
                        }
                        else
                        {
                            UnityEngine.Debug.LogWarning($"[MapLoaderFramework] Could not parse map data or missing id in file: {file}");
                        }
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError($"[MapLoaderFramework] Failed to preload map from {file}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Called when the component is first added or reset in the Inspector.
        /// Ensures all MapLoaderFramework runtime scripts are attached to this GameObject.
        /// </summary>
        void Reset()
        {
            AddAllMapLoaderComponents();
        }

        /// <summary>
        /// Ensures all MapLoaderFramework runtime scripts are attached in edit mode as well as at runtime.
        /// </summary>
        void Awake()
        {
            #if UNITY_EDITOR
            // In edit mode, ensure all components are present
            if (!Application.isPlaying)
            {
                AddAllMapLoaderComponents();
                return;
            }
            #endif
            // In play mode, preload all maps at startup
            PreloadAllMaps();
            // Load all external Lua scripts into memory and update inspector list
            foundLuaScriptsInspector.Clear();
            LuaScriptLoader.LoadAndRunAllScripts(foundLuaScriptsInspector);
            // Remove any Lua scripts from memory that are not needed for the active and connected maps
            LuaScriptLoader.RemoveUnusedScripts(foundLuaScriptsInspector);
        }

        /// <summary>
        /// Finds and attaches all MapLoaderFramework runtime MonoBehaviour scripts to this GameObject, except itself.
        /// This allows for easy setup by simply adding MapLoaderFramework.
        /// </summary>
        private void AddAllMapLoaderComponents()
        {
            var thisType = this.GetType();
            var assembly = thisType.Assembly;
            var runtimeNamespace = "MapLoaderFramework.Runtime";
            // Get all runtime types except this
            var allTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(MonoBehaviour)) && t.Namespace == runtimeNamespace && t != thisType)
                .ToList();

            // Add MapLoaderManager first (enabled)
            var managerType = allTypes.FirstOrDefault(t => t.Name == "MapLoaderManager");
            if (managerType != null && this.GetComponent(managerType) == null)
            {
                this.gameObject.AddComponent(managerType);
            }

            // Add AutoMapLoader second (enabled)
            var autoLoaderType = allTypes.FirstOrDefault(t => t.Name == "AutoMapLoader");
            if (autoLoaderType != null && this.GetComponent(autoLoaderType) == null)
            {
                this.gameObject.AddComponent(autoLoaderType);
            }

            // Add the rest (excluding MapLoaderManager and AutoMapLoader), disabled by default
            foreach (var type in allTypes)
            {
                if (type == managerType || type == autoLoaderType) continue;
                if (this.GetComponent(type) == null)
                {
                    var comp = this.gameObject.AddComponent(type) as MonoBehaviour;
                    if (comp != null) comp.enabled = false;
                }
            }
        }

        /// <summary>
        /// Loads a map and its connections by name.
        /// Loads a map and all its connected maps by name, recursively.
        ///
        /// This method searches for a map JSON file with the given name in both InternalMaps (StreamingAssets)
        /// and ExternalMaps (persistentDataPath). If found, it loads and logs the contents.
        /// Extend this method to parse the JSON and load any connected maps as needed.
        /// This method searches for a map JSON file with the given name in both InternalMaps (Assets/InternalMaps)
        /// and ExternalMaps (Assets/ExternalMaps). If found, it loads and parses the JSON, logs the contents, and
        /// recursively loads any maps listed in the "connections" array of the JSON. Each connected map is loaded only if
        /// its name is not empty and not the same as the current map.
        ///
        /// Called by MapLoaderManager.
        /// </summary>
        /// <param name="mapName">The name of the map to load (without extension).</param>
        public void LoadMapAndConnections(string mapName)
        {
            LoadMapAndConnections(mapName, 0, 1);
        }

        // Internal recursive version with depth control
        private void LoadMapAndConnections(string mapName, int currentDepth, int maxDepth)
        {
            // Prevent duplicate loading by id
            string parsedId = null;

            // Search for the map file in both internal and external directories
            string projectPath = Application.dataPath;
            string internalDir = System.IO.Path.Combine(projectPath, "InternalMaps");
            string externalDir = null;
            #if UNITY_EDITOR
            externalDir = System.IO.Path.Combine(projectPath, "ExternalMaps");
            #else
            externalDir = System.IO.Path.Combine(Application.persistentDataPath, "ExternalMaps");
            #endif
            string mapPathInternal = System.IO.Path.Combine(internalDir, mapName + ".json");
            string mapPathExternal = System.IO.Path.Combine(externalDir, mapName + ".json");

            string chosenJson = null;
            string chosenLayout = null;
            string chosenSource = null;


            // Try external JSON first
            MapData extData = null;
            MapData intData = null;
            string extJson = null;
            string intJson = null;
            bool overwrite = false;

            if (System.IO.File.Exists(mapPathExternal))
            {
                extJson = System.IO.File.ReadAllText(mapPathExternal);
                chosenJson = extJson;
                chosenSource = "External";
                extData = JsonUtility.FromJson<MapData>(extJson);
                if (extData != null && !string.IsNullOrEmpty(extData.layout))
                {
                    chosenLayout = extData.layout;
                }
                // Check for overwrite attribute in raw JSON
                if (!string.IsNullOrEmpty(extJson) && extJson.Contains("\"overwrite\""))
                {
                    try
                    {
                        var jObj = MiniJSON.Json.Deserialize(extJson) as System.Collections.Generic.Dictionary<string, object>;
                        if (jObj != null && jObj.ContainsKey("overwrite"))
                        {
                            bool.TryParse(jObj["overwrite"].ToString(), out overwrite);
                        }
                    }
                    catch { /* ignore parse errors */ }
                }
            }

            if (System.IO.File.Exists(mapPathInternal))
            {
                intJson = System.IO.File.ReadAllText(mapPathInternal);
                intData = JsonUtility.FromJson<MapData>(intJson);
                if (intData != null && !string.IsNullOrEmpty(intData.layout))
                {
                    if (string.IsNullOrEmpty(chosenLayout))
                    {
                        chosenLayout = intData.layout;
                        if (chosenJson == null)
                        {
                            chosenJson = intJson;
                            chosenSource = "Internal";
                        }
                    }
                }
            }

            // Merge internal into external if needed
            if (extData != null && intData != null && !overwrite)
            {
                // For each field in intData, if extData's field is null or default, copy from intData
                var extType = typeof(MapData);
                foreach (var field in extType.GetFields())
                {
                    var extVal = field.GetValue(extData);
                    var intVal = field.GetValue(intData);
                    if ((extVal == null || (extVal is string s && string.IsNullOrEmpty(s))) && intVal != null)
                    {
                        field.SetValue(extData, intVal);
                    }
                }

                // Merge raw JSONs for rawJson property
                System.Collections.Generic.Dictionary<string, object> extDict = null;
                System.Collections.Generic.Dictionary<string, object> intDict = null;
                try {
                    extDict = MiniJSON.Json.Deserialize(extJson) as System.Collections.Generic.Dictionary<string, object>;
                } catch { }
                try {
                    intDict = MiniJSON.Json.Deserialize(intJson) as System.Collections.Generic.Dictionary<string, object>;
                } catch { }
                if (extDict != null && intDict != null)
                {
                    foreach (var kvp in intDict)
                    {
                        if (!extDict.ContainsKey(kvp.Key) || extDict[kvp.Key] == null || (extDict[kvp.Key] is string s && string.IsNullOrEmpty(s)))
                        {
                            extDict[kvp.Key] = kvp.Value;
                        }
                    }
                    // Use merged extData as chosenJson
                    chosenJson = JsonUtility.ToJson(extData);
                    // Set merged rawJson
                    extData.rawJson = MiniJSON.Json.Serialize(extDict);
                }
                else
                {
                    // Fallback: just use extJson
                    chosenJson = JsonUtility.ToJson(extData);
                    extData.rawJson = extJson;
                }
                chosenSource = "External+Internal";
            }

            if (chosenJson != null)
            {
                var mapData = JsonUtility.FromJson<MapData>(chosenJson);
                if (mapData == null || string.IsNullOrEmpty(mapData.id) || string.IsNullOrEmpty(mapData.layout))
                {
                    UnityEngine.Debug.LogError($"[MapLoaderFramework] Could not extract valid map data from JSON for '{mapName}'.");
                    return;
                }
                // If merged, use extData.rawJson if available
                if (extData != null && intData != null && !overwrite && extData.rawJson != null)
                {
                    mapData.rawJson = extData.rawJson;
                }
                else
                {
                    mapData.rawJson = chosenJson;
                }
                parsedId = mapData.id;

                bool alreadyInRegistry = mapRegistry.ContainsKey(parsedId);
                if (alreadyInRegistry && mapRegistry[parsedId].isLoaded)
                {
                    UnityEngine.Debug.Log($"[MapLoaderFramework] Map id '{parsedId}' already fully loaded. Skipping to prevent recursion.");
                    return;
                }
                if (!alreadyInRegistry)
                {
                    // Register map info and update inspector
                    mapRegistry[mapData.id] = new MapRegistryEntry {
                        id = mapData.id,
                        name = mapData.name,
                        filePath = chosenSource == "External" ? mapPathExternal : mapPathInternal,
                        prefabInstantiated = false,
                        isLoaded = false
                    };
                    loadedMapsInspector.RemoveAll(m => m.id == mapData.id);
                    loadedMapsInspector.Add(mapData);

                    // Notify subscribers
                    if (OnRawJsonUpdated != null)
                    {
                        OnRawJsonUpdated(mapData.id, mapData.rawJson);
                    }
                }
                // Mark as loading to prevent recursion
                mapRegistry[mapData.id].isLoaded = true;
                // Instantiate the Tiled map prefab if not already instantiated
                if (!mapRegistry[mapData.id].prefabInstantiated)
                {
                    InstantiateTiledMap(mapData.layout);
                    mapRegistry[mapData.id].prefabInstantiated = true;
                }

                // Parse JSON and load map connections as needed
                // --- Enhanced placement logic with absolute position tracking and debug logs ---
                // Static dictionary to track absolute positions of each map (by id)
                if (_mapAbsolutePositions == null)
                    _mapAbsolutePositions = new System.Collections.Generic.Dictionary<string, Vector3>();

                if (currentDepth < maxDepth)
                {
                    try
                    {
                        UnityEngine.Debug.Log($"[MapLoaderFramework] Map '{mapName}' has {mapData.connections?.Count ?? 0} connection(s).");

                        if (mapData.connections != null)
                        {

                            // Get this map's absolute position (default to zero if not set)
                            Vector3 basePosition = Vector3.zero;
                            if (_mapAbsolutePositions.TryGetValue(mapData.id, out var absPos))
                                basePosition = absPos;
                            else if (instantiatedPrefabs.TryGetValue(mapData.id, out var baseInstance) && baseInstance != null)
                                basePosition = baseInstance.transform.position;

                            // Ensure the prefab is at the correct position
                            if (instantiatedPrefabs.TryGetValue(mapData.id, out var thisInstance) && thisInstance != null)
                            {
                                thisInstance.transform.position = basePosition;
                                UnityEngine.Debug.Log($"[MapLoaderFramework] Placing map '{mapData.id}' at {basePosition}");
                            }
                            _mapAbsolutePositions[mapData.id] = basePosition;

                            // 1st pass: Recursively load/initiate all connected maps
                            foreach (var conn in mapData.connections)
                            {
                                if (conn == null || string.IsNullOrEmpty(conn.mapId) || string.Equals(conn.mapId, parsedId, StringComparison.OrdinalIgnoreCase))
                                    continue;

                                // If the connected map is already placed, skip
                                if (_mapAbsolutePositions.ContainsKey(conn.mapId))
                                    continue;

                                // Find the filename for this id in the registry
                                if (mapRegistry.TryGetValue(conn.mapId, out var info) && !string.IsNullOrEmpty(info.filePath))
                                {
                                    string fileName = System.IO.Path.GetFileNameWithoutExtension(info.filePath);
                                    UnityEngine.Debug.Log($"[MapLoaderFramework] Loading connected map by id: {conn.mapId} (filename: {fileName}), direction: {conn.direction}");
                                    LoadMapAndConnections(fileName, currentDepth + 1, maxDepth);
                                }
                            }

                            // --- Get actual map size from prefab (current map) ---
                            float mapWidth = 1f;
                            float mapHeight = 1f;
                            GameObject parentInstance = null;
                            if (!instantiatedPrefabs.TryGetValue(mapData.id, out parentInstance) || parentInstance == null)
                            {
                                instantiatedPrefabs.TryGetValue(mapData.layout, out parentInstance);
                            }
                            if (parentInstance != null)
                            {
                                UnityEngine.Debug.Log($"[MapLoaderFramework] Getting size for map '{mapData.id}' from instantiated prefab.");
                                var renderers = parentInstance.GetComponentsInChildren<Renderer>();
                                if (renderers != null && renderers.Length > 0)
                                {
                                    var bounds = renderers[0].bounds;
                                    foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                                    mapWidth = bounds.size.x;
                                    mapHeight = bounds.size.y;
                                    UnityEngine.Debug.Log($"[MapLoaderFramework] Detected map size for '{mapData.id}': width={mapWidth}, height={mapHeight}");
                                }
                                else
                                {
                                    UnityEngine.Debug.LogWarning($"[MapLoaderFramework] No Renderer found for map '{mapData.id}', using default size.");
                                }
                            }

                            // 2nd pass: Calculate and set positions for all connected maps
                            foreach (var conn in mapData.connections)
                            {
                                if (conn == null || string.IsNullOrEmpty(conn.mapId) || string.Equals(conn.mapId, parsedId, StringComparison.OrdinalIgnoreCase))
                                    continue;

                                // If the connected map is already placed, skip repositioning
                                if (_mapAbsolutePositions.ContainsKey(conn.mapId))
                                {
                                    UnityEngine.Debug.Log($"[MapLoaderFramework] Map '{conn.mapId}' already placed at {_mapAbsolutePositions[conn.mapId]}, skipping reposition.");
                                    continue;
                                }

                                // Find the filename for this id in the registry
                                if (mapRegistry.TryGetValue(conn.mapId, out var info) && !string.IsNullOrEmpty(info.filePath))
                                {
                                    string fileName = System.IO.Path.GetFileNameWithoutExtension(info.filePath);

                                    // After recursion, calculate and set the absolute position for the connected map
                                    // (Now prefab and metadata should be available)
                                    float connMapWidth = 1f;
                                    float connMapHeight = 1f;
                                    GameObject connInstance = null;
                                    if (!instantiatedPrefabs.TryGetValue(conn.mapId, out connInstance) || connInstance == null)
                                    {
                                        // Try by layout name if available in registry
                                        if (mapRegistry.TryGetValue(conn.mapId, out var connReg) && !string.IsNullOrEmpty(connReg.name))
                                        {
                                            instantiatedPrefabs.TryGetValue(connReg.name, out connInstance);
                                        }
                                    }
                                    // Try by layout (from info) if still not found
                                    if (connInstance == null && !string.IsNullOrEmpty(info.name))
                                    {
                                        instantiatedPrefabs.TryGetValue(info.name, out connInstance);
                                    }
                                    // Try by fileName (layout) if still not found
                                    if (connInstance == null && !string.IsNullOrEmpty(fileName))
                                    {
                                        instantiatedPrefabs.TryGetValue(fileName, out connInstance);
                                    }
                                    if (connInstance != null)
                                    {
                                        var renderers = connInstance.GetComponentsInChildren<Renderer>();
                                        if (renderers != null && renderers.Length > 0)
                                        {
                                            var bounds = renderers[0].bounds;
                                            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                                            connMapWidth = bounds.size.x;
                                            connMapHeight = bounds.size.y;
                                            UnityEngine.Debug.Log($"[MapLoaderFramework] (post) Detected map size for connected '{conn.mapId}': width={connMapWidth}, height={connMapHeight}");
                                        }
                                        else
                                        {
                                            UnityEngine.Debug.LogWarning($"[MapLoaderFramework] (post) No Renderer found for connected map '{conn.mapId}', using default size.");
                                        }
                                    }
                                    else
                                    {
                                        UnityEngine.Debug.LogError($"[MapLoaderFramework] (post) Connected map prefab for '{conn.mapId}' not found, cannot determine size.");
                                    }

                                    // Calculate offset for true edge-to-edge placement (centered origins)
                                    float xOffset = 0f;
                                    float yOffset = 0f;
                                    // Empirically determined: using connMapWidth/10 and connMapHeight/10 gives correct edge-to-edge placement for these assets
                                    // If asset bounds or origins change, adjust these divisors accordingly
                                    switch ((conn.direction ?? "").ToLowerInvariant())
                                    {
                                        case "up":
                                            yOffset = (mapHeight / 2f) + (connMapHeight / 10f);
                                            break;
                                        case "down":
                                            yOffset = -((mapHeight / 2f) + (connMapHeight / 10f));
                                            break;
                                        case "left":
                                            xOffset = -((mapWidth / 2f) + (connMapWidth / 10f));
                                            break;
                                        case "right":
                                            xOffset = (mapWidth / 2f) + (connMapWidth / 10f);
                                            break;
                                        default:
                                            xOffset = (mapWidth / 2f) + (connMapWidth / 10f); // Default to right
                                            break;
                                    }
                                    Vector3 offset = new Vector3(xOffset, yOffset, 0);
                                    Vector3 connAbsPos = basePosition + offset;
                                    _mapAbsolutePositions[conn.mapId] = connAbsPos;
                                    if (connInstance != null)
                                    {
                                        connInstance.transform.position = connAbsPos;
                                        UnityEngine.Debug.Log($"[MapLoaderFramework] Placed connected map '{conn.mapId}' at {connAbsPos}");
                                    }
                                    else
                                    {
                                        var prefabDetails = string.Join(", ", instantiatedPrefabs.Select(kvp => {
                                            if (kvp.Value == null) return $"{kvp.Key} (instance: null)";
                                            string path = kvp.Value.transform.parent == null ? kvp.Value.name : kvp.Value.transform.parent.name + "/" + kvp.Value.name;
                                            return $"{kvp.Key} (instance: {kvp.Value.name}, type: {kvp.Value.GetType().Name}, active: {kvp.Value.activeSelf}, path: {path})";
                                        }));
                                        UnityEngine.Debug.LogError($"[MapLoaderFramework] Connected map '{conn.mapId}' prefab instance not found after loading. Instantiated prefabs: [{prefabDetails}]");
                                    }
                                }
                                else
                                {
                                    UnityEngine.Debug.LogError($"[MapLoaderFramework] Connection id '{conn.mapId}' not found in map registry. Cannot resolve filename.");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError($"[MapLoaderFramework] Exception parsing/loading map '{mapName}': {ex.Message}");
                    }
                }
            }
            else
            {
                bool hasExternal = System.IO.File.Exists(mapPathExternal);
                bool hasInternal = System.IO.File.Exists(mapPathInternal);
                string msg = $"[MapLoaderFramework] Map file for '{mapName}' not found or missing layout. ";
                if (!hasExternal && !hasInternal)
                {
                    msg += $"Neither {mapPathExternal} nor {mapPathInternal} exist.";
                }
                else if (hasExternal && !hasInternal)
                {
                    msg += $"Found only {mapPathExternal}, but it is missing a 'layout' field.";
                }
                else if (!hasExternal && hasInternal)
                {
                    msg += $"Found only {mapPathInternal}, but it is missing a 'layout' field.";
                }
                else
                {
                    msg += $"Both files exist, but neither contains a valid 'layout' field.";
                }
                UnityEngine.Debug.LogError(msg);
            }
        }


        /// <summary>
        /// Loads and instantiates a Tiled map prefab using SuperTiled2Unity.
        /// Looks for a prefab named after the map in Assets/InternalMaps.
        /// </summary>
        /// <param name="mapName">The name of the map (without extension).</param>
        private void InstantiateTiledMap(string mapName)
        {
            // Try InternalMaps first
            string prefabPathInternal = $"InternalMaps/{mapName}";
            UnityEngine.Debug.Log($"[MapLoaderFramework] Attempting to load prefab at path: Resources/{prefabPathInternal}");
            var prefab = Resources.Load<GameObject>(prefabPathInternal);
            if (prefab == null)
            {
                string prefabPathExternal = $"ExternalMaps/{mapName}";
                UnityEngine.Debug.Log($"[MapLoaderFramework] Not found. Attempting to load prefab at path: Resources/{prefabPathExternal}");
                prefab = Resources.Load<GameObject>(prefabPathExternal);
                if (prefab == null)
                {
                    UnityEngine.Debug.LogError($"[MapLoaderFramework] Tiled map prefab not found in Resources/InternalMaps or Resources/ExternalMaps for '{mapName}'. Make sure the .tmx file is imported by SuperTiled2Unity and placed in Assets/Resources/InternalMaps or Assets/Resources/ExternalMaps.");
                    return;
                }
                else
                {
                    UnityEngine.Debug.Log($"[MapLoaderFramework] Successfully loaded prefab from Resources/{prefabPathExternal}");
                }
            }
            else
            {
                UnityEngine.Debug.Log($"[MapLoaderFramework] Successfully loaded prefab from Resources/{prefabPathInternal}");
            }
            // Instantiate the prefab at origin
            var instance = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            UnityEngine.Debug.Log($"[MapLoaderFramework] Instantiated Tiled map prefab: {mapName}");
            // Track the instance by mapName (layout) and by map id if available
            instantiatedPrefabs[mapName] = instance;
            // Try to also store by map id if different
            // Find the map id from the registry (reverse lookup by layout field)
            var mapId = mapRegistry.FirstOrDefault(kvp => kvp.Value != null && kvp.Value.id != null && loadedMapsInspector.FirstOrDefault(m => m.id == kvp.Value.id)?.layout == mapName).Key;
            if (!string.IsNullOrEmpty(mapId) && mapId != mapName)
            {
                instantiatedPrefabs[mapId] = instance;
            }
        }

        /// <summary>
        /// Removes prefabs and resets prefabInstantiated for maps not in the allowed set.
        /// </summary>
        private void CleanupPrefabsNotInSet(System.Collections.Generic.HashSet<string> allowedMapIds)
        {
            var toRemove = instantiatedPrefabs.Keys.Where(id => !allowedMapIds.Contains(id)).ToList();
            foreach (var id in toRemove)
            {
                if (instantiatedPrefabs[id] != null)
                {
                    Destroy(instantiatedPrefabs[id]);
                }
                instantiatedPrefabs.Remove(id);
                if (mapRegistry.ContainsKey(id))
                {
                    mapRegistry[id].prefabInstantiated = false;
                }
            }
        }

        /// <summary>
        /// Gets all map ids within the allowed depth from the root map.
        /// </summary>
        private System.Collections.Generic.HashSet<string> GetMapIdsWithinDepth(string rootMapName, int maxDepth)
        {
            var result = new System.Collections.Generic.HashSet<string>();
            void Traverse(string mapName, int depth)
            {
                if (depth > maxDepth) return;
                if (!mapRegistry.ContainsKey(mapName) || result.Contains(mapName)) return;
                result.Add(mapName);
                // Find connections
                var mapData = loadedMapsInspector.FirstOrDefault(m => m.id == mapName);
                if (mapData != null && mapData.connections != null)
                {
                    foreach (var conn in mapData.connections)
                    {
                        if (conn != null && !string.IsNullOrEmpty(conn.mapId) && !result.Contains(conn.mapId))
                        {
                            Traverse(conn.mapId, depth + 1);
                        }
                    }
                }
            }
            Traverse(rootMapName, 0);
            return result;
        }

        /// <summary>
        /// Loads a map and its direct connections, then cleans up prefabs not in the allowed set.
        /// </summary>
        public void LoadMapAndDirectConnections(string mapName)
        {
            LoadMapAndConnections(mapName, 0, 1);
            // After loading, cleanup prefabs not in the allowed set
            var allowed = GetMapIdsWithinDepth(mapName, 1);
            CleanupPrefabsNotInSet(allowed);
        }
        public event Action<string, string> OnRawJsonUpdated; // (mapId, rawJson)

        /// <summary>
        /// Allows components to subscribe to rawJson updates for a specific map id.
        /// </summary>
        public void SubscribeToRawJson(Action<string, string> callback)
        {
            OnRawJsonUpdated += callback;
        }

        /// <summary>
        /// Allows components to unsubscribe from rawJson updates.
        /// </summary>
        public void UnsubscribeFromRawJson(Action<string, string> callback)
        {
            OnRawJsonUpdated -= callback;
        }

        /// <summary>
        /// Provides the current rawJson for a given map id (if loaded).
        /// </summary>
        public string GetRawJson(string mapId)
        {
            var map = loadedMapsInspector.FirstOrDefault(m => m.id == mapId);
            return map?.rawJson;
        }

#if UNITY_EDITOR
        /// <summary>
        /// When MapLoaderFramework is removed from the GameObject in the Editor, remove all other MapLoaderFramework runtime scripts.
        /// </summary>
        private void OnDestroy()
        {
            if (Application.isPlaying) return;
            var thisType = this.GetType();
            var assembly = thisType.Assembly;
            var runtimeNamespace = "MapLoaderFramework.Runtime";
            var allTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(MonoBehaviour)) && t.Namespace == runtimeNamespace && t != thisType);
            foreach (var type in allTypes)
            {
                var comp = GetComponent(type);
                if (comp != null)
                {
                    UnityEditor.Undo.DestroyObjectImmediate((Component)comp);
                }
            }
        }
#endif
    }
}
