# MapLoaderFramework FAQ

**Q: Can I use this framework in commercial projects?**
A: Yes, as long as you comply with the license terms below.

**Q: How do I update the framework in my project?**
A: Replace the old `MapLoaderFramework/` folder in your `Assets/` directory with the new version, or re-import the updated Unity package.

**Q: Does this support Unity 2022+?**
A: Yes, the framework is tested with Unity 2022.3 LTS and newer.

**Q: Where can I get help or report issues?**
A: See the main guide or contact the maintainer listed in the repository.

**Q: How are maps connected?**
A: Maps can be connected in two ways:

- Direct map connections (edge-to-edge, e.g., doors, paths) managed by MapLoader and defined in each map's JSON as `MapConnection` objects.
- Warp event connections (teleports, special transitions) managed by MapWarpLoader and defined as `MapWarpConnection` objects.
See the API Reference for details.

**Q: What is Map Connection Depth?**
A: The `mapConnectionDepth` field (in the MapLoaderFramework Inspector) controls how many levels of directly connected maps are loaded recursively. The default is 2, but you can adjust this to suit your project.

**Q: Can I extend or customize map loading or events?**
A: Yes! You can:

- Subscribe to map change notifications (see Integration Guide).
- Extend map JSON with new fields or warp event types.
- Add your own scripts (Lua or C#) for custom logic.
- Implement custom logic for warp event handling by extending or subscribing to MapWarpLoader.

**Q: How do I load maps or scripts at runtime?**
A: Place user maps in `Application.persistentDataPath/ExternalMaps` and scripts in `Application.persistentDataPath/Scripts` (for builds). The framework will automatically check these locations for user content.

**Q: Where are the main scripts and data structures?**
A: See the Integration Guide and API Reference for a full list and descriptions of all main runtime scripts and data structures.
