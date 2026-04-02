using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace MapLoaderFramework.Runtime
{
    /// <summary>
    /// <b>ModManager</b> discovers, loads, enables, and disables mods for the game.
    /// <para>
    /// Mods are placed in a <c>Mods/</c> directory (in the editor: under <c>Assets/</c>; at runtime: in
    /// <c>Application.persistentDataPath</c>). Each mod subfolder must contain a <c>mod_manifest.json</c>.
    /// </para>
    /// <para>
    /// <b>Mod folder layout:</b>
    /// <code>
    /// Mods/
    ///   my_mod/
    ///     mod_manifest.json
    ///     maps/           (optional JSON map files)
    ///     scripts/        (optional Lua scripts)
    /// </code>
    /// </para>
    /// <para>
    /// <b>Usage:</b> Attach to the same GameObject as <see cref="MapLoaderFramework"/>. Mods are
    /// discovered automatically on Awake. Use <see cref="EnableMod"/> / <see cref="DisableMod"/> to
    /// toggle mods at runtime (changes take effect on next map preload).
    /// </para>
    /// </summary>
    [AddComponentMenu("MapLoaderFramework/Mod Manager")]
    [DisallowMultipleComponent]
    public class ModManager : MonoBehaviour
    {
        // --- Inspector ---

        /// <summary>
        /// Inspector-visible list of discovered mods and their enabled state.
        /// </summary>
        [SerializeField]
        private List<ModManifest> discoveredMods = new List<ModManifest>();

        /// <summary>
        /// Read-only access to all discovered mod manifests.
        /// </summary>
        public IReadOnlyList<ModManifest> DiscoveredMods => discoveredMods;

        // --- Events ---

        /// <summary>
        /// Fired after mods are (re-)loaded. Use to trigger a map preload refresh.
        /// </summary>
        public event Action OnModsChanged;

        // --- Persistent mod-enabled state (PlayerPrefs key prefix) ---
        private const string PrefKeyPrefix = "MLF_Mod_Enabled_";

        // --- Internal ---

        private string modsRootDirectory;

        // -------------------------------------------------------------------------
        // Unity lifecycle
        // -------------------------------------------------------------------------

        private void Awake()
        {
            modsRootDirectory = GetModsRootDirectory();
            DiscoverMods();
        }

        // -------------------------------------------------------------------------
        // Directory resolution
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns the root Mods directory. In the editor this is <c>Assets/Mods</c>;
        /// at runtime it is <c>Application.persistentDataPath/Mods</c>.
        /// </summary>
        public static string GetModsRootDirectory()
        {
#if UNITY_EDITOR
            return Path.Combine(Application.dataPath, "Mods");
#else
            return Path.Combine(Application.persistentDataPath, "Mods");
#endif
        }

        // -------------------------------------------------------------------------
        // Discovery
        // -------------------------------------------------------------------------

        /// <summary>
        /// Scans the Mods directory for <c>mod_manifest.json</c> files and populates
        /// <see cref="DiscoveredMods"/>. Respects previously persisted enabled/disabled state.
        /// </summary>
        public void DiscoverMods()
        {
            discoveredMods.Clear();

            if (!Directory.Exists(modsRootDirectory))
            {
                Debug.Log($"[ModManager] No Mods directory found at {modsRootDirectory}. No mods loaded.");
                return;
            }

            var modDirs = Directory.GetDirectories(modsRootDirectory);
            foreach (var dir in modDirs)
            {
                string manifestPath = Path.Combine(dir, "mod_manifest.json");
                if (!File.Exists(manifestPath))
                {
                    Debug.LogWarning($"[ModManager] Mod folder '{dir}' has no mod_manifest.json, skipping.");
                    continue;
                }

                try
                {
                    string json = File.ReadAllText(manifestPath);
                    var manifest = JsonUtility.FromJson<ModManifest>(json);
                    if (manifest == null || string.IsNullOrEmpty(manifest.mod_id))
                    {
                        Debug.LogWarning($"[ModManager] Invalid or empty mod_id in manifest at {manifestPath}, skipping.");
                        continue;
                    }

                    manifest.modDirectory = dir;
                    // Restore previously saved enabled state from PlayerPrefs
                    string prefKey = PrefKeyPrefix + manifest.mod_id;
                    if (PlayerPrefs.HasKey(prefKey))
                        manifest.enabled = PlayerPrefs.GetInt(prefKey, 1) == 1;

                    discoveredMods.Add(manifest);
                    Debug.Log($"[ModManager] Discovered mod: {manifest.name} (id={manifest.mod_id}, enabled={manifest.enabled})");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ModManager] Failed to load mod manifest at {manifestPath}: {ex.Message}");
                }
            }

            // Sort mods respecting dependency order
            SortByDependencies();
        }

        // -------------------------------------------------------------------------
        // Enable / Disable
        // -------------------------------------------------------------------------

        /// <summary>
        /// Enables a mod by its <paramref name="modId"/>. Persists the setting and fires
        /// <see cref="OnModsChanged"/> so the framework can reload maps.
        /// </summary>
        public void EnableMod(string modId)
        {
            SetModEnabled(modId, true);
        }

        /// <summary>
        /// Disables a mod by its <paramref name="modId"/>. Persists the setting and fires
        /// <see cref="OnModsChanged"/> so the framework can reload maps.
        /// </summary>
        public void DisableMod(string modId)
        {
            SetModEnabled(modId, false);
        }

        private void SetModEnabled(string modId, bool enabled)
        {
            var mod = discoveredMods.FirstOrDefault(m => m.mod_id == modId);
            if (mod == null)
            {
                Debug.LogWarning($"[ModManager] Mod '{modId}' not found.");
                return;
            }
            mod.enabled = enabled;
            PlayerPrefs.SetInt(PrefKeyPrefix + modId, enabled ? 1 : 0);
            PlayerPrefs.Save();
            Debug.Log($"[ModManager] Mod '{modId}' {(enabled ? "enabled" : "disabled")}.");
            OnModsChanged?.Invoke();
        }

        // -------------------------------------------------------------------------
        // Map and script path helpers
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns all map JSON file paths provided by currently enabled mods.
        /// Files are returned in dependency-respecting order.
        /// </summary>
        public IEnumerable<(string filePath, string modId)> GetEnabledModMapFiles()
        {
            foreach (var mod in discoveredMods.Where(m => m.enabled))
            {
                if (mod.map_files == null) continue;
                foreach (var mapFile in mod.map_files)
                {
                    string path = Path.Combine(mod.modDirectory, "maps", mapFile);
                    if (File.Exists(path))
                        yield return (path, mod.mod_id);
                    else
                        Debug.LogWarning($"[ModManager] Map file not found: {path}");
                }
            }
        }

        /// <summary>
        /// Returns all Lua script file paths provided by currently enabled mods.
        /// </summary>
        public IEnumerable<(string filePath, string modId)> GetEnabledModScriptFiles()
        {
            foreach (var mod in discoveredMods.Where(m => m.enabled))
            {
                if (mod.script_files == null) continue;
                foreach (var scriptFile in mod.script_files)
                {
                    string path = Path.Combine(mod.modDirectory, "scripts", scriptFile);
                    if (File.Exists(path))
                        yield return (path, mod.mod_id);
                    else
                        Debug.LogWarning($"[ModManager] Script file not found: {path}");
                }
            }
        }

        // -------------------------------------------------------------------------
        // Dependency sort (Kahn's algorithm)
        // -------------------------------------------------------------------------

        private void SortByDependencies()
        {
            var sorted = new List<ModManifest>();
            var visited = new HashSet<string>();
            var inStack = new HashSet<string>();
            var modById = discoveredMods.ToDictionary(m => m.mod_id, m => m);

            void Visit(ModManifest mod)
            {
                if (inStack.Contains(mod.mod_id))
                {
                    Debug.LogWarning($"[ModManager] Circular dependency detected for mod '{mod.mod_id}'.");
                    return;
                }
                if (visited.Contains(mod.mod_id)) return;
                inStack.Add(mod.mod_id);
                if (mod.dependencies != null)
                {
                    foreach (var dep in mod.dependencies)
                    {
                        if (modById.TryGetValue(dep, out var depMod))
                            Visit(depMod);
                        else
                            Debug.LogWarning($"[ModManager] Mod '{mod.mod_id}' depends on '{dep}' which is not installed.");
                    }
                }
                inStack.Remove(mod.mod_id);
                visited.Add(mod.mod_id);
                sorted.Add(mod);
            }

            foreach (var mod in discoveredMods)
                Visit(mod);

            discoveredMods.Clear();
            discoveredMods.AddRange(sorted);
        }
    }
}
