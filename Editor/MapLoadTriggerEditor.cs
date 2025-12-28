using UnityEditor;
using UnityEngine;
using MapLoaderFramework.Runtime;
using System.Collections.Generic;

[CustomEditor(typeof(MapLoadTrigger))]
public class MapLoadTriggerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        // Draw MapLoaderManager as read-only
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("mapLoaderManager"));
        EditorGUI.EndDisabledGroup();

        // Get available maps
        List<string> mapNames = new List<string>();
        var mapLoaderManagerProp = serializedObject.FindProperty("mapLoaderManager");
        MapLoaderManager mapLoaderManager = mapLoaderManagerProp.objectReferenceValue as MapLoaderManager;
        if (mapLoaderManager != null)
        {
            mapNames = mapLoaderManager.GetAvailableMaps();
        }
        else
        {
            MapLoaderManager found = GameObject.FindObjectOfType<MapLoaderManager>();
            if (found != null)
            {
                mapNames = found.GetAvailableMaps();
            }
        }

        // Also search Assets/ExternalMaps and Assets/InternalMaps in edit mode
        if (mapNames.Count == 0)
        {
            string[] editorDirs = {
                "Assets/ExternalMaps",
                "Assets/InternalMaps"
            };
            var editorMapNames = new HashSet<string>();
            foreach (var dir in editorDirs)
            {
                if (System.IO.Directory.Exists(dir))
                {
                    foreach (var file in System.IO.Directory.GetFiles(dir, "*.json"))
                    {
                        editorMapNames.Add(System.IO.Path.GetFileNameWithoutExtension(file));
                    }
                }
            }
            mapNames = new List<string>(editorMapNames);
        }

        var mapToLoadProp = serializedObject.FindProperty("mapToLoad");
        if (mapNames.Count > 0)
        {
            // Insert (None) option at the top
            var options = new List<string> { "(None)" };
            options.AddRange(mapNames);
            // Map empty string to (None) and vice versa
            int selectedIndex = 0;
            if (!string.IsNullOrEmpty(mapToLoadProp.stringValue))
            {
                int foundIndex = options.IndexOf(mapToLoadProp.stringValue);
                if (foundIndex > 0)
                    selectedIndex = foundIndex;
            }
            selectedIndex = EditorGUILayout.Popup("Map To Load", selectedIndex, options.ToArray());
            mapToLoadProp.stringValue = (selectedIndex == 0) ? string.Empty : options[selectedIndex];
        }
        else
        {
            mapToLoadProp.stringValue = EditorGUILayout.TextField("Map To Load", mapToLoadProp.stringValue);
            EditorGUILayout.HelpBox("No maps found. Ensure MapLoaderManager is assigned and maps are available.", MessageType.Warning);
        }

        serializedObject.ApplyModifiedProperties();

        // Add Execute button to trigger map loading (only in play mode, if a map is selected, and component is enabled)
        var triggerComponent = (MapLoadTrigger)target;
        if (Application.isPlaying && triggerComponent.enabled && !string.IsNullOrEmpty(mapToLoadProp.stringValue))
        {
            EditorGUILayout.Space();
            if (GUILayout.Button("Execute TriggerLoad()"))
            {
                triggerComponent.TriggerLoad();
            }
        }

    }
}
