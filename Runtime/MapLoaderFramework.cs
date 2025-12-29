using MapLoaderFramework.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace MapLoaderFramework.Runtime
{
    /// <summary>
    /// Core framework component for MapLoaderFramework.
    ///
    /// Loads a map and its connections by name, recursively, up to <c>mapConnectionDepth</c>.
    ///
    /// <para>
    /// <b>How it works:</b>
    /// <list type="number">
    /// <item>Preloads all map JSON files from InternalMaps and ExternalMaps.</item>
    /// <item>Destroys all previously loaded map prefabs to ensure a clean scene.</item>
    /// <item>First pass: Recursively instantiates all map prefabs using the <c>layout</c> field (must match SuperTiled2Unity prefab name).</item>
    /// <item>Second pass: Recursively places all maps using parent-child relationships and absolute positions.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Depth Control:</b> The maximum depth for loading connected maps is set by <c>mapConnectionDepth</c> (Inspector, default 2).
    /// </para>
    /// <para>
    /// <b>Traversal Note:</b> Only explicit connections listed in each map's "connections" array are followed. Connections are NOT bidirectional unless both maps list each other as connections.
    /// </para>
    /// <para>
    /// Called by MapLoaderManager.
    /// </para>
    /// </summary>
    /// <param name="mapName">The name of the map to load (without extension).</param>
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

        /// <summary>
        /// Maximum depth for loading connected maps (configurable in Inspector).
        /// Controls how many levels of connected maps are loaded recursively when calling LoadMapAndConnections.
        /// Default is 2.
        /// </summary>
        [SerializeField]
        [Tooltip("Maximum depth for loading connected maps. Default is 2.")]
        private int mapConnectionDepth = 2;

        // Inspector-visible list of loaded maps (read-only)
        [SerializeField] private System.Collections.Generic.List<MapData> loadedMapsInspector = new System.Collections.Generic.List<MapData>();
        // Inspector-visible list of found Lua scripts (read-only)
        [SerializeField] private System.Collections.Generic.List<string> foundLuaScriptsInspector = new System.Collections.Generic.List<string>();
        public System.Collections.Generic.IReadOnlyList<string> FoundLuaScripts => foundLuaScriptsInspector;

        /// <summary>
        /// Destroys map prefabs that are not within the allowed depth from the new root map.
        /// Only out-of-scope (exceeded depth) map prefabs are destroyed; valid ones are preserved.
        /// </summary>
        private void CleanupLoadedMaps(string rootMapName, int maxDepth)
        {
            // Get allowed map IDs within depth
            var allowedMapIds = GetMapIdsWithinDepth(rootMapName, maxDepth);
            // Add corresponding layout names for each allowed map id
            var allowed = new HashSet<string>(allowedMapIds);
            foreach (var mapId in allowedMapIds)
            {
                var mapData = loadedMapsInspector.FirstOrDefault(m => m.id == mapId);
                if (mapData != null && !string.IsNullOrEmpty(mapData.layout))
                {
                    allowed.Add(mapData.layout);
                }
            }
            // Use the existing cleanup utility for prefab removal
            CleanupPrefabsNotInSet(new HashSet<string>(allowed));

            // Optionally, remove absolute positions for destroyed maps
            if (_mapAbsolutePositions != null)
            {
                // Remove all keys from _mapAbsolutePositions that are not in allowed and not in instantiatedPrefabs
                var absToRemove = _mapAbsolutePositions.Keys.Where(id => !allowed.Contains(id) && !instantiatedPrefabs.ContainsKey(id)).ToList();
                foreach (var id in absToRemove)
                {
                    _mapAbsolutePositions.Remove(id);
                }
            }
        }

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
        /// First pass: Recursively instantiate all map prefabs by layout.
        /// Ensures all required GameObjects exist before placement.
        /// </summary>
        private void InstantiateAllPrefabs(string mapName, int currentDepth, int maxDepth, HashSet<string> visited)
        {
            if (visited.Contains(mapName) || currentDepth > maxDepth) return;
            visited.Add(mapName);
            // Load map data
            var entry = mapRegistry.Values.FirstOrDefault(e => System.IO.Path.GetFileNameWithoutExtension(e.filePath) == mapName);
            if (entry == null) return;
            var mapData = loadedMapsInspector.FirstOrDefault(m => m.id == entry.id);
            if (mapData == null || string.IsNullOrEmpty(mapData.layout)) return;
            // Prevent multiple instantiations: check both map ID and layout name
            bool alreadyInstantiated = instantiatedPrefabs.ContainsKey(entry.id) || instantiatedPrefabs.ContainsKey(mapData.layout);
            if (!alreadyInstantiated)
            {
                InstantiateTiledMap(mapData.layout);
                entry.prefabInstantiated = true;
            }
            if (mapData != null && mapData.connections != null && currentDepth < maxDepth)
            {
                foreach (var conn in mapData.connections)
                {
                    if (conn == null || string.IsNullOrEmpty(conn.mapId) || string.Equals(conn.mapId, mapData.id, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (mapRegistry.TryGetValue(conn.mapId, out var info) && !string.IsNullOrEmpty(info.filePath))
                    {
                        string fileName = System.IO.Path.GetFileNameWithoutExtension(info.filePath);
                        InstantiateAllPrefabs(fileName, currentDepth + 1, maxDepth, visited);
                    }
                }
            }
        }

        /// <summary>
        /// Second pass: Recursively place all maps using parent-child relationships and absolute positions.
        /// </summary>
        private void PlaceAllMaps(string mapId, int currentDepth, int maxDepth, string parentId, GameObject parentInstance, MapConnection connection, HashSet<string> visited)
        {
            if (visited.Contains(mapId) || currentDepth > maxDepth) return;
            visited.Add(mapId);
            var mapData = loadedMapsInspector.FirstOrDefault(m => m.id == mapId);
            if (mapData == null) return;
            PlaceMapWithParent(mapData, connection, parentId, parentInstance);
            if (mapData.connections != null && currentDepth < maxDepth)
            {
                GameObject thisInstance = null;
                instantiatedPrefabs.TryGetValue(mapData.id, out thisInstance);
                foreach (var conn in mapData.connections)
                {
                    if (conn == null || string.IsNullOrEmpty(conn.mapId) || string.Equals(conn.mapId, mapId, StringComparison.OrdinalIgnoreCase))
                        continue;
                    PlaceAllMaps(conn.mapId, currentDepth + 1, maxDepth, mapData.id, thisInstance, conn, visited);
                }
            }
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
        /// Places a map relative to its parent (or at origin if no parent).
        /// Uses the parent's absolute position and both map and parent sizes for edge-to-edge placement.
        /// </summary>
        private void PlaceMapWithParent(MapData mapData, MapConnection connection, string parentId, GameObject parentInstance)
        {
            // Get this map's prefab instance
            GameObject thisInstance = null;
            instantiatedPrefabs.TryGetValue(mapData.id, out thisInstance);
            if (thisInstance == null)
                instantiatedPrefabs.TryGetValue(mapData.layout, out thisInstance);
            if (thisInstance == null)
                return;

            // Get parent position from _mapAbsolutePositions using parentId if available
            Vector3 parentPosition = Vector3.zero;
            if (!string.IsNullOrEmpty(parentId) && _mapAbsolutePositions.ContainsKey(parentId))
            {
                parentPosition = _mapAbsolutePositions[parentId];
            }
            else if (parentInstance != null)
            {
                parentPosition = parentInstance.transform.position;
            }

            // Get map size
            float mapWidth = 1f, mapHeight = 1f;
            var renderers = thisInstance.GetComponentsInChildren<Renderer>();
            if (renderers != null && renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                mapWidth = bounds.size.x;
                mapHeight = bounds.size.y;
            }

            // Calculate offset for true edge-to-edge placement (centered origins)
            float xOffset = 0f, yOffset = 0f;
            // Empirically determined: using mapWidth/2 + parentWidth/10 gives correct edge-to-edge placement for these assets
            float parentWidth = 1f, parentHeight = 1f;
            if (parentInstance != null)
            {
                var parentRenderers = parentInstance.GetComponentsInChildren<Renderer>();
                if (parentRenderers != null && parentRenderers.Length > 0)
                {
                    var parentBounds = parentRenderers[0].bounds;
                    foreach (var r in parentRenderers) parentBounds.Encapsulate(r.bounds);
                    parentWidth = parentBounds.size.x;
                    parentHeight = parentBounds.size.y;
                }
            }
            string direction = connection != null ? (connection.direction ?? "right").ToLowerInvariant() : "right";
            switch (direction)
            {
                case "up":
                    yOffset = (parentHeight / 2f) + (mapHeight / 10f);
                    break;
                case "down":
                    yOffset = -((parentHeight / 2f) + (mapHeight / 10f));
                    break;
                case "left":
                    xOffset = -((parentWidth / 2f) + (mapWidth / 10f));
                    break;
                case "right":
                default:
                    xOffset = (parentWidth / 2f) + (mapWidth / 10f);
                    break;
            }
            Vector3 offset = new Vector3(xOffset, yOffset, 0);
            Vector3 absPos = parentPosition + offset;
            thisInstance.transform.position = absPos;
            _mapAbsolutePositions[mapData.id] = absPos;
            UnityEngine.Debug.Log($"[MapLoaderFramework] Placed map '{mapData.id}' at {absPos} (parent: {(parentInstance != null ? parentInstance.name : "none")})");
        }

        /// <summary>
        /// <summary>
        /// Loads a map and its connections by name, recursively, up to <c>mapConnectionDepth</c>.
        ///
        /// <para>
        /// <b>How it works:</b>
        /// <list type="number">
        /// <item>Preloads all map JSON files from InternalMaps and ExternalMaps.</item>
        /// <item>Destroys all previously loaded map prefabs to ensure a clean scene.</item>
        /// <item>First pass: Recursively instantiates all map prefabs using the <c>layout</c> field (must match SuperTiled2Unity prefab name).</item>
        /// <item>Second pass: Recursively places all maps using parent-child relationships and absolute positions.</item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>Depth Control:</b> The maximum depth for loading connected maps is set by <c>mapConnectionDepth</c> (Inspector, default 2).
        /// </para>
        /// <para>
        /// Called by MapLoaderManager.
        /// </para>
        /// </summary>
        /// <param name="mapName">The name of the map to load (without extension).</param>
        public void LoadMapAndConnections(string mapName)
        {
            // Resolve file name to map ID before cleanup
            string rootMapId = null;
            var entry = mapRegistry.Values.FirstOrDefault(e => System.IO.Path.GetFileNameWithoutExtension(e.filePath) == mapName);
            if (entry != null)
                rootMapId = entry.id;
            else
                rootMapId = mapName; // fallback, may be a map id already
            // Destroy only out-of-scope map prefabs before loading new ones
            CleanupLoadedMaps(rootMapId, mapConnectionDepth);
            // First pass: instantiate all prefabs recursively
            var visitedInstantiate = new HashSet<string>();
            InstantiateAllPrefabs(mapName, 0, mapConnectionDepth, visitedInstantiate);
            // Second pass: place all maps recursively
            var visitedPlace = new HashSet<string>();
            var rootEntry = mapRegistry.Values.FirstOrDefault(e => System.IO.Path.GetFileNameWithoutExtension(e.filePath) == mapName);
            if (rootEntry != null)
            {
                PlaceAllMaps(rootEntry.id, 0, mapConnectionDepth, null, null, null, visitedPlace);
            }
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

                if (_mapAbsolutePositions == null)
                    _mapAbsolutePositions = new System.Collections.Generic.Dictionary<string, Vector3>();

                // Place the root map at origin (no parent)
                if (currentDepth == 0)
                {
                    PlaceMapWithParent(mapData, null, null, null);
                }

                if (currentDepth < maxDepth)
                {
                    try
                    {
                        UnityEngine.Debug.Log($"[MapLoaderFramework] Map '{mapName}' has {mapData.connections?.Count ?? 0} connection(s).");
                        if (mapData.connections != null)
                        {
                            // Recursively load/initiate all connected maps
                            foreach (var conn in mapData.connections)
                            {
                                if (conn == null || string.IsNullOrEmpty(conn.mapId) || string.Equals(conn.mapId, parsedId, StringComparison.OrdinalIgnoreCase))
                                    continue;
                                if (_mapAbsolutePositions.ContainsKey(conn.mapId))
                                    continue;
                                if (mapRegistry.TryGetValue(conn.mapId, out var info) && !string.IsNullOrEmpty(info.filePath))
                                {
                                    string fileName = System.IO.Path.GetFileNameWithoutExtension(info.filePath);
                                    UnityEngine.Debug.Log($"[MapLoaderFramework] Loading connected map by id: {conn.mapId} (filename: {fileName}), direction: {conn.direction}");
                                    LoadMapAndConnections(fileName, currentDepth + 1, maxDepth);
                                    // After recursion, place the connected map relative to this one
                                    if (mapRegistry.TryGetValue(conn.mapId, out var connInfo))
                                    {
                                        var connMapData = loadedMapsInspector.FirstOrDefault(m => m.id == conn.mapId);
                                        GameObject thisInstance = null;
                                        instantiatedPrefabs.TryGetValue(mapData.id, out thisInstance);
                                        if (connMapData != null)
                                            PlaceMapWithParent(connMapData, conn, mapData.id, thisInstance);
                                    }
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
            // Track the instance by layout (mapName) and by map id if available
            instantiatedPrefabs[mapName] = instance;
            // Find the map id from the registry (reverse lookup by layout field)
            var mapId = loadedMapsInspector.FirstOrDefault(m => m.layout == mapName)?.id;
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
                if (depth >= maxDepth) return;
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
            var allowedMapIds = GetMapIdsWithinDepth(mapName, 1);
            var allowed = new HashSet<string>(allowedMapIds);
            foreach (var mapId in allowedMapIds)
            {
                var mapData = loadedMapsInspector.FirstOrDefault(m => m.id == mapId);
                if (mapData != null && !string.IsNullOrEmpty(mapData.layout))
                {
                    allowed.Add(mapData.layout);
                }
            }
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
