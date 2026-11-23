-- luacheck settings: Lua 5.1 plus the globals Mudlet and Geyser provide (only the ones the package uses).
std = "lua51"
max_line_length = 240
globals = { "Hollowmere" }
read_globals = {
  "gmcp", "cecho", "Geyser", "tempTimer", "sendGMCP",
  "registerAnonymousEventHandler", "killAnonymousEventHandler",
  "addRoom", "roomExists", "setRoomName", "setRoomArea", "addAreaName", "getAreaTable", "getAreaRooms", "getRoomArea",
  "setRoomCoordinates", "getRoomCoordinates", "getRoomsByPosition", "setRoomEnv",
  "setExit", "setExitStub", "addSpecialExit", "getSpecialExitsSwap", "setDoor", "centerview", "updateMap",
  "getMainWindowSize", "setBorderBottom",
}

-- The stand-in for Mudlet defines Mudlet's functions as globals on purpose.
files["test/stub.lua"] = { ignore = { "1" } }
files["test/map_test.lua"] = { ignore = { "1" } }
exclude_files = { "test/village_fixture.lua" }
