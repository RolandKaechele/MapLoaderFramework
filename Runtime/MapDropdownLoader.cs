using UnityEngine;
using UnityEngine.UI;

namespace MapLoaderFramework.Runtime
{
    /// <summary>
    /// UI integration: Dropdown to select and load maps at runtime.
    /// </summary>
	[AddComponentMenu("MapLoaderFramework/Map Dropdown Loader")]
    [DisallowMultipleComponent]
    public class MapDropdownLoader : MonoBehaviour
    {
        [SerializeField] private MapLoaderManager mapLoaderManager;

        public Dropdown mapDropdown;
        public string defaultMapName;

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
            if (mapDropdown != null)
            {
                // Populate dropdown with available maps
                mapDropdown.ClearOptions();
                if (mapLoaderManager != null)
                {
                    var mapNames = mapLoaderManager.GetAvailableMaps();
                    if (mapNames != null)
                    {
                        // Insert empty option at the top
                        var options = new System.Collections.Generic.List<string> { "" };
                        options.AddRange(mapNames);
                        mapDropdown.AddOptions(options);
                        // Set default selection: empty if defaultMapName is empty or not found
                        int defaultIndex = 0;
                        if (!string.IsNullOrEmpty(defaultMapName))
                        {
                            int foundIndex = options.IndexOf(defaultMapName);
                            if (foundIndex > 0)
                                defaultIndex = foundIndex;
                        }
                        mapDropdown.value = defaultIndex;
                    }
                }
                mapDropdown.onValueChanged.AddListener(OnDropdownChanged);
            }
        }

        private void OnDropdownChanged(int index)
        {
            if (mapLoaderManager != null && mapDropdown != null)
            {
                string selectedMap = mapDropdown.options[index].text;
                mapLoaderManager.LoadMap(selectedMap);
            }
        }
    }
}
