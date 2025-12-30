using MapLoaderFramework.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MapLoaderFramework.Runtime
{

    /// <summary>
    /// <b>MapWarpLoader</b> manages all logic related to warp event maps in the MapLoaderFramework.
    /// <para>
    /// <b>Responsibilities:</b>
    /// <list type="number">
    /// <item>Loads and places all maps that are destinations of warp events, ensuring they are instantiated and positioned for debugging and visualization.</item>
    /// <item>Positions warp event maps and their directly or indirectly connected maps in the scene for clear spatial organization.</item>
    /// <item>Cleans up out-of-scope warp event map prefabs and updates the registry to reflect the current state.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Usage:</b> Instantiated and managed by <see cref="MapLoaderFramework"/>. This is not a MonoBehaviour and should not be attached to GameObjects.
    /// </para>
    /// </summary>
    public class MapWarpLoader
    {
        /// <summary>
        /// Tracks instantiated map prefabs by map id or layout name.
        /// </summary>
        private Dictionary<string, GameObject> instantiatedPrefabs;

        /// <summary>
        /// Registry of all known maps (id -> MapRegistryEntry).
        /// </summary>
        private Dictionary<string, MapRegistryEntry> mapRegistry;

        /// <summary>
        /// Inspector-visible list of loaded maps.
        /// </summary>
        private List<MapData> loadedMapsInspector;

        /// <summary>
        /// Delegate to load a map and its connections by name, depth, and maxDepth.
        /// </summary>
        private Action<string, int, int> loadMapAndConnections;

        /// <summary>
        /// Tracks the last set of warp map ids for cleanup.
        /// </summary>
        private HashSet<string> _lastWarpMapIds = new HashSet<string>();

        /// <summary>
        /// Constructs a MapWarpLoader with references to shared data structures and the map loading delegate.
        /// </summary>
        /// <param name="instantiatedPrefabs">Dictionary tracking instantiated prefabs by id/layout. Used for instantiation and cleanup.</param>
        /// <param name="mapRegistry">Registry of all known maps. Used for lookups and prefab state.</param>
        /// <param name="loadedMapsInspector">Inspector-visible list of loaded maps. Used for iterating loaded maps and warp events.</param>
        /// <param name="loadMapAndConnections">Delegate to load a map and its connections. Used to recursively load warp destinations and their connections.</param>
        public MapWarpLoader(
            Dictionary<string, GameObject> instantiatedPrefabs,
            Dictionary<string, MapRegistryEntry> mapRegistry,
            List<MapData> loadedMapsInspector,
            Action<string, int, int> loadMapAndConnections)
        {
            UnityEngine.Debug.Log("[MapWarpLoader] Constructor called");
            this.instantiatedPrefabs = instantiatedPrefabs;
            this.mapRegistry = mapRegistry;
            this.loadedMapsInspector = loadedMapsInspector;
            this.loadMapAndConnections = loadMapAndConnections;
        }

        /// <summary>
        /// Loads and places all maps that are destinations of warp events, positions them for debugging, and cleans up out-of-scope warp event maps.
        /// </summary>
        /// <param name="rootMapName">The root map name to use for allowed warp map calculation.</param>
        /// <param name="maxDepth">Maximum allowed recursion depth for connections.</param>
        /// <param name="getMapIdsWithinDepth">Delegate to get all map ids within depth. Used to determine which maps are in scope for cleanup and positioning.</param>
        public void HandleWarpEventMaps(string rootMapName, int maxDepth, Func<string, int, HashSet<string>> getMapIdsWithinDepth)
        {
            UnityEngine.Debug.Log($"[MapWarpLoader] HandleWarpEventMaps called for rootMapName={rootMapName}, maxDepth={maxDepth}");
            // Set to track all warp destination map IDs for later cleanup
            var warpMapIds = new HashSet<string>();
            int warpIndex = 0;
            const float warpBaseX = 10f; // X position for first warp group
            const float warpSpacing = 10f; // Spacing between warp groups

            // Iterate all loaded maps to find warp event destinations
            foreach (var mapData in loadedMapsInspector)
            {
                if (mapData.warp_events != null)
                {
                    foreach (var warp in mapData.warp_events)
                    {
                        // Only process if the warp event has a valid destination map
                        if (!string.IsNullOrEmpty(warp.dest_map))
                        {
                            string mapId = warp.dest_map;
                            string fileName = null;
                            // Try to resolve file name from registry, fallback to mapId
                            if (mapRegistry.TryGetValue(mapId, out var entry) && !string.IsNullOrEmpty(entry.filePath))
                            {
                                fileName = System.IO.Path.GetFileNameWithoutExtension(entry.filePath);
                            }
                            else
                            {
                                fileName = mapId;
                            }
                            UnityEngine.Debug.Log($"[MapWarpLoader] Loading warp destination map: {fileName}");
                            // Load the warp destination map and its connections
                            loadMapAndConnections(fileName, 0, maxDepth);
                            warpMapIds.Add(mapId);

                            // Try to position the warp destination instance in the scene for debugging
                            string destLayout = null;
                            if (mapRegistry.TryGetValue(mapId, out var warpEntry) && !string.IsNullOrEmpty(warpEntry.filePath))
                            {
                                var warpMapData = loadedMapsInspector.FirstOrDefault(m => m.id == mapId);
                                if (warpMapData != null && !string.IsNullOrEmpty(warpMapData.layout))
                                {
                                    destLayout = warpMapData.layout;
                                    GameObject warpInstance = null;
                                    // Try to get the instantiated prefab by id or layout
                                    instantiatedPrefabs.TryGetValue(warpMapData.id, out warpInstance);
                                    if (warpInstance == null && !string.IsNullOrEmpty(destLayout))
                                        instantiatedPrefabs.TryGetValue(destLayout, out warpInstance);
                                    if (warpInstance != null)
                                    {
                                        float groupX = warpBaseX + warpIndex * warpSpacing;
                                        UnityEngine.Debug.Log($"[MapWarpLoader] Placing warpInstance for mapId={mapId} at ({groupX}, 0, 0)");
                                        warpInstance.transform.position = new Vector3(groupX, 0, 0);
                                    }
                                }
                            }

                            // Position all maps connected to this warp destination below it for visualization
                            var connectedIds = getMapIdsWithinDepth(warp.dest_map, maxDepth);
                            int connIndex = 1;
                            foreach (var connId in connectedIds)
                            {
                                if (mapRegistry.TryGetValue(connId, out var connEntry) && !string.IsNullOrEmpty(connEntry.filePath))
                                {
                                    var connMapData = loadedMapsInspector.FirstOrDefault(m => m.id == connId);
                                    if (connMapData != null && !string.IsNullOrEmpty(connMapData.layout))
                                    {
                                        GameObject connInstance = null;
                                        // Try to get the instantiated prefab by id or layout
                                        instantiatedPrefabs.TryGetValue(connMapData.id, out connInstance);
                                        if (connInstance == null)
                                            instantiatedPrefabs.TryGetValue(connMapData.layout, out connInstance);
                                        if (connInstance != null)
                                        {
                                            float groupX = warpBaseX + warpIndex * warpSpacing;
                                            UnityEngine.Debug.Log($"[MapWarpLoader] Placing connected instance for connId={connId} at ({groupX}, {-connIndex * warpSpacing}, 0)");
                                            connInstance.transform.position = new Vector3(groupX, -connIndex * warpSpacing, 0);
                                            connIndex++;
                                        }
                                    }
                                }
                            }
                            warpIndex++;
                        }
                    }
                }
            }
            // Track the set of warp map ids for cleanup
            _lastWarpMapIds = warpMapIds;
            if (_lastWarpMapIds != null && _lastWarpMapIds.Count > 0)
            {
                UnityEngine.Debug.Log($"[MapWarpLoader] Cleaning up old warp map instances not in allowed set");
                var allowedWarpIds = getMapIdsWithinDepth(rootMapName, maxDepth);
                foreach (var warpId in _lastWarpMapIds)
                {
                    if (!allowedWarpIds.Contains(warpId))
                    {
                        // Destroy and remove prefab if it is no longer allowed
                        if (instantiatedPrefabs.ContainsKey(warpId))
                        {
                            UnityEngine.Debug.Log($"[MapWarpLoader] Destroying prefab for warpId '{warpId}' (HandleWarpEventMaps)");
                            UnityEngine.Object.Destroy(instantiatedPrefabs[warpId]);
                            instantiatedPrefabs.Remove(warpId);
                        }
                        // Mark the registry entry as not instantiated
                        if (mapRegistry.ContainsKey(warpId))
                        {
                            mapRegistry[warpId].prefabInstantiated = false;
                        }
                    }
                }
                _lastWarpMapIds.Clear();
            }
        }
    }
}

