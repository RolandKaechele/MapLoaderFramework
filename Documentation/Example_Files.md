# MapLoaderFramework Example Files

This document lists and explains the example files included with the framework:

## Example Map JSON

- `Examples/InternalMaps/example_internal_map.json`: Sample internal map data, demonstrating both direct map connections (edge-to-edge) and warp event connections.
- `Examples/InternalMaps/example_internal_start_map.json`: Example of a starting map for internal use.
- `Examples/InternalMaps/example_internal_house.json`: Example of an internal house map.
- `Examples/InternalMaps/example_test_up.json`, `example_test_down.json`, `example_test_left.json`, `example_test_right.json`, `example_test_left_up.json`, `example_test_left_left.json`: Test maps for demonstrating direct connections in various directions.
- `overwrite_test.json` (in both InternalMaps and ExternalMaps): Example for demonstrating map override/merging between internal and external maps.
- `Examples/ExternalMaps/example_map.json`: Sample external map data for merging/override, also includes examples of direct and warp connections.
- `Examples/ExternalMaps/example_forest.json`: Example of an external forest map.

See the API Reference for details on map connection types and data structure fields.

## Example TMX Layout Files

- `Examples/Resources/InternalMaps/`: Contains TMX files for internal map layouts (e.g., `LAYOUT_ORIGINAL_TEST_DOWN.tmx`, `LAYOUT_INTERNAL_START_TOWN.tmx`, `LAYOUT_INTERNAL_HOUSE.tmx`).
- `Examples/Resources/ExternalMaps/`: Contains TMX files for external map layouts (e.g., `LAYOUT_OVERWRITTEN_OVERWRITE_TEST.tmx`).

These TMX files are referenced by the example map JSON files and demonstrate how to structure map layouts for use with the framework.

## Example Lua Script

- `Examples/Scripts/example_event.lua`: Example Lua script for map events.


See the main README for more details on how to use these files in your project.
