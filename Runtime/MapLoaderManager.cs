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
    /// <item>Exposes chapter-based loading via <see cref="LoadChapter"/>.</item>
    /// <item>Exposes mod management via <see cref="EnableMod"/> and <see cref="DisableMod"/>.</item>
    /// <item>Can be called from UI, scripts, or triggers to initiate map loading.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Usage:</b> Attach to a GameObject in your scene. Use <see cref="LoadMap"/> to load a map by name,
    /// <see cref="LoadChapter"/> to load all maps for a chapter, or <see cref="GetAvailableMaps"/> to list all available maps.
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
        /// Optional reference to the ModManager on this GameObject.
        /// </summary>
        private ModManager modManager;

        // Stored delegate references so we can cleanly unsubscribe the same instances.
        private System.Action<int, int> _chapterChangedForwarder;
        private System.Action<MapData>  _mapLoadedForwarder;

        // -------------------------------------------------------------------------
        // Events (forwarded from MapLoaderFramework)
        // -------------------------------------------------------------------------

        /// <summary>
        /// Raised when a new chapter is started. Parameters: (previousChapter, newChapter).
        /// Forwarded from <see cref="MapLoaderFramework.OnChapterChanged"/>.
        /// </summary>
        public event System.Action<int, int> OnChapterChanged;

        /// <summary>
        /// Raised whenever a root map finishes loading (direct load, chapter transition, or warp).
        /// Forwarded from <see cref="MapLoaderFramework.OnMapLoaded"/>.
        /// </summary>
        public event System.Action<MapData> OnMapLoaded;

        // -------------------------------------------------------------------------
        // Delegates / callbacks (forwarded to MapLoaderFramework)
        // -------------------------------------------------------------------------

        /// <summary>
        /// Optional fade-transition hook. Signature: (displayName, doLoad).
        /// Set this before calling <see cref="LoadMap"/> or <see cref="LoadChapter"/> to wrap
        /// map switches in a custom transition (e.g. fade-out → load → fade-in).
        /// Forwarded to <see cref="MapLoaderFramework.TransitionCallback"/>.
        /// </summary>
        public System.Action<string, System.Action> TransitionCallback
        {
            get  => mapLoader != null ? mapLoader.TransitionCallback : null;
            set  { if (mapLoader != null) mapLoader.TransitionCallback = value; }
        }

        // -------------------------------------------------------------------------
        // Properties
        // -------------------------------------------------------------------------

        /// <summary>The id of the most-recently loaded root map. <see langword="null"/> until the first map loads.</summary>
        public string CurrentMapId => mapLoader != null ? mapLoader.CurrentMapId : null;

        // -------------------------------------------------------------------------
        // Lifecycle
        // -------------------------------------------------------------------------

        /// <summary>
        /// On Awake, ensure the MapLoaderFramework component is present and assign it.
        /// </summary>
        void Awake()
        {
            mapLoader = GetComponent<MapLoaderFramework>();
            if (mapLoader == null)
            {
                Debug.LogError("MapLoaderFramework component not found! Please attach MapLoaderFramework to this GameObject.");
                return;
            }
            modManager = GetComponent<ModManager>();

            _chapterChangedForwarder = (prev, next) => OnChapterChanged?.Invoke(prev, next);
            _mapLoadedForwarder      = data          => OnMapLoaded?.Invoke(data);

            mapLoader.OnChapterChanged += _chapterChangedForwarder;
            mapLoader.OnMapLoaded      += _mapLoadedForwarder;
        }

        private void OnDestroy()
        {
            if (mapLoader == null) return;
            mapLoader.OnChapterChanged -= _chapterChangedForwarder;
            mapLoader.OnMapLoaded      -= _mapLoadedForwarder;
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


        // -------------------------------------------------------------------------
        // Chapter API (episodic structure)
        // -------------------------------------------------------------------------

        /// <summary>
        /// Loads all maps belonging to the specified chapter (1–46).
        /// Delegates to <see cref="MapLoaderFramework.LoadChapter"/> which handles
        /// fade transitions and <see cref="MapLoaderFramework.OnChapterChanged"/>.
        /// </summary>
        /// <param name="chapterId">Chapter number (1–46).</param>
        public void LoadChapter(int chapterId)
        {
            if (mapLoader != null)
                mapLoader.LoadChapter(chapterId);
        }

        /// <summary>
        /// Returns all <see cref="MapData"/> entries that belong to the specified chapter.
        /// </summary>
        public System.Collections.Generic.List<MapData> GetMapsForChapter(int chapterId)
        {
            return mapLoader != null
                ? mapLoader.GetMapsForChapter(chapterId)
                : new System.Collections.Generic.List<MapData>();
        }


        // -------------------------------------------------------------------------
        // Mod Management API (modding system)
        // -------------------------------------------------------------------------

        /// <summary>
        /// Enables the mod with the given <paramref name="modId"/> and triggers a map registry refresh.
        /// Requires a <see cref="ModManager"/> component on the same GameObject.
        /// </summary>
        public void EnableMod(string modId)
        {
            if (modManager != null)
                modManager.EnableMod(modId);
            else
                Debug.LogWarning("[MapLoaderManager] No ModManager found. Cannot enable mod.");
        }

        /// <summary>
        /// Disables the mod with the given <paramref name="modId"/> and triggers a map registry refresh.
        /// Requires a <see cref="ModManager"/> component on the same GameObject.
        /// </summary>
        public void DisableMod(string modId)
        {
            if (modManager != null)
                modManager.DisableMod(modId);
            else
                Debug.LogWarning("[MapLoaderManager] No ModManager found. Cannot disable mod.");
        }

        /// <summary>
        /// Returns all discovered mod manifests. Requires a <see cref="ModManager"/> on the same GameObject.
        /// Returns an empty list if no ModManager is present.
        /// </summary>
        public System.Collections.Generic.IReadOnlyList<ModManifest> GetDiscoveredMods()
        {
            return modManager != null
                ? modManager.DiscoveredMods
                : System.Array.Empty<ModManifest>();
        }


        // -------------------------------------------------------------------------
        // Map Discovery API
        // -------------------------------------------------------------------------

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
            // Mod maps
            if (modManager != null)
            {
                foreach (var (filePath, _) in modManager.GetEnabledModMapFiles())
                {
                    mapNames.Add(System.IO.Path.GetFileNameWithoutExtension(filePath));
                }
            }
            return new System.Collections.Generic.List<string>(mapNames);
        }
    }
}
