using System;
using System.Collections.Generic;

namespace MapLoaderFramework.Runtime
{
    /// <summary>
    /// <b>ModManifest</b> represents the metadata for a single mod in the modding system.
    /// <para>
    /// Each mod folder under the Mods directory must contain a <c>mod_manifest.json</c> file that
    /// deserializes into this class. The framework reads manifests at startup to discover, enable,
    /// and disable mods without recompiling.
    /// </para>
    /// <para>
    /// <b>Example mod_manifest.json:</b>
    /// <code>
    /// {
    ///   "mod_id": "my_extra_chapter",
    ///   "name": "Extra Chapter: Die geheime Station",
    ///   "author": "Community",
    ///   "version": "1.0.0",
    ///   "description": "Adds a hidden bonus chapter after chapter 15.",
    ///   "enabled": true,
    ///   "map_files": ["extra_chapter_01.json", "extra_chapter_02.json"],
    ///   "script_files": ["extra_chapter_events.lua"],
    ///   "min_game_version": "1.0.0"
    /// }
    /// </code>
    /// </para>
    /// </summary>
    [Serializable]
    public class ModManifest
    {
        /// <summary>
        /// Unique identifier for this mod (lowercase, no spaces). Used for dependency resolution and enable/disable tracking.
        /// </summary>
        public string mod_id;

        /// <summary>
        /// Display name of the mod shown in the Mod Manager UI.
        /// </summary>
        public string name;

        /// <summary>
        /// Author or team name.
        /// </summary>
        public string author;

        /// <summary>
        /// Semantic version string (e.g., "1.2.0"). Used for update checks and compatibility.
        /// </summary>
        public string version;

        /// <summary>
        /// Short description of the mod shown in the Mod Manager UI.
        /// </summary>
        public string description;

        /// <summary>
        /// Whether this mod is currently enabled. Can be toggled at runtime via ModManager.
        /// Persisted between sessions in the player's settings.
        /// </summary>
        public bool enabled = true;

        /// <summary>
        /// List of map JSON file names (relative to the mod's root folder) that this mod provides.
        /// </summary>
        public List<string> map_files;

        /// <summary>
        /// List of Lua script file names (relative to the mod's root folder) that this mod provides.
        /// </summary>
        public List<string> script_files;

        /// <summary>
        /// List of mini-game JSON file names (relative to the mod's <c>minigames/</c> subfolder).
        /// Loaded by <see cref="ModManager.GetEnabledModMiniGameFiles"/> and consumed by
        /// <c>MiniGameManager</c> when the <c>MINIGAMEMANAGER_MLF</c> define is active.
        /// </summary>
        public List<string> minigame_files;

        /// <summary>
        /// List of DLC pack JSON file names (relative to the mod's <c>dlcpacks/</c> subfolder).
        /// Loaded by <see cref="ModManager.GetEnabledModDlcPackFiles"/> and consumed by
        /// <c>DlcManager</c> when the <c>DLCMANAGER_MLF</c> define is active.
        /// </summary>
        public List<string> dlc_pack_files;

        /// <summary>
        /// Minimum game version required for this mod. Checked at load time against the running game version.
        /// </summary>
        public string min_game_version;

        /// <summary>
        /// List of mod_ids that must be loaded before this mod.
        /// </summary>
        public List<string> dependencies;

        /// <summary>
        /// Full directory path to this mod folder. Set at runtime by ModManager; not stored in JSON.
        /// </summary>
        [NonSerialized]
        public string modDirectory;
    }
}
