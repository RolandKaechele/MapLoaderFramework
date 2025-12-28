using UnityEngine;

namespace MapLoaderFramework.Runtime
{
    /// <summary>
    /// Main entry point for map loading and management. Attach this to a GameObject in your scene.
    /// </summary>
	[AddComponentMenu("MapLoaderFramework/MapLoader Manager")]
    [DisallowMultipleComponent]
    public class MapLoaderManager : MonoBehaviour
    {
        private MapLoaderFramework mapLoader;

        void Awake()
        {
            // Optionally, you can initialize or configure the framework here
            mapLoader = GetComponent<MapLoaderFramework>();
            if (mapLoader == null)
            {
                Debug.LogError("MapLoaderFramework component not found! Please attach MapLoaderFramework to this GameObject.");
            }
        }

        // Loads a map by name, including its connections.
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
        public System.Collections.Generic.List<string> GetAvailableMaps()
        {
            var mapNames = new System.Collections.Generic.HashSet<string>();
            // Internal maps
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
            // External maps
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
