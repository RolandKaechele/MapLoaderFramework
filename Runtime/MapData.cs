using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MapData
{
    public string id;
    public string name;
    public string layout;
    public string music;
    public string region_map_section;
    public string map_type;
    public bool show_map_name;
    public int floor_number;
    public AudioData audio;
    public List<SfxData> sfx;
    public List<MapConnection> connections;
    public List<string> connectedMaps;

    // Stores the original JSON for extra fields not mapped to class members
    [TextArea(10, 10)]
    public string rawJson;
}

[Serializable]
public class MapConnection
{
    public string mapId;
    public string direction;
}

[Serializable]
public class AudioData
{
    public string backgroundMusic;
    public List<string> ambientSounds;
}

[Serializable]
public class SfxData
{
    public string trigger;
    public string file;
}

[Serializable]
public class ItemData
{
    public string id;
    public string name;
    public int quantity;
}
