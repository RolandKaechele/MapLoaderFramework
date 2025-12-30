
using MapLoaderFramework.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;


namespace MapLoaderFramework.Runtime
{
    /// <summary>
    /// <b>MapLoaderFramework</b> is the core orchestrator for modular map loading in Unity.
    /// <para>
    /// <b>Architecture:</b> Delegates standard map loading and placement logic to <see cref="MapLoader"/>, and all warp event map logic to <see cref="MapWarpLoader"/>. Manages the overall workflow, registry, and Inspector integration.
    /// </para>
    /// <para>
    /// <b>Responsibilities:</b>
    /// <list type="number">
    /// <item>Preloads all map JSON files from InternalMaps and ExternalMaps, updating the registry and Inspector mirror.</item>
    /// <item>Delegates standard map instantiation and placement to <see cref="MapLoader"/>.</item>
    /// <item>Delegates warp event map handling and cleanup to <see cref="MapWarpLoader"/>.</item>
    /// <item>Maintains Inspector-visible lists for diagnostics and editor tooling.</item>
    /// <item>Provides depth control for recursive map loading via <c>mapConnectionDepth</c> (Inspector, default 2).</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Usage:</b> Add this component to a GameObject. It will ensure all required runtime scripts are attached. Use <c>LoadMapAndConnections</c> to load a map and its connections, or <c>PreloadAllMaps</c> to refresh the registry.
    /// </para>
    /// <para>
    /// <b>Note:</b> Only explicit connections listed in each map's "connections" array are followed. Connections are NOT bidirectional unless both maps list each other as connections. Warp event destinations are handled by <see cref="MapWarpLoader"/>.
    /// </para>
    /// <para>
    /// <b>Delegation:</b> All map prefab instantiation, placement, and cleanup logic is handled by <see cref="MapLoader"/>. All warp event map logic is handled by <see cref="MapWarpLoader"/>. This class coordinates and manages the overall process.
    /// </para>
    /// </summary>
	[AddComponentMenu("MapLoaderFramework/MapLoader Framework")]
    [DisallowMultipleComponent]
    public class MapLoaderFramework : MonoBehaviour
    {

        // --- Static dictionary for absolute map positions (cleared on PreloadAllMaps) ---
        // Managed by MapLoaderFramework, used by MapLoader for placement.
        private static System.Collections.Generic.Dictionary<string, Vector3> _mapAbsolutePositions;


        // Track instantiated map prefabs by map id or layout name.
        // Used by MapLoader for instantiation and cleanup.
        private System.Collections.Generic.Dictionary<string, GameObject> instantiatedPrefabs = new System.Collections.Generic.Dictionary<string, GameObject>();


        // Internal map registry: id -> MapRegistryEntry
        // Populated on preload, used for all map lookups.
        private System.Collections.Generic.Dictionary<string, MapRegistryEntry> mapRegistry = new System.Collections.Generic.Dictionary<string, MapRegistryEntry>();


        // Inspector-visible mirror of mapRegistry for debugging and tooling.
        [SerializeField, Tooltip("Mirror of mapRegistry for Inspector view")] 
        private List<MapRegistryEntry> mapRegistryInspector = new List<MapRegistryEntry>();


        /// <summary>
        /// Handles standard map loading and placement logic.
        /// </summary>
        private MapLoader mapLoader;


        /// <summary>
        /// Handles all warp event map logic and cleanup.
        /// </summary>
        private MapWarpLoader mapWarpLoader;


        /// <summary>
        /// Track last loaded map id to detect changes.
        /// </summary>
        private string lastLoadedMapId = null;


        /// <summary>
        /// Event triggered when a map's raw JSON is updated. (mapId, rawJson)
        /// </summary>
        public event Action<string, string> OnRawJsonUpdated;



        /// <summary>
        /// Logs all children of this GameObject when F8 is pressed at runtime for diagnostics.
        /// </summary>
        private void Update()
        {
            // Press F8 to log all children of this GameObject
            if (Input.GetKeyDown(KeyCode.F8))
            {
                UnityEngine.Debug.Log("[MapLoaderFramework] F8 pressed, logging all children");
                LogAllChildrenOfThisGameObject();
            }
        }

