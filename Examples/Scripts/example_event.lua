-- Example Lua event script for MapLoaderFramework

function onPlayerEnter()
    print("Welcome to the Internal Forest!")
    -- Trigger background music or other logic here
end

function onFindItem(itemId)
    print("You found an item: " .. itemId)
    -- Add item to inventory or trigger SFX
end
