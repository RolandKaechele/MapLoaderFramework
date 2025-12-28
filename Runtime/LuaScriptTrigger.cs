using System;
using UnityEngine;

namespace MapLoaderFramework.Runtime
{
    /// <summary>
    /// Allows triggering the execution of a specific Lua script by filename at runtime.
    /// </summary>
    [AddComponentMenu("MapLoaderFramework/Lua Script Trigger")]
    [DisallowMultipleComponent]
    public class LuaScriptTrigger : MonoBehaviour
    {
        [SerializeField] private MapLoaderManager mapLoaderManager;
        [field: SerializeField]
        public string scriptFileName = "(None)";

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
        /// Call this method to execute the specified Lua script (if found in the Scripts folder).
        /// </summary>
        public void TriggerScript()
        {
            if (string.IsNullOrEmpty(scriptFileName))
            {
                Debug.LogWarning("[LuaScriptTrigger] No script file name specified.");
                return;
            }
            // Optionally, you could use mapLoaderManager for context if needed
            bool result = LuaScriptLoader.RunScriptByFileName(scriptFileName);
            if (result)
                Debug.Log($"[LuaScriptTrigger] Successfully triggered script: {scriptFileName}");
            else
                Debug.LogWarning($"[LuaScriptTrigger] Script not found or failed: {scriptFileName}");
        }
    }
}
