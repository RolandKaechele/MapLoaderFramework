using System;
using System.IO;
using System.Linq;
using UnityEngine;
using MoonSharp.Interpreter;

namespace MapLoaderFramework.Runtime
{
    /// <summary>
    /// <b>LuaScriptLoader</b> loads and executes Lua scripts from the appropriate Scripts folder (Editor or persistentDataPath) in a sandboxed environment.
    /// <para>
    /// <b>Responsibilities:</b>
    /// <list type="number">
    /// <item>Loads all Lua scripts from disk into memory for fast access.</item>
    /// <item>Executes Lua scripts in a sandboxed environment using MoonSharp.</item>
    /// <item>Allows scripts to be run by filename, removed from memory, or listed for diagnostics.</item>
    /// <item>Supports script management for both Editor and runtime environments.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Usage:</b> Use static methods to load, run, or remove scripts. Not a MonoBehaviour.
    /// </para>
    /// </summary>
	[AddComponentMenu("MapLoaderFramework/Lua Script Loader")]
    [DisallowMultipleComponent]
    public static class LuaScriptLoader
    {

        // In-memory storage for loaded Lua scripts: filename -> code
        private static System.Collections.Generic.Dictionary<string, string> loadedScripts = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
 
        /// <summary>
        /// Runs a loaded Lua script by its filename (if present in memory).
        /// </summary>
        public static bool RunScriptByFileName(string fileName)
        {
            if (loadedScripts.TryGetValue(fileName, out var code))
            {
                try
                {
                    // Run the script in a sandboxed environment
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

        /// <summary>
        /// Loads all Lua scripts from the Scripts folder into memory and optionally runs them (sandboxed).
        /// </summary>
        /// <param name="foundScripts">Optional list to populate with found script filenames.</param>
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

        /// <summary>
        /// Runs a Lua script in a sandboxed environment using MoonSharp.
        /// Only safe core modules are enabled; dangerous globals are removed.
        /// </summary>
        /// <param name="code">The Lua script code to execute.</param>
        /// <param name="scriptName">Optional script name for diagnostics.</param>
        public static void RunScriptSandboxed(string code, string scriptName = "external_script.lua")
        {
            // Only allow safe core modules
            var script = new Script(CoreModules.Basic | CoreModules.Table | CoreModules.String | CoreModules.Math);
            // Remove or restrict access to dangerous globals
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