        /// <summary>
        /// Initializes MapLoader and MapWarpLoader, preloads all maps, and manages Lua scripts on Awake.
        /// </summary>
        private void Awake()
        {
            UnityEngine.Debug.Log("[MapLoaderFramework] Awake called");
            // Ensure the static absolute positions dictionary is initialized
            if (_mapAbsolutePositions == null)
                _mapAbsolutePositions = new Dictionary<string, Vector3>();
            // Initialize core loader classes
            mapLoader = new MapLoader(instantiatedPrefabs, mapRegistry, loadedMapsInspector, _mapAbsolutePositions);
            mapWarpLoader = new MapWarpLoader(instantiatedPrefabs, mapRegistry, loadedMapsInspector, LoadMapAndConnectionsInternal);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEngine.Debug.Log("[MapLoaderFramework] Not playing in editor, adding all MapLoader components");
                AddAllMapLoaderComponents();
                return;
            }
#endif
            UnityEngine.Debug.Log("[MapLoaderFramework] Preloading all maps");
            PreloadAllMaps();
            foundLuaScriptsInspector.Clear();
            UnityEngine.Debug.Log("[MapLoaderFramework] Loading and running all Lua scripts");
            LuaScriptLoader.LoadAndRunAllScripts(foundLuaScriptsInspector);
            UnityEngine.Debug.Log("[MapLoaderFramework] Removing unused Lua scripts");
            LuaScriptLoader.RemoveUnusedScripts(foundLuaScriptsInspector);
        }

        // Delegate wrapper for MapWarpLoader
        private void LoadMapAndConnectionsInternal(string mapName, int currentDepth, int maxDepth)
        {
            UnityEngine.Debug.Log($"[MapLoaderFramework] LoadMapAndConnectionsInternal called for mapName={mapName}, currentDepth={currentDepth}, maxDepth={maxDepth}");
            LoadMapAndConnections(mapName, currentDepth, maxDepth);
        }

        /// <summary>
        /// Call this after any change to mapRegistry to update the Inspector mirror.
        /// </summary>
        private void UpdateMapRegistryInspector()
        {
            UnityEngine.Debug.Log("[MapLoaderFramework] UpdateMapRegistryInspector called");
            mapRegistryInspector.Clear();
            mapRegistryInspector.AddRange(mapRegistry.Values);
        }

        // Track last warp map ids for cleanup
        private HashSet<string> _lastWarpMapIds = new HashSet<string>();

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
        // Inspector-visible list of all warp events across all maps
        [SerializeField] private System.Collections.Generic.List<MapWarpConnection> foundWarpEventsInspector = new System.Collections.Generic.List<MapWarpConnection>();
        // Inspector-visible list of found Lua scripts (read-only)
        [SerializeField] private System.Collections.Generic.List<string> foundLuaScriptsInspector = new System.Collections.Generic.List<string>();
        public System.Collections.Generic.IReadOnlyList<string> FoundLuaScripts => foundLuaScriptsInspector;

        /// <summary>
        /// Destroys map prefabs that are not within the allowed depth from the new root map.
        /// Only out-of-scope (exceeded depth) map prefabs are destroyed; valid ones are preserved.
        /// </summary>
        private void CleanupLoadedMaps(string rootMapName, int maxDepth)
        {
            UnityEngine.Debug.Log($"[MapLoaderFramework] CleanupLoadedMaps called for rootMapName={rootMapName}, maxDepth={maxDepth}");
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
            UnityEngine.Debug.Log($"[MapLoaderFramework] Allowed map ids for cleanup: {string.Join(", ", allowed)}");
            // Use the existing cleanup utility for prefab removal
            CleanupPrefabsNotInSet(new HashSet<string>(allowed));

            // Optionally, remove absolute positions for destroyed maps
            if (_mapAbsolutePositions != null)
            {
                // Remove all keys from _mapAbsolutePositions that are not in allowed and not in instantiatedPrefabs
                var absToRemove = _mapAbsolutePositions.Keys.Where(id => !allowed.Contains(id) && !instantiatedPrefabs.ContainsKey(id)).ToList();
                foreach (var id in absToRemove)
                {
                    UnityEngine.Debug.Log($"[MapLoaderFramework] Removing absolute position for destroyed map id={id}");
                    _mapAbsolutePositions.Remove(id);
                }
            }
        }

