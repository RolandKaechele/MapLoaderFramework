using System;
using System.IO;
using System.Linq;
using UnityEngine;
using MoonSharp.Interpreter;

namespace MapLoaderFramework.Runtime
{
    /// <summary>
    /// Loads and executes Lua scripts from the appropriate Scripts folder (Editor or persistentDataPath) in a sandboxed environment.
    /// </summary>
	[AddComponentMenu("MapLoaderFramework/Lua Script Loader")]
    [DisallowMultipleComponent]
    public static class LuaScriptLoader
    {
        // In-memory storage for loaded Lua scripts: filename -> code
        private static System.Collections.Generic.Dictionary<string, string> loadedScripts = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
 
        /// <summary>
        /// Runs a loaded Lua script by its filename.
        /// </summary>
        public static bool RunScriptByFileName(string fileName)
        {
            if (loadedScripts.TryGetValue(fileName, out var code))
            {
                try
                {
                    RunScriptSandboxed(code, fileName);
                    return true;
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[LuaScriptLoader] Failed to run script {fileName}: {ex.Message}");
                    return false;
                }
            }
            UnityEngine.Debug.LogWarning($"[LuaScriptLoader] Script {fileName} not loaded in memory");
            return false;
        }

        // Loads and executes all Lua scripts in the Scripts folder (sandboxed)
        public static void LoadAndRunAllScripts(System.Collections.Generic.IList<string> foundScripts = null)
        {
            string scriptsDir = null;
    #if UNITY_EDITOR
            scriptsDir = Path.Combine(Application.dataPath, "Scripts");
    #else
            scriptsDir = Path.Combine(Application.persistentDataPath, "Scripts");
    #endif
            if (!Directory.Exists(scriptsDir))
            {
                Debug.Log($"[LuaScriptLoader] No Scripts directory found at {scriptsDir}");
                return;
            }

            var luaFiles = Directory.GetFiles(scriptsDir, "*.lua", SearchOption.AllDirectories);
            if (foundScripts != null)
            {
                foundScripts.Clear();
                foreach (var file in luaFiles)
                {
                    foundScripts.Add(Path.GetFileName(file));
                }
            }
            loadedScripts.Clear();
            foreach (var file in luaFiles)
            {
                try
                {
                    string code = File.ReadAllText(file);
                    string fileName = Path.GetFileName(file);
                    loadedScripts[fileName] = code;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[LuaScriptLoader] Failed to load script {file}: {ex.Message}");
                }
            }

        }

        /// <summary>
        /// Removes a loaded script from memory by filename.
        /// </summary>
        public static bool RemoveScriptFromMemory(string fileName)
        {
            return loadedScripts.Remove(fileName);
        }

        // Runs a Lua script in a sandboxed environment
        public static void RunScriptSandboxed(string code, string scriptName = "external_script.lua")
        {
            // Only allow safe core modules
            var script = new Script(CoreModules.Basic | CoreModules.Table | CoreModules.String | CoreModules.Math);
            // Optionally, remove or restrict access to dangerous globals here
            script.Globals["os"] = null;
            script.Globals["io"] = null;
            script.Globals["dofile"] = null;
            script.Globals["loadfile"] = null;
            script.Globals["require"] = null;
            script.Globals["debug"] = null;
            try
            {
                script.DoString(code, null, scriptName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LuaScriptLoader] Error running script {scriptName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Removes all loaded scripts except those in the provided list.
        /// </summary>
        public static void RemoveUnusedScripts(System.Collections.Generic.IEnumerable<string> scriptsToKeep)
        {
            if (scriptsToKeep == null) return;
            var keepSet = new System.Collections.Generic.HashSet<string>(scriptsToKeep, StringComparer.OrdinalIgnoreCase);
            var keysToRemove = loadedScripts.Keys.Where(k => !keepSet.Contains(k)).ToList();
            foreach (var key in keysToRemove)
            {
                loadedScripts.Remove(key);
            }
        }
    }
}
