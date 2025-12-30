using System;

namespace MapLoaderFramework.Runtime
{
    /// <summary>
    /// <b>MapRegistryEntry</b> represents a single map's metadata and state in the MapLoaderFramework registry.
    /// <para>
    /// This class is used to track loaded maps, their file locations, and runtime instantiation state.
    /// </para>
    /// <para>
    /// <b>Usage:</b> Each map loaded by the framework is represented by a MapRegistryEntry, which is used for lookups, instantiation, and Inspector diagnostics.
    /// </para>
    /// </summary>
    [Serializable]
    public class MapRegistryEntry
    {
        /// <summary>
        /// Unique identifier for the map (usually the map's id from JSON).
        /// </summary>
        public string id;

        /// <summary>
        /// Human-readable name of the map (may be null or empty if not provided).
        /// </summary>
        public string name;

        /// <summary>
        /// Full file path to the map's JSON or data file.
        /// </summary>
        public string filePath;

        /// <summary>
        /// True if the map's prefab has been instantiated in the scene.
        /// </summary>
        public bool prefabInstantiated = false;

        /// <summary>
        /// True if the map has been loaded into memory (not necessarily instantiated).
        /// </summary>
        public bool isLoaded = false;
    }
}
