using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

namespace MapLoaderFramework.Editor
{
    [CustomEditor(typeof(MapLoaderFramework.Runtime.MapLoaderFramework))]
    public class MapLoaderFrameworkEditor : UnityEditor.Editor
    {

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Use the button below to remove MapLoaderFramework and all related runtime scripts from this GameObject.", MessageType.Info);

            if (GUILayout.Button("Remove All MapLoaderFramework Components"))
            {
                RemoveAllMapLoaderComponents((MapLoaderFramework.Runtime.MapLoaderFramework)target);
            }
        }

        private void RemoveAllMapLoaderComponents(MapLoaderFramework.Runtime.MapLoaderFramework framework)
        {
            var go = framework.gameObject;
            var thisType = framework.GetType();
            var assembly = thisType.Assembly;
            var runtimeNamespace = "MapLoaderFramework.Runtime";
            var allTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(MonoBehaviour)) && t.Namespace == runtimeNamespace);
            foreach (var type in allTypes)
            {
                var comp = go.GetComponent(type);
                if (comp != null)
                {
                    Undo.DestroyObjectImmediate((Component)comp);
                }
            }
        }
    }
}