        /// <summary>
        /// Preloads all map JSON files from InternalMaps and ExternalMaps, saving their id and JSON content in the registry and inspector list.
        /// </summary>
        public void PreloadAllMaps()
        {
            UnityEngine.Debug.Log("[MapLoaderFramework] PreloadAllMaps called");
            loadedMapsInspector.Clear();
            foundWarpEventsInspector.Clear();
            mapRegistry.Clear();
            UpdateMapRegistryInspector();
            // Clear absolute position tracking on preload
            _mapAbsolutePositions = new System.Collections.Generic.Dictionary<string, Vector3>();
            string projectPath = Application.dataPath;
            string internalDir = System.IO.Path.Combine(projectPath, "InternalMaps");
            string externalDir;
#if UNITY_EDITOR
            externalDir = System.IO.Path.Combine(projectPath, "ExternalMaps");
#else
            externalDir = System.IO.Path.Combine(Application.persistentDataPath, "ExternalMaps");
#endif
            UnityEngine.Debug.Log($"[MapLoaderFramework] InternalDir: {internalDir}");
            UnityEngine.Debug.Log($"[MapLoaderFramework] ExternalDir: {externalDir}");

            // Helper to load all map JSON files from a directory
            void LoadMapsFromDir(string dir, bool isExternal)
            {
                if (!System.IO.Directory.Exists(dir)) return;
                var files = System.IO.Directory.GetFiles(dir, "*.json", System.IO.SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    try
                    {
                        string json = System.IO.File.ReadAllText(file);
                        var mapData = JsonUtility.FromJson<MapData>(json);
                        if (mapData != null && !string.IsNullOrEmpty(mapData.id) && !string.IsNullOrEmpty(mapData.layout))
                        {
                            // Only add if not already present, or if external should overwrite
                            if (!mapRegistry.ContainsKey(mapData.id) || isExternal)
                            {
                                var entry = new MapRegistryEntry
                                {
                                    id = mapData.id,
                                    filePath = file,
                                    prefabInstantiated = false,
                                    isLoaded = false
                                };
                                mapRegistry[mapData.id] = entry;
                                // Only add to loadedMapsInspector if not already present
                                if (!loadedMapsInspector.Any(m => m.id == mapData.id))
                                {
                                    mapData.rawJson = json;
                                    loadedMapsInspector.Add(mapData);
                                }
                            }
                        }
                        else
                        {
                            UnityEngine.Debug.LogWarning($"[MapLoaderFramework] Skipping invalid map JSON: {file}");
                        }
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError($"[MapLoaderFramework] Failed to load map JSON: {file}, error: {ex.Message}");
                    }
                }
            }

            // Load internal maps first, then external (external can overwrite)
            LoadMapsFromDir(internalDir, false);
            LoadMapsFromDir(externalDir, true);

            UpdateMapRegistryInspector();

            // Find all maps that reference a warp event destination and call HandleWarpEventMaps for each
            UnityEngine.Debug.Log("[MapLoaderFramework] Searching for maps referencing warp event destinations");
            var referencingMapIds = new HashSet<string>();
            foreach (var map in loadedMapsInspector)
            {
                if (map.warp_events != null)
                {
                    foreach (var warp in map.warp_events)
                    {
                        if (!string.IsNullOrEmpty(warp.dest_map))
                        {
                            referencingMapIds.Add(map.id);
                            break;
                        }
                    }
                }
            }
            if (referencingMapIds.Count == 0)
            {
                UnityEngine.Debug.Log("[MapLoaderFramework] No maps referencing warp event destinations found.");
            }
            else
            {
                foreach (var refId in referencingMapIds)
                {
                    UnityEngine.Debug.Log($"[MapLoaderFramework] Delegating warp event map loading to MapWarpLoader for referencing map: {refId}");
                    mapWarpLoader.HandleWarpEventMaps(refId, mapConnectionDepth, GetMapIdsWithinDepth);
                }
            }
        }

        /// <summary>
        /// Called when the component is first added or reset in the Inspector.
        /// Ensures all MapLoaderFramework runtime scripts are attached to this GameObject.
        /// <summary>
        /// Preloads all map JSON files from InternalMaps and ExternalMaps, updates the registry and Inspector lists.
        /// Delegates warp event map loading and placement to <see cref="MapWarpLoader"/>.
        /// </summary>
        void Reset()
        {
            UnityEngine.Debug.Log("[MapLoaderFramework] Reset called");
            AddAllMapLoaderComponents();
        }

