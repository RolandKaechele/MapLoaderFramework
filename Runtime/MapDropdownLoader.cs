using UnityEngine;
using UnityEngine.UI;

namespace MapLoaderFramework.Runtime
{
    /// <summary>
    /// <b>MapDropdownLoader</b> provides UI integration for selecting and loading maps at runtime using a Unity Dropdown.
    /// <para>
    /// <b>Responsibilities:</b>
    /// <list type="number">
    /// <item>Populates a Dropdown UI element with available map names from <see cref="MapLoaderManager"/>.</item>
    /// <item>Handles user selection to trigger map loading.</item>
    /// <item>Supports setting a default map selection.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Usage:</b> Attach to a GameObject with a Dropdown and (optionally) a MapLoaderManager. Assign the Dropdown in the Inspector.
    /// </para>
    /// </summary>
	[AddComponentMenu("MapLoaderFramework/Map Dropdown Loader")]
    [DisallowMultipleComponent]
    public class MapDropdownLoader : MonoBehaviour
    {

        /// <summary>
        /// Reference to the MapLoaderManager that provides available maps and handles loading. If not set, will be auto-assigned at runtime.
        /// </summary>
        [SerializeField] private MapLoaderManager mapLoaderManager;

        /// <summary>
        /// The Dropdown UI element to populate with map names.
        /// </summary>
        public Dropdown mapDropdown;

        /// <summary>
        /// The default map name to select in the dropdown (optional).
        /// </summary>
        public string defaultMapName;


        /// <summary>
        /// On start, ensure mapLoaderManager is assigned, populate the dropdown, and set up the event listener.
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
                // Listen for dropdown value changes
                mapDropdown.onValueChanged.AddListener(OnDropdownChanged);
            }
        }


        /// <summary>
        /// Called when the dropdown value changes. Loads the selected map using MapLoaderManager.
        /// </summary>
        /// <param name="index">The index of the selected dropdown option.</param>
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
