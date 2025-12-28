using UnityEditor;
using UnityEngine;
using MapLoaderFramework.Runtime;
using System.Collections.Generic;

[CustomEditor(typeof(AutoMapLoader))]
public class AutoMapLoaderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        // Draw MapLoaderManager as read-only
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("mapLoaderManager"));
        EditorGUI.EndDisabledGroup();

        // Get available maps (runtime and editor folders)
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

        // If no maps found, try Assets/ExternalMaps and Assets/InternalMaps (edit mode only)
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

        var defaultMapNameProp = serializedObject.FindProperty("defaultMapName");
        if (mapNames.Count > 0)
        {
            // Insert (None) option at the top
            var options = new List<string> { "(None)" };
            options.AddRange(mapNames);
            // Map empty string to (None) and vice versa
            int selectedIndex = 0;
            if (!string.IsNullOrEmpty(defaultMapNameProp.stringValue))
            {
                int foundIndex = options.IndexOf(defaultMapNameProp.stringValue);
                if (foundIndex > 0)
                    selectedIndex = foundIndex;
            }
            selectedIndex = EditorGUILayout.Popup("Default Map Name", selectedIndex, options.ToArray());
            defaultMapNameProp.stringValue = (selectedIndex == 0) ? string.Empty : options[selectedIndex];
        }
        else
        {
            defaultMapNameProp.stringValue = EditorGUILayout.TextField("Default Map Name", defaultMapNameProp.stringValue);
            EditorGUILayout.HelpBox("No maps found. Ensure MapLoaderManager is assigned and maps are available.", MessageType.Warning);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