        /// <summary>
        /// Logs all children of the MapLoaderFramework GameObject at runtime for diagnostics.
        /// </summary>
        private void LogAllChildrenOfThisGameObject()
        {
            UnityEngine.Debug.Log("[MapLoaderFramework] LogAllChildrenOfThisGameObject called");
            // Recursively log all descendants with their full hierarchy paths
            void LogDescendants(Transform current, string path)
            {
                int childCount = current.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    var child = current.GetChild(i);
                    string childPath = string.IsNullOrEmpty(path) ? child.name : path + "/" + child.name;
                    UnityEngine.Debug.Log($"[MapLoaderFramework] Child: {childPath} (active: {child.gameObject.activeSelf}, inHierarchy: {child.gameObject.activeInHierarchy})");
                    LogDescendants(child, childPath);
                }
            }
            UnityEngine.Debug.Log($"[MapLoaderFramework] Logging all descendants of {this.gameObject.name}:");
            LogDescendants(this.transform, this.gameObject.name);
        }

        /// <summary>
        /// First pass: Recursively instantiate all map prefabs by layout.
        /// Ensures all required GameObjects exist before placement.
        /// </summary>
        private void InstantiateAllPrefabs(string mapName, int currentDepth, int maxDepth, HashSet<string> visited)
        {
            UnityEngine.Debug.Log($"[MapLoaderFramework] InstantiateAllPrefabs called for mapName={mapName}, currentDepth={currentDepth}, maxDepth={maxDepth}");
            if (visited.Contains(mapName) || currentDepth > maxDepth) {
                UnityEngine.Debug.Log($"[MapLoaderFramework] Skipping {mapName} (already visited or depth exceeded)");
                return;
            }
            visited.Add(mapName);
            // Load map data
            var entry = mapRegistry.Values.FirstOrDefault(e => System.IO.Path.GetFileNameWithoutExtension(e.filePath) == mapName);
            if (entry == null) {
                UnityEngine.Debug.Log($"[MapLoaderFramework] No registry entry found for {mapName}");
                return;
            }
            var mapData = loadedMapsInspector.FirstOrDefault(m => m.id == entry.id);
            if (mapData == null || string.IsNullOrEmpty(mapData.layout)) {
                UnityEngine.Debug.Log($"[MapLoaderFramework] No mapData or layout for {mapName}");
                return;
            }
            // Prevent multiple instantiations: check both map ID and layout name
            bool alreadyInstantiated = instantiatedPrefabs.ContainsKey(entry.id) || instantiatedPrefabs.ContainsKey(mapData.layout);
            if (!alreadyInstantiated)
            {
                UnityEngine.Debug.Log($"[MapLoaderFramework] Instantiating tiled map for layout {mapData.layout}");
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
                        UnityEngine.Debug.Log($"[MapLoaderFramework] Recursively instantiating connected map {fileName}");
                        InstantiateAllPrefabs(fileName, currentDepth + 1, maxDepth, visited);
                    }
                }
            }
        }

        /// <summary>
        /// Second pass: Recursively place all maps using parent-child relationships and absolute positions.
        /// <summary>
        /// Delegates to <see cref="MapLoader"/>: Recursively instantiate all map prefabs by layout.
        /// Ensures all required GameObjects exist before placement.
        /// </summary>
        private void PlaceAllMaps(string mapId, int currentDepth, int maxDepth, string parentId, GameObject parentInstance, MapConnection connection, HashSet<string> visited)
        {
            UnityEngine.Debug.Log($"[MapLoaderFramework] PlaceAllMaps called for mapId={mapId}, currentDepth={currentDepth}, maxDepth={maxDepth}, parentId={parentId}");
            if (visited.Contains(mapId) || currentDepth > maxDepth) {
                UnityEngine.Debug.Log($"[MapLoaderFramework] Skipping {mapId} (already visited or depth exceeded)");
                return;
            }
            visited.Add(mapId);
            var mapData = loadedMapsInspector.FirstOrDefault(m => m.id == mapId);
            if (mapData == null) {
                UnityEngine.Debug.Log($"[MapLoaderFramework] No mapData found for {mapId}");
                return;
            }
            mapLoader.PlaceMapWithParent(mapData, connection, parentId, parentInstance);
            if (mapData.connections != null && currentDepth < maxDepth)
            {
                GameObject thisInstance = null;
                instantiatedPrefabs.TryGetValue(mapData.id, out thisInstance);
                foreach (var conn in mapData.connections)
                {
                    if (conn == null || string.IsNullOrEmpty(conn.mapId) || string.Equals(conn.mapId, mapId, StringComparison.OrdinalIgnoreCase))
                        continue;
                    UnityEngine.Debug.Log($"[MapLoaderFramework] Recursively placing connected map {conn.mapId}");
                    PlaceAllMaps(conn.mapId, currentDepth + 1, maxDepth, mapData.id, thisInstance, conn, visited);
                }
            }
        }

        /// <summary>
        /// Finds and attaches all MapLoaderFramework runtime MonoBehaviour scripts to this GameObject, except itself.
        /// This allows for easy setup by simply adding MapLoaderFramework.
        /// <summary>
        /// Delegates to <see cref="MapLoader"/>: Recursively place all maps using parent-child relationships and absolute positions.
        /// </summary>
        private void AddAllMapLoaderComponents()
        {
            UnityEngine.Debug.Log("[MapLoaderFramework] AddAllMapLoaderComponents called");
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
                UnityEngine.Debug.Log("[MapLoaderFramework] Adding MapLoaderManager component");
                this.gameObject.AddComponent(managerType);
            }

            // Add AutoMapLoader second (enabled)
            var autoLoaderType = allTypes.FirstOrDefault(t => t.Name == "AutoMapLoader");
            if (autoLoaderType != null && this.GetComponent(autoLoaderType) == null)
            {
                UnityEngine.Debug.Log("[MapLoaderFramework] Adding AutoMapLoader component");
                this.gameObject.AddComponent(autoLoaderType);
            }

            // Add the rest (excluding MapLoaderManager and AutoMapLoader), disabled by default
            foreach (var type in allTypes)
            {
                if (type == managerType || type == autoLoaderType) continue;
                if (this.GetComponent(type) == null)
                {
                    UnityEngine.Debug.Log($"[MapLoaderFramework] Adding {type.Name} component (disabled by default)");
                    var comp = this.gameObject.AddComponent(type) as MonoBehaviour;
                    if (comp != null) comp.enabled = false;
                }
            }
        }


        /// <summary>
        /// Loads a map and its connections by name, recursively, up to <c>mapConnectionDepth</c>.
        /// Delegates instantiation and placement to <see cref="MapLoader"/> and warp event map handling to <see cref="MapWarpLoader"/>.
        /// </summary>
        /// <param name="mapName">The name of the map to load (without extension).</param>
        public void LoadMapAndConnections(string mapName)
        {
            UnityEngine.Debug.Log($"[MapLoaderFramework] LoadMapAndConnections called for mapName={mapName}");
            // Resolve file name to map ID before cleanup
            string rootMapId = null;
            var entry = mapRegistry.Values.FirstOrDefault(e => System.IO.Path.GetFileNameWithoutExtension(e.filePath) == mapName);
            if (entry != null)
                rootMapId = entry.id;
            else
                rootMapId = mapName; // fallback, may be a map id already
            UnityEngine.Debug.Log($"[MapLoaderFramework] Resolved rootMapId={rootMapId}");
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
                UnityEngine.Debug.Log($"[MapLoaderFramework] Placing all maps for rootEntry.id={rootEntry.id}");
                PlaceAllMaps(rootEntry.id, 0, mapConnectionDepth, null, null, null, visitedPlace);
            }

            UnityEngine.Debug.Log($"[MapLoaderFramework] Handling warp event maps for rootMapId={rootMapId}");
            mapWarpLoader.HandleWarpEventMaps(rootMapId, mapConnectionDepth, GetMapIdsWithinDepth);
        }

        // Internal recursive version with depth control
        private void LoadMapAndConnections(string mapName, int currentDepth, int maxDepth)
        {
            UnityEngine.Debug.Log($"[MapLoaderFramework] LoadMapAndConnections (internal recursive) called for mapName={mapName}, currentDepth={currentDepth}, maxDepth={maxDepth}");
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

            // Try external JSON first
            MapData extData = null;
            MapData intData = null;
            string extJson = null;
            string intJson = null;
            bool overwrite = false;

            if (System.IO.File.Exists(mapPathExternal))
            {
                UnityEngine.Debug.Log($"[MapLoaderFramework] Found external map JSON at {mapPathExternal}");
                extJson = System.IO.File.ReadAllText(mapPathExternal);
                chosenJson = extJson;
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
                UnityEngine.Debug.Log($"[MapLoaderFramework] Found internal map JSON at {mapPathInternal}");
                intJson = System.IO.File.ReadAllText(mapPathInternal);
                intData = JsonUtility.FromJson<MapData>(intJson);
            }

            // Choose which mapData to use: prefer intData if valid, else extData

            MapData mapData = null;
            if (intData != null && !string.IsNullOrEmpty(intData.layout))
            {
                UnityEngine.Debug.Log($"[MapLoaderFramework] Using internal map data for {mapName}");
                mapData = intData;
            }
            else if (extData != null && !string.IsNullOrEmpty(extData.layout))
            {
                UnityEngine.Debug.Log($"[MapLoaderFramework] Using external map data for {mapName}");
                mapData = extData;
            }

            if (mapData != null)
            {
                // Fire event if map changed
                if (lastLoadedMapId != mapData.id)
                {
                    UnityEngine.Debug.Log($"[MapLoaderFramework] Map changed: firing OnRawJsonUpdated for id={mapData.id}");
                    OnRawJsonUpdated?.Invoke(mapData.id, mapData.rawJson);
                    lastLoadedMapId = mapData.id;
                }
                mapRegistry[mapData.id].isLoaded = true;
                // Instantiate the Tiled map prefab if not already instantiated
                if (!mapRegistry[mapData.id].prefabInstantiated)
                {
                    UnityEngine.Debug.Log($"[MapLoaderFramework] Instantiating prefab for map id '{mapData.id}' with layout '{mapData.layout}' (LoadMapAndConnections)");
                    InstantiateTiledMap(mapData.layout);
                    mapRegistry[mapData.id].prefabInstantiated = true;
                }

                if (_mapAbsolutePositions == null)
                    _mapAbsolutePositions = new System.Collections.Generic.Dictionary<string, Vector3>();

                // Place the root map at origin (no parent)
                if (currentDepth == 0)
                {
                    UnityEngine.Debug.Log($"[MapLoaderFramework] Placing root map at origin for id={mapData.id}");
                    mapLoader.PlaceMapWithParent(mapData, null, null, null);
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
                                            mapLoader.PlaceMapWithParent(connMapData, conn, mapData.id, thisInstance);
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
        /// <summary>
        /// Destroys map prefabs that are not within the allowed depth from the new root map.
        /// Only out-of-scope (exceeded depth) map prefabs are destroyed; valid ones are preserved.
        /// Delegates prefab removal to <see cref="MapLoader"/> utility methods.
        /// </summary>
        /// <param name="mapName">The name of the map (without extension).</param>
        private void InstantiateTiledMap(string mapName)
        {
            UnityEngine.Debug.Log($"[MapLoaderFramework] InstantiateTiledMap called for mapName={mapName}");
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
            // Instantiate the prefab at origin and parent it to this GameObject
            var instance = Instantiate(prefab, Vector3.zero, Quaternion.identity, this.transform);
            UnityEngine.Debug.Log($"[MapLoaderFramework] Instantiated Tiled map prefab: {mapName} (parent: {this.gameObject.name}), instance name: {instance.name}, activeInHierarchy: {instance.activeInHierarchy}, scene: {instance.scene.name}");
            // Diagnostic: List all children of this GameObject after instantiation
            LogAllChildrenOfThisGameObject();
            // Track the instance by layout (mapName) and by map id if available
            instantiatedPrefabs[mapName] = instance;
            // Find the map id from the registry (reverse lookup by layout field)
            var mapId = loadedMapsInspector.FirstOrDefault(m => m.layout == mapName)?.id;
            if (!string.IsNullOrEmpty(mapId) && mapId != mapName)
            {
                UnityEngine.Debug.Log($"[MapLoaderFramework] Also tracking instance by mapId={mapId}");
                instantiatedPrefabs[mapId] = instance;
            }
        }

        /// <summary>
        /// Removes prefabs and resets prefabInstantiated for maps not in the allowed set.
        /// </summary>
        private void CleanupPrefabsNotInSet(System.Collections.Generic.HashSet<string> allowedMapIds)
        {
            UnityEngine.Debug.Log($"[MapLoaderFramework] CleanupPrefabsNotInSet called. AllowedMapIds: {string.Join(", ", allowedMapIds)}");
            var toRemove = instantiatedPrefabs.Keys.Where(id => !allowedMapIds.Contains(id)).ToList();
            foreach (var id in toRemove)
            {
                if (instantiatedPrefabs[id] != null)
                {
                    UnityEngine.Debug.Log($"[MapLoaderFramework] Destroying and uninitializing prefab for map id/layout '{id}' (CleanupPrefabsNotInSet)");
                    Destroy(instantiatedPrefabs[id]);
                }
                else
                {
                    UnityEngine.Debug.Log($"[MapLoaderFramework] Removing uninitialized/null prefab reference for map id/layout '{id}' (CleanupPrefabsNotInSet)");
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
            UnityEngine.Debug.Log($"[MapLoaderFramework] GetMapIdsWithinDepth called for rootMapName={rootMapName}, maxDepth={maxDepth}");
            var result = new System.Collections.Generic.HashSet<string>();
            if (string.IsNullOrEmpty(rootMapName))
            {
                UnityEngine.Debug.LogError("[MapLoaderFramework] GetMapIdsWithinDepth called with null or empty rootMapName. Returning empty set.");
                return result;
            }
            void Traverse(string mapName, int depth)
            {
                if (depth >= maxDepth) return;
                if (!mapRegistry.ContainsKey(mapName) || result.Contains(mapName)) return;
                result.Add(mapName);
                // Find connections
                var mapData = loadedMapsInspector.FirstOrDefault(m => m.id == mapName);
                if (mapData != null)
                {
                    // Standard connections
                    if (mapData.connections != null)
                    {
                        foreach (var conn in mapData.connections)
                        {
                            if (conn != null && !string.IsNullOrEmpty(conn.mapId) && !result.Contains(conn.mapId))
                            {
                                UnityEngine.Debug.Log($"[MapLoaderFramework] Traverse: following connection from {mapName} to {conn.mapId}");
                                Traverse(conn.mapId, depth + 1);
                            }
                        }
                    }
                    // Warp event destinations
                    if (mapData.warp_events != null)
                    {
                        foreach (var warp in mapData.warp_events)
                        {
                            if (warp != null && !string.IsNullOrEmpty(warp.dest_map) && !result.Contains(warp.dest_map))
                            {
                                UnityEngine.Debug.Log($"[MapLoaderFramework] Traverse: following warp event from {mapName} to {warp.dest_map}");
                                Traverse(warp.dest_map, depth + 1);
                            }
                        }
                    }
                }
            }
            Traverse(rootMapName, 0);
            UnityEngine.Debug.Log($"[MapLoaderFramework] GetMapIdsWithinDepth result: {string.Join(", ", result)}");
            return result;
        }

        /// <summary>
        /// Loads a map and its direct connections, then cleans up prefabs not in the allowed set.
        /// </summary>
        public void LoadMapAndDirectConnections(string mapName)
        {
            UnityEngine.Debug.Log($"[MapLoaderFramework] LoadMapAndDirectConnections called for mapName={mapName}");
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
            UnityEngine.Debug.Log($"[MapLoaderFramework] Cleaning up prefabs not in allowed set: {string.Join(", ", allowed)}");
            CleanupPrefabsNotInSet(allowed);
        }


        /// <summary>
        /// Allows components to subscribe to rawJson updates for a specific map id.
        /// </summary>
        public void SubscribeToRawJson(Action<string, string> callback)
        {
            UnityEngine.Debug.Log("[MapLoaderFramework] SubscribeToRawJson called");
            OnRawJsonUpdated += callback;
        }

        /// <summary>
        /// Allows components to unsubscribe from rawJson updates.
        /// </summary>
        public void UnsubscribeFromRawJson(Action<string, string> callback)
        {
            UnityEngine.Debug.Log("[MapLoaderFramework] UnsubscribeFromRawJson called");
            OnRawJsonUpdated -= callback;
        }

        /// <summary>
        /// Provides the current rawJson for a given map id (if loaded).
        /// </summary>
        public string GetRawJson(string mapId)
        {
            UnityEngine.Debug.Log($"[MapLoaderFramework] GetRawJson called for mapId={mapId}");
            var map = loadedMapsInspector.FirstOrDefault(m => m.id == mapId);
            return map?.rawJson;
        }

#if UNITY_EDITOR
        /// <summary>
        /// When MapLoaderFramework is removed from the GameObject in the Editor, remove all other MapLoaderFramework runtime scripts.
        /// </summary>
        private void OnDestroy()
        {
            UnityEngine.Debug.Log("[MapLoaderFramework] OnDestroy called");
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
                    UnityEngine.Debug.Log($"[MapLoaderFramework] Destroying component {type.Name} in OnDestroy");
                    UnityEditor.Undo.DestroyObjectImmediate((Component)comp);
                }
            }
        }
#endif
    }
}
