using MapLoaderFramework.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MapLoaderFramework.Runtime
{
    /// <summary>
    /// <b>MapLoader</b> handles standard map loading, instantiation, placement, and cleanup logic for MapLoaderFramework.
    /// <para>
    /// <b>Responsibilities:</b>
    /// <list type="number">
    /// <item>Recursively instantiates map prefabs by layout.</item>
    /// <item>Places maps in the scene based on parent-child relationships and connection directions.</item>
    /// <item>Cleans up out-of-scope map prefabs and absolute positions.</item>
    /// <item>Delegates warp event map logic to <see cref="MapWarpLoader"/> (not handled here).</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Usage:</b> Instantiated and managed by <see cref="MapLoaderFramework"/>. Not a MonoBehaviour.
    /// </para>
    /// </summary>
    public class MapLoader
    {

        // Tracks instantiated map prefabs by map id or layout name
        private Dictionary<string, GameObject> instantiatedPrefabs;
        // Registry of all known maps (id -> MapRegistryEntry)
        private Dictionary<string, MapRegistryEntry> mapRegistry;
        // Inspector-visible list of loaded maps
        private List<MapData> loadedMapsInspector;
        // Static dictionary for absolute map positions (shared across framework)
        private static Dictionary<string, Vector3> _mapAbsolutePositions;

        /// <summary>
        /// Constructs a MapLoader with references to shared data structures.
        /// </summary>
        /// <param name="instantiatedPrefabs">Dictionary tracking instantiated prefabs by id/layout. Used for instantiation and cleanup.</param>
        /// <param name="mapRegistry">Registry of all known maps. Used for lookups and prefab state.</param>
        /// <param name="loadedMapsInspector">Inspector-visible list of loaded maps. Used for iterating loaded maps and warp events.</param>
        /// <param name="mapAbsolutePositions">Static dictionary for absolute map positions. Used for placement and cleanup.</param>
        public MapLoader(
            Dictionary<string, GameObject> instantiatedPrefabs,
            Dictionary<string, MapRegistryEntry> mapRegistry,
            List<MapData> loadedMapsInspector,
            Dictionary<string, Vector3> mapAbsolutePositions)
        {
            UnityEngine.Debug.Log("[MapLoader] Constructor called");
            this.instantiatedPrefabs = instantiatedPrefabs;
            this.mapRegistry = mapRegistry;
            this.loadedMapsInspector = loadedMapsInspector;
            // Ensure the static absolute positions dictionary is initialized
            if (mapAbsolutePositions == null)
            {
                UnityEngine.Debug.LogWarning("[MapLoader] mapAbsolutePositions passed to constructor is null. Initializing new dictionary.");
                _mapAbsolutePositions = new Dictionary<string, Vector3>();
            }
            else
            {
                _mapAbsolutePositions = mapAbsolutePositions;
            }
        }

        /// <summary>
        /// Recursively instantiates all map prefabs by layout, ensuring all required GameObjects exist before placement.
        /// </summary>
        /// <param name="mapName">The name of the map to instantiate (without extension).</param>
        /// <param name="currentDepth">Current recursion depth.</param>
        /// <param name="maxDepth">Maximum allowed recursion depth.</param>
        /// <param name="visited">Set of already visited map names to prevent cycles.</param>
        /// <param name="instantiateTiledMap">Delegate to instantiate a prefab by layout name.</param>
        public void InstantiateAllPrefabs(string mapName, int currentDepth, int maxDepth, HashSet<string> visited, Func<string, GameObject> instantiateTiledMap)
        {
            UnityEngine.Debug.Log($"[MapLoader] InstantiateAllPrefabs called for mapName={mapName}, currentDepth={currentDepth}, maxDepth={maxDepth}");
            // Prevent cycles and excessive recursion
            if (visited.Contains(mapName) || currentDepth > maxDepth) {
                UnityEngine.Debug.Log($"[MapLoader] Skipping {mapName} (already visited or depth exceeded)");
                return;
            }
            visited.Add(mapName);
            // Find map registry entry by file name
            var entry = mapRegistry.Values.FirstOrDefault(e => System.IO.Path.GetFileNameWithoutExtension(e.filePath) == mapName);
            if (entry == null) {
                UnityEngine.Debug.Log($"[MapLoader] No registry entry found for {mapName}");
                return;
            }
            // Find loaded map data by id
            var mapData = loadedMapsInspector.FirstOrDefault(m => m.id == entry.id);
            if (mapData == null || string.IsNullOrEmpty(mapData.layout)) {
                UnityEngine.Debug.Log($"[MapLoader] No mapData or layout for {mapName}");
                return;
            }
            // Prevent multiple instantiations: check both map ID and layout name
            bool alreadyInstantiated = instantiatedPrefabs.ContainsKey(entry.id) || instantiatedPrefabs.ContainsKey(mapData.layout);
            if (!alreadyInstantiated)
            {
                UnityEngine.Debug.Log($"[MapLoader] Instantiating prefab for layout {mapData.layout}");
                // Instantiate the prefab using the provided delegate
                var instance = instantiateTiledMap(mapData.layout);
                entry.prefabInstantiated = true;
            }
            // Recursively instantiate connected maps
            if (mapData != null && mapData.connections != null && currentDepth < maxDepth)
            {
                foreach (var conn in mapData.connections)
                {
                    // Skip invalid or self-referential connections
                    if (conn == null || string.IsNullOrEmpty(conn.mapId) || string.Equals(conn.mapId, mapData.id, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (mapRegistry.TryGetValue(conn.mapId, out var info) && !string.IsNullOrEmpty(info.filePath))
                    {
                        string fileName = System.IO.Path.GetFileNameWithoutExtension(info.filePath);
                        UnityEngine.Debug.Log($"[MapLoader] Recursively instantiating connected map {fileName}");
                        InstantiateAllPrefabs(fileName, currentDepth + 1, maxDepth, visited, instantiateTiledMap);
                    }
                }
            }
        }

        /// <summary>
        /// Recursively places all maps using parent-child relationships and absolute positions.
        /// </summary>
        /// <param name="mapId">The id of the map to place.</param>
        /// <param name="currentDepth">Current recursion depth.</param>
        /// <param name="maxDepth">Maximum allowed recursion depth.</param>
        /// <param name="parentId">Id of the parent map (if any).</param>
        /// <param name="parentInstance">GameObject instance of the parent map (if any).</param>
        /// <param name="connection">Connection data from parent to this map.</param>
        /// <param name="visited">Set of already visited map ids to prevent cycles.</param>
        public void PlaceAllMaps(string mapId, int currentDepth, int maxDepth, string parentId, GameObject parentInstance, MapConnection connection, HashSet<string> visited)
        {
            UnityEngine.Debug.Log($"[MapLoader] PlaceAllMaps called for mapId={mapId}, currentDepth={currentDepth}, maxDepth={maxDepth}, parentId={parentId}");
            // Prevent cycles and excessive recursion
            if (visited.Contains(mapId) || currentDepth > maxDepth) {
                UnityEngine.Debug.Log($"[MapLoader] Skipping {mapId} (already visited or depth exceeded)");
                return;
            }
            visited.Add(mapId);
            // Find loaded map data by id
            var mapData = loadedMapsInspector.FirstOrDefault(m => m.id == mapId);
            if (mapData == null) {
                UnityEngine.Debug.LogError($"[MapLoader] No mapData found for {mapId}");
                return;
            }
            // Place this map relative to its parent
            PlaceMapWithParent(mapData, connection, parentId, parentInstance);
            // Recursively place connected maps
            if (mapData.connections != null && currentDepth < maxDepth)
            {
                GameObject thisInstance = null;
                instantiatedPrefabs.TryGetValue(mapData.id, out thisInstance);
                foreach (var conn in mapData.connections)
                {
                    // Skip invalid or self-referential connections
                    if (conn == null || string.IsNullOrEmpty(conn.mapId) || string.Equals(conn.mapId, mapId, StringComparison.OrdinalIgnoreCase))
                        continue;
                    UnityEngine.Debug.Log($"[MapLoader] Recursively placing connected map {conn.mapId}");
                    PlaceAllMaps(conn.mapId, currentDepth + 1, maxDepth, mapData.id, thisInstance, conn, visited);
                }
            }
        }

        /// <summary>
        /// Places a map GameObject in the scene relative to its parent, using connection direction and bounding boxes.
        /// </summary>
        /// <param name="mapData">The map data to place.</param>
        /// <param name="connection">Connection data from parent to this map.</param>
        /// <param name="parentId">Id of the parent map (if any).</param>
        /// <param name="parentInstance">GameObject instance of the parent map (if any).</param>
        public void PlaceMapWithParent(MapData mapData, MapConnection connection, string parentId, GameObject parentInstance)
        {
            if (mapData == null)
            {
                UnityEngine.Debug.LogError("[MapLoader] PlaceMapWithParent called with null mapData!");
                return;
            }
            if (string.IsNullOrEmpty(mapData.id))
            {
                UnityEngine.Debug.LogError("[MapLoader] PlaceMapWithParent: mapData.id is null or empty!");
                return;
            }
            if (string.IsNullOrEmpty(mapData.layout))
            {
                UnityEngine.Debug.LogError($"[MapLoader] PlaceMapWithParent: mapData.layout is null or empty for mapData.id={mapData.id}!");
                return;
            }
            UnityEngine.Debug.Log($"[MapLoader] PlaceMapWithParent called for mapData.id={mapData.id}, parentId={parentId}, parentInstance={(parentInstance != null ? parentInstance.name : "null")}");
            // Try to get the instance by id or layout
            GameObject thisInstance = null;
            if (!instantiatedPrefabs.TryGetValue(mapData.id, out thisInstance))
            {
                UnityEngine.Debug.LogWarning($"[MapLoader] instantiatedPrefabs has no entry for mapData.id={mapData.id}");
            }
            if (thisInstance == null && !string.IsNullOrEmpty(mapData.layout))
            {
                instantiatedPrefabs.TryGetValue(mapData.layout, out thisInstance);
                if (thisInstance == null)
                {
                    UnityEngine.Debug.LogWarning($"[MapLoader] instantiatedPrefabs has no entry for mapData.layout={mapData.layout}");
                }
            }
            if (thisInstance == null)
            {
                UnityEngine.Debug.LogError($"[MapLoader] No instance found for mapData.id={mapData.id} or layout={mapData.layout}. Cannot place map.");
                return;
            }

            // Special handling: if this map is a warp event destination, force to a fixed position
            bool isWarpEvent = false;
            foreach (var m in loadedMapsInspector)
            {
                if (m.warp_events != null && m.warp_events.Any(w => w.dest_map == mapData.id))
                {
                    isWarpEvent = true;
                    break;
                }
            }
            if (isWarpEvent)
            {
                if (thisInstance == null)
                {
                    var availableKeys = string.Join(", ", instantiatedPrefabs.Keys);
                    UnityEngine.Debug.LogError($"[MapLoader] Warp event: No instance found for mapData.id={mapData.id} or layout={mapData.layout}. Cannot place warp event map. Available instances: [{availableKeys}]");
                    return;
                }
                thisInstance.transform.position = new Vector3(-11f, 0f, 0f);
                try
                {
                    _mapAbsolutePositions[mapData.id] = thisInstance.transform.position;
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[MapLoader] Exception while setting _mapAbsolutePositions for mapData.id={mapData.id}: {ex}");
                    return;
                }
                UnityEngine.Debug.Log($"[MapLoader] Placed warp event map '{mapData.id}' at {thisInstance.transform.position} (forced warp placement)");
                return;
            }

            // Determine parent position
            if (thisInstance == null)
            {
                var availableKeys = string.Join(", ", instantiatedPrefabs.Keys);
                UnityEngine.Debug.LogError($"[MapLoader] After warp event: No instance found for mapData.id={mapData.id} or layout={mapData.layout}. Cannot place map. Available instances: [{availableKeys}]");
                return;
            }
            Vector3 parentPosition = Vector3.zero;
            if (!string.IsNullOrEmpty(parentId) && _mapAbsolutePositions.ContainsKey(parentId))
            {
                parentPosition = _mapAbsolutePositions[parentId];
            }
            else if (parentInstance != null)
            {
                parentPosition = parentInstance.transform.position;
            }

            // Calculate map bounds for placement
            float mapWidth = 1f, mapHeight = 1f;
            var renderers = thisInstance.GetComponentsInChildren<Renderer>();
            if (renderers != null && renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                mapWidth = bounds.size.x;
                mapHeight = bounds.size.y;
            }

            // Calculate parent bounds for offset
            float xOffset = 0f, yOffset = 0f;
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
            // Determine placement direction
            string direction = connection != null ? (connection.direction ?? "right").ToLowerInvariant() : "right";
            UnityEngine.Debug.Log($"[MapLoader] Placement direction for mapData.id={mapData.id}: {direction}");
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
            try
            {
                if (thisInstance == null)
                {
                    UnityEngine.Debug.LogError($"[MapLoader] thisInstance is null for mapData.id={mapData.id} at placement. Aborting placement.");
                    return;
                }
                thisInstance.transform.position = absPos;
                if (_mapAbsolutePositions == null)
                {
                    UnityEngine.Debug.LogError($"[MapLoader] _mapAbsolutePositions is null when placing mapData.id={mapData.id}. Aborting placement.");
                    return;
                }
                _mapAbsolutePositions[mapData.id] = absPos;
                UnityEngine.Debug.Log($"[MapLoader] Placed map '{mapData.id}' at {absPos} (parent: {(parentInstance != null ? parentInstance.name : "none")})");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[MapLoader] Exception while placing mapData.id={mapData.id}: {ex}");
            }
        }

        /// <summary>
        /// Destroys map prefabs that are not within the allowed depth from the new root map.
        /// Only out-of-scope (exceeded depth) map prefabs are destroyed; valid ones are preserved.
        /// Also removes absolute positions for destroyed maps.
        /// </summary>
        /// <param name="rootMapName">The root map name to start from.</param>
        /// <param name="maxDepth">Maximum allowed recursion depth.</param>
        /// <param name="getMapIdsWithinDepth">Delegate to get all map ids within depth.</param>
        public void CleanupLoadedMaps(string rootMapName, int maxDepth, Func<string, int, HashSet<string>> getMapIdsWithinDepth)
        {
            UnityEngine.Debug.Log($"[MapLoader] CleanupLoadedMaps called for rootMapName={rootMapName}, maxDepth={maxDepth}");
            var allowedMapIds = getMapIdsWithinDepth(rootMapName, maxDepth);
            var allowed = new HashSet<string>(allowedMapIds);
            // Add corresponding layout names for each allowed map id
            foreach (var mapId in allowedMapIds)
            {
                var mapData = loadedMapsInspector.FirstOrDefault(m => m.id == mapId);
                if (mapData != null && !string.IsNullOrEmpty(mapData.layout))
                {
                    allowed.Add(mapData.layout);
                }
            }
            UnityEngine.Debug.Log($"[MapLoader] Allowed map ids for cleanup: {string.Join(", ", allowed)}");
            // Remove prefabs not in allowed set
            CleanupPrefabsNotInSet(new HashSet<string>(allowed));

            // Optionally, remove absolute positions for destroyed maps
            if (_mapAbsolutePositions != null)
            {
                var absToRemove = _mapAbsolutePositions.Keys.Where(id => !allowed.Contains(id) && !instantiatedPrefabs.ContainsKey(id)).ToList();
                foreach (var id in absToRemove)
                {
                    UnityEngine.Debug.Log($"[MapLoader] Removing absolute position for destroyed map id={id}");
                    _mapAbsolutePositions.Remove(id);
                }
            }
        }

        /// <summary>
        /// Removes prefabs and resets prefabInstantiated for maps not in the allowed set.
        /// </summary>
        /// <param name="allowedMapIds">Set of map ids/layouts to keep.</param>
        public void CleanupPrefabsNotInSet(HashSet<string> allowedMapIds)
        {
            UnityEngine.Debug.Log($"[MapLoader] CleanupPrefabsNotInSet called. AllowedMapIds: {string.Join(", ", allowedMapIds)}");
            var toRemove = instantiatedPrefabs.Keys.Where(id => !allowedMapIds.Contains(id)).ToList();
            foreach (var id in toRemove)
            {
                if (instantiatedPrefabs[id] != null)
                {
                    UnityEngine.Debug.Log($"[MapLoader] Destroying and uninitializing prefab for map id/layout '{id}' (CleanupPrefabsNotInSet)");
                    UnityEngine.Object.Destroy(instantiatedPrefabs[id]);
                }
                else
                {
                    UnityEngine.Debug.Log($"[MapLoader] Removing uninitialized/null prefab reference for map id/layout '{id}' (CleanupPrefabsNotInSet)");
                }
                instantiatedPrefabs.Remove(id);
                if (mapRegistry.ContainsKey(id))
                {
                    mapRegistry[id].prefabInstantiated = false;
                }
            }
        }
    }
}
