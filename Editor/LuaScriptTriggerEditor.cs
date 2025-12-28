using UnityEditor;
using UnityEngine;
using MapLoaderFramework.Runtime;
using System.Collections.Generic;

[CustomEditor(typeof(LuaScriptTrigger))]
public class LuaScriptTriggerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw MapLoaderManager as read-only
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("mapLoaderManager"));
        EditorGUI.EndDisabledGroup();

        // Draw scriptFileName as a dropdown using foundLuaScriptsInspector from MapLoaderFramework
        var scriptFileNameProp = serializedObject.FindProperty("scriptFileName");
        string[] scriptOptions = null;
        string[] displayOptions = null;
        int selectedIndex = 0;
        // Find MapLoaderFramework in the scene
        MapLoaderFramework.Runtime.MapLoaderFramework framework = GameObject.FindObjectOfType<MapLoaderFramework.Runtime.MapLoaderFramework>();
        if (framework != null && framework.FoundLuaScripts != null && framework.FoundLuaScripts.Count > 0)
        {
            var scripts = new List<string> { "(None)" };
            scripts.AddRange(framework.FoundLuaScripts);
            scriptOptions = scripts.ToArray();
            displayOptions = scripts.ToArray();
            // Find current selection
            selectedIndex = Mathf.Max(0, scripts.FindIndex(s => s == scriptFileNameProp.stringValue));
            selectedIndex = EditorGUILayout.Popup("Lua Script", selectedIndex, displayOptions);
            scriptFileNameProp.stringValue = scriptOptions[selectedIndex];
        }
        else
        {
            // Fallback: just a text field
            scriptFileNameProp.stringValue = EditorGUILayout.TextField("Script File Name", scriptFileNameProp.stringValue);
        }

        serializedObject.ApplyModifiedProperties();

        // Add Execute button to trigger script (only in play mode, if a script is selected, and component is enabled)
        var triggerComponent = (LuaScriptTrigger)target;
        if (Application.isPlaying && triggerComponent.enabled && !string.IsNullOrEmpty(scriptFileNameProp.stringValue) && scriptFileNameProp.stringValue != "(None)")
        {
            EditorGUILayout.Space();
            if (GUILayout.Button("Execute TriggerScript()"))
            {
                triggerComponent.TriggerScript();
            }
        }
    }
}
