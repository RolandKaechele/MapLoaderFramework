using UnityEngine;

namespace MapLoaderFramework.Runtime
{
    /// <summary>
    /// <b>MapLoadTrigger</b> is a MonoBehaviour that triggers map loading in the MapLoaderFramework from UI, animation events, or other scripts.
    /// <para>
    /// <b>Responsibilities:</b>
    /// <list type="number">
    /// <item>References a <see cref="MapLoaderManager"/> and triggers map loading by map name.</item>
    /// <item>Can be called from UI buttons, animation events, or other scripts to initiate map loading.</item>
    /// <item>Automatically finds a <see cref="MapLoaderManager"/> if not assigned in the Inspector.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Usage:</b> Attach to a GameObject, assign the map name to <c>mapToLoad</c>, and call <see cref="TriggerLoad"/> from UI or code.
    /// </para>
    /// </summary>
	[AddComponentMenu("MapLoaderFramework/Map Load Trigger")]
    [DisallowMultipleComponent]
    public class MapLoadTrigger : MonoBehaviour
    {

        /// <summary>
        /// Reference to the MapLoaderManager that will handle the map loading. If not set, will be auto-assigned at runtime.
        /// </summary>
        [SerializeField] private MapLoaderManager mapLoaderManager;

        /// <summary>
        /// Name or ID of the map to load when triggered.
        /// </summary>
        public string mapToLoad;


        /// <summary>
        /// On start, ensure mapLoaderManager is assigned. Prefer component on the same GameObject, fallback to any in the scene.
        /// </summary>
        void Start()
        {
            if (mapLoaderManager == null)
            {
                // Prefer component on the same GameObject
                mapLoaderManager = GetComponent<MapLoaderManager>();
                // Fallback: find any in the scene
                if (mapLoaderManager == null)
                {
                    mapLoaderManager = FindObjectOfType<MapLoaderManager>();
                }
            }
        }


        /// <summary>
        /// Triggers loading of the specified map. Call from UI, animation event, or other script.
        /// </summary>
        public void TriggerLoad()
        {
            if (mapLoaderManager != null && !string.IsNullOrEmpty(mapToLoad))
            {
                mapLoaderManager.LoadMap(mapToLoad);
            }
        }
    }
}
