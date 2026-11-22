-- Hollowmere map: builds the Mudlet map from the game's GMCP Room.Info and follows the player.
-- Uses only documented Mudlet functions: registerAnonymousEventHandler, killAnonymousEventHandler, addRoom, roomExists,
-- setRoomName, setRoomArea, addAreaName, getAreaTable, setRoomCoordinates, getRoomCoordinates, getRoomsByPosition,
-- setRoomEnv, setExit, setExitStub, addSpecialExit, getSpecialExitsSwap, setDoor, centerview, updateMap, sendGMCP, tempTimer.
-- Needs layout.lua (loaded before this file). Written for Lua 5.1.

Hollowmere = Hollowmere or {}
local H = Hollowmere
local L = H.layout

H.map = H.map or {}
local M = H.map

-- What the script remembers between messages (not saved: the map itself is Mudlet's, and is saved with the profile).
M.current = nil      -- number of the room the player is in
M.exitsOf = {}       -- room number -> the exits table of the last Room.Info for it
M.pending = {}       -- target room number -> list of { from = room, name = exit name } to link when the target is first seen

local function say(text)
  if cecho then cecho("\n<grey>[Hollowmere map] " .. text .. "\n") end
end
M.say = say

local function validInfo(info)
  if type(info) ~= "table" then return false end
  if type(info.num) ~= "number" or info.num < 1 or info.num ~= math.floor(info.num) then return false end
  if type(info.name) ~= "string" then return false end
  if type(info.exits) ~= "table" then return false end
  return true
end
M.validInfo = validInfo

-- The id of an area by name, made if it is new.
function M.areaId(name)
  if type(name) ~= "string" or name == "" then name = "Elsewhere" end
  local table_ = getAreaTable() or {}
  local id = table_[name]
  if id then return id end
  id = addAreaName(name)
  if type(id) == "number" then return id end
  table_ = getAreaTable() or {}
  return table_[name]
end

-- Is some room already standing at this spot of this area?
local function occupiedIn(areaId)
  return function(x, y, z)
    local rooms = getRoomsByPosition(areaId, x, y, z)
    if type(rooms) ~= "table" then return false end
    for _, v in pairs(rooms) do
      if v then return true end
    end
    return false
  end
end

-- The largest x of the rooms on level 0 of the area, or nil if the area has none.
local function extentIn(areaId)
  return function()
    local rooms = getAreaRooms(areaId)
    if type(rooms) ~= "table" then return nil end
    local best
    for _, id in pairs(rooms) do
      local x = getRoomCoordinates(id)
      if x and (not best or x > best) then best = x end
    end
    return best
  end
end

-- Link room `from` to room `to` by the exit called `name`: an ordinary exit for a compass direction, a special exit
-- (a command the player types) for anything else.
local function link(from, to, name)
  local c = L.direction(name)
  if c then
    setExit(from, to, c.number)
    setExitStub(from, c.number, false) -- the stub made earlier for this exit, if any, is not needed now
  else
    local specials = getSpecialExitsSwap(from) or {}
    if specials[name] ~= to then addSpecialExit(from, to, name) end
  end
end

-- An exit whose room is not on the map yet: a stub (for a compass exit) so Mudlet draws the way, and a note to link it later.
local function leaveStub(from, name, to)
  local c = L.direction(name)
  if c then setExitStub(from, c.number, true) end
  M.pending[to] = M.pending[to] or {}
  for _, p in ipairs(M.pending[to]) do
    if p.from == from and p.name == name then return end
  end
  table.insert(M.pending[to], { from = from, name = name })
end

local function applyDoors(num, doors, exits)
  if type(doors) ~= "table" then return end
  for name, state in pairs(doors) do
    local status = L.doorStatus[state]
    local target = exits[name]
    if status and target then
      local c = L.direction(name)
      -- a compass exit exists (or is a stub) already; a named exit exists only once its room is on the map
      if c then setDoor(num, c.short, status) elseif roomExists(target) then setDoor(num, name, status) end
    end
  end
end

-- Handle one Room.Info table. Returns the room number it put the player in, or nil if the message was not usable.
function M.onRoomInfo(info)
  if not validInfo(info) then return nil end
  local num = info.num
  local previous = M.current
  local isNew = not roomExists(num)

  if isNew then
    local area = M.areaId(info.area)
    addRoom(num)
    setRoomName(num, info.name)
    setRoomArea(num, area)
    local from, direction
    if previous and roomExists(previous) then
      local fx, fy, fz = getRoomCoordinates(previous)
      -- only when we came from a room of the same area; a jump into another area starts its own sheet
      if fx and getRoomArea(previous) == area then
        direction = L.exitTo(M.exitsOf[previous], num)
        -- no exit of the old room leads here: a jump (a teleport, a flight, a respawn), so there is no neighbour to lay out from
        if direction then from = { fx, fy, fz } end
      end
    end
    local x, y, z = L.place(from, direction, occupiedIn(area), extentIn(area))
    setRoomCoordinates(num, x, y, z)
  end
  local env = L.environment[info.environment]
  if env then setRoomEnv(num, env) end

  -- This room's exits: to rooms we know, linked now; to rooms we do not know yet, left as stubs and linked when they appear.
  for name, target in pairs(info.exits) do
    if type(name) == "string" and type(target) == "number" then
      if roomExists(target) then link(num, target, name) else leaveStub(num, name, target) end
    end
  end
  -- Rooms that were waiting for this one.
  local waiting = M.pending[num]
  if waiting then
    M.pending[num] = nil
    for _, p in ipairs(waiting) do
      if roomExists(p.from) then
        link(p.from, num, p.name)
        local doors = M.doorsOf and M.doorsOf[p.from]
        if doors then applyDoors(p.from, doors, M.exitsOf[p.from] or {}) end
      end
    end
  end
  M.exitsOf[num] = info.exits
  M.doorsOf = M.doorsOf or {}
  M.doorsOf[num] = info.doors
  applyDoors(num, info.doors, info.exits)

  M.current = num
  centerview(num)
  if updateMap then updateMap() end
  return num
end

local function onRoomInfoEvent()
  local ok, err = pcall(M.onRoomInfo, gmcp and gmcp.Room and gmcp.Room.Info)
  if not ok then say("could not use Room.Info: " .. tostring(err)) end
end

-- Tell the game which GMCP packages this package wants. Safe to send twice.
function M.sendSupports()
  if sendGMCP then sendGMCP('Core.Supports.Add ["Room 1", "Char 1", "Comm.Channel 1"]') end
end

function M.install()
  if M.handlers then
    for _, id in ipairs(M.handlers) do killAnonymousEventHandler(id) end
  end
  M.handlers = {
    registerAnonymousEventHandler("gmcp.Room.Info", onRoomInfoEvent),
    registerAnonymousEventHandler("sysProtocolEnabled", function(_, protocol)
      if protocol == "GMCP" then M.sendSupports() end
    end),
  }
  if tempTimer then tempTimer(1, M.sendSupports) end
end

M.install()
