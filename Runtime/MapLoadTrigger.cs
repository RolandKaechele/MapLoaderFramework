using UnityEngine;

namespace MapLoaderFramework.Runtime
{
    /// <summary>
    /// Script to trigger map loading from another script or event.
    /// </summary>
	[AddComponentMenu("MapLoaderFramework/Map Load Trigger")]
    [DisallowMultipleComponent]
    public class MapLoadTrigger : MonoBehaviour
    {
        [SerializeField] private MapLoaderManager mapLoaderManager;
        public string mapToLoad;

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

        // Call this from UI, animation event, or other script
        public void TriggerLoad()
        {
            if (mapLoaderManager != null && !string.IsNullOrEmpty(mapToLoad))
            {
                mapLoaderManager.LoadMap(mapToLoad);
            }
        }
    }
}
