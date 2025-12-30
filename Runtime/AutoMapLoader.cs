using UnityEngine;

namespace MapLoaderFramework.Runtime
{
    /// <summary>
    /// <b>AutoMapLoader</b> automatically loads a default map at startup using MapLoaderManager.
    /// <para>
    /// <b>Responsibilities:</b>
    /// <list type="number">
    /// <item>References a <see cref="MapLoaderManager"/> and loads a specified default map on Start.</item>
    /// <item>Can be used to ensure a map is always loaded when the scene starts.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Usage:</b> Attach to a GameObject, set the default map name, and ensure a MapLoaderManager is present.
    /// </para>
    /// </summary>
	[AddComponentMenu("MapLoaderFramework/Auto Map Loader")]
    [DisallowMultipleComponent]
    public class AutoMapLoader : MonoBehaviour
    {

        /// <summary>
        /// Reference to the MapLoaderManager that will handle the map loading. If not set, will be auto-assigned at runtime.
        /// </summary>
        [SerializeField] private MapLoaderManager mapLoaderManager;

        /// <summary>
        /// The name or ID of the default map to load at startup.
        /// </summary>
        public string defaultMapName = "";


        /// <summary>
        /// On start, ensure mapLoaderManager is assigned and load the default map if specified.
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
            if (mapLoaderManager != null && !string.IsNullOrEmpty(defaultMapName))
            {
                // Load the default map at startup
                mapLoaderManager.LoadMap(defaultMapName);
            }
            else if (mapLoaderManager == null)
            {
                Debug.LogError("[AutoMapLoader] MapLoaderManager is not set or found on this GameObject or in the scene.");
            }
        }
    }
}
