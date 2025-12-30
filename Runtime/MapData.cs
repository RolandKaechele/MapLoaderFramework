
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MapLoaderFramework.Runtime
{

    /// <summary>
    /// <b>MapData</b> represents the deserialized data for a single map, including metadata, connections, warps, and audio.
    /// <para>
    /// <b>Usage:</b> Used by MapLoaderFramework to load, instantiate, and manage maps and their relationships.
    /// </para>
    /// </summary>
    [Serializable]
    public class MapData
    {
        /// <summary>Unique identifier for the map (from JSON).</summary>
        public string id;
        /// <summary>Human-readable name of the map.</summary>
        public string name;
        /// <summary>Layout or prefab name for the map (used for instantiation).</summary>
        public string layout;
        /// <summary>Background music file or identifier.</summary>
        public string music;
        /// <summary>Region or section this map belongs to (for world/region maps).</summary>
        public string region_map_section;
        /// <summary>Type of map (e.g., "indoor", "outdoor", etc.).</summary>
        public string map_type;
        /// <summary>Whether to show the map name in the UI.</summary>
        public bool show_map_name;
        /// <summary>Floor number (for multi-floor buildings).</summary>
        public int floor_number;
        /// <summary>Audio data for background music and ambient sounds.</summary>
        public AudioData audio;
        /// <summary>List of sound effects for this map.</summary>
        public List<SfxData> sfx;
        /// <summary>List of standard map connections (to other maps).</summary>
        public List<MapConnection> connections;
        /// <summary>List of warp event connections (special teleports, warps, etc.).</summary>
        public List<MapWarpConnection> warp_events;
        /// <summary>List of directly connected map IDs (for quick lookup, may be redundant with connections).</summary>
        public List<string> connectedMaps;

        /// <summary>
        /// Stores the original JSON for extra fields not mapped to class members (Inspector-visible).
        /// </summary>
        [TextArea(10, 10)]
        public string rawJson;
    }


    /// <summary>
    /// Represents a warp event connection (teleport, warp, etc.) between maps.
    /// </summary>
    [Serializable]
    public class MapWarpConnection
    {
        /// <summary>Unique identifier for the warp event.</summary>
        public string id;
        /// <summary>X coordinate of the warp source.</summary>
        public int src_x;
        /// <summary>Y coordinate of the warp source.</summary>
        public int src_y;
        /// <summary>Destination map ID for the warp event.</summary>
        public string dest_map;
        /// <summary>X coordinate of the warp destination.</summary>
        public int dest_x;
        /// <summary>Y coordinate of the warp destination.</summary>
        public int dest_y;
    }


    /// <summary>
    /// Represents a standard connection between two maps (e.g., door, path, etc.).
    /// </summary>
    [Serializable]
    public class MapConnection
    {
        /// <summary>ID of the connected map.</summary>
        public string mapId;
        /// <summary>Direction of the connection (e.g., "up", "down", "left", "right").</summary>
        public string direction;
    }


    /// <summary>
    /// Represents audio data for a map, including background music and ambient sounds.
    /// </summary>
    [Serializable]
    public class AudioData
    {
        /// <summary>Background music file or identifier.</summary>
        public string backgroundMusic;
        /// <summary>List of ambient sound file names or identifiers.</summary>
        public List<string> ambientSounds;
    }


    /// <summary>
    /// Represents a sound effect (SFX) for a map, including trigger and file.
    /// </summary>
    [Serializable]
    public class SfxData
    {
        /// <summary>Trigger or event name for the SFX.</summary>
        public string trigger;
        /// <summary>File name or identifier for the SFX.</summary>
        public string file;
    }


    /// <summary>
    /// Represents an item and its quantity for use in maps (optional, for extensibility).
    /// </summary>
    [Serializable]
    public class ItemData
    {
        /// <summary>Unique identifier for the item.</summary>
        public string id;
        /// <summary>Human-readable name of the item.</summary>
        public string name;
        /// <summary>Quantity of the item.</summary>
        public int quantity;
    }
}
