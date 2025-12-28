using UnityEngine;

namespace MapLoaderFramework.Runtime
{
    /// <summary>
    /// Script to automatically load a default map at startup.
    /// </summary>
	[AddComponentMenu("MapLoaderFramework/Auto Map Loader")]
    [DisallowMultipleComponent]
    public class AutoMapLoader : MonoBehaviour
    {
        [SerializeField] private MapLoaderManager mapLoaderManager;

        public string defaultMapName = "";

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
                mapLoaderManager.LoadMap(defaultMapName);
            }
            else if (mapLoaderManager == null)
            {
                Debug.LogError("[AutoMapLoader] MapLoaderManager is not set or found on this GameObject or in the scene.");
            }
        }
    }
}
