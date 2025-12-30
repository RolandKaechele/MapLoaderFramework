using UnityEngine;

namespace MapLoaderFramework.Runtime
{
    /// <summary>
    /// <b>MapLoaderManager</b> is the main MonoBehaviour entry point for map loading and management in the MapLoaderFramework.
    /// <para>
    /// <b>Responsibilities:</b>
    /// <list type="number">
    /// <item>References and delegates to <see cref="MapLoaderFramework"/> for all map loading operations.</item>
    /// <item>Provides a public API for loading maps by name and listing available maps.</item>
    /// <item>Can be called from UI, scripts, or triggers to initiate map loading.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Usage:</b> Attach to a GameObject in your scene. Use <see cref="LoadMap"/> to load a map by name, or <see cref="GetAvailableMaps"/> to list all available maps.
    /// </para>
    /// </summary>
	[AddComponentMenu("MapLoaderFramework/MapLoader Manager")]
    [DisallowMultipleComponent]
    public class MapLoaderManager : MonoBehaviour
    {

        /// <summary>
        /// Reference to the MapLoaderFramework component that handles the core map loading logic.
        /// </summary>
        private MapLoaderFramework mapLoader;


        /// <summary>
        /// On Awake, ensure the MapLoaderFramework component is present and assign it.
        /// </summary>
        void Awake()
        {
            // Optionally, you can initialize or configure the framework here
            mapLoader = GetComponent<MapLoaderFramework>();
            if (mapLoader == null)
            {
                Debug.LogError("MapLoaderFramework component not found! Please attach MapLoaderFramework to this GameObject.");
            }
        }


        /// <summary>
        /// Loads a map by name, including all its connections. Call from UI, triggers, or other scripts.
        /// </summary>
        /// <param name="mapName">The name or ID of the map to load.</param>
        public void LoadMap(string mapName)
        {
            if (mapLoader != null)
            {
                mapLoader.LoadMapAndConnections(mapName);
            }
        }


        /// <summary>
        /// Returns a list of all available map names (without extension) from InternalMaps and ExternalMaps.
        /// </summary>
        /// <returns>List of available map names as strings.</returns>
        public System.Collections.Generic.List<string> GetAvailableMaps()
        {
            var mapNames = new System.Collections.Generic.HashSet<string>();
            // Internal maps directory
            string internalDir = System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "MapLoaderFramework/InternalMaps");
            if (System.IO.Directory.Exists(internalDir))
            {
                foreach (var file in System.IO.Directory.GetFiles(internalDir, "*.json"))
                {
                    // Exclude files in the package folder
                    string packagePath = System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath, "MapLoaderFramework"));
                    string fileFullPath = System.IO.Path.GetFullPath(file);
                    if (!fileFullPath.StartsWith(packagePath))
                    {
                        mapNames.Add(System.IO.Path.GetFileNameWithoutExtension(file));
                    }
                }
            }
            // External maps directory
            string externalDir = System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "MapLoaderFramework/ExternalMaps");
            if (System.IO.Directory.Exists(externalDir))
            {
                foreach (var file in System.IO.Directory.GetFiles(externalDir, "*.json"))
                {
                    mapNames.Add(System.IO.Path.GetFileNameWithoutExtension(file));
                }
            }
            return new System.Collections.Generic.List<string>(mapNames);
        }
    }
}
