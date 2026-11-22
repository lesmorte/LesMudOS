-- A stand-in for the parts of Mudlet's API the package uses, so the layout and map logic can be run and tested without Mudlet.
-- It models only what the package needs: rooms with names, areas, coordinates, exits, stubs, special exits and doors.
-- It is NOT Mudlet: behaviour here is what Mudlet's documentation says, and has not been checked against a real Mudlet.

local S = { rooms = {}, areas = {}, areaNames = {}, nextArea = 1, handlers = {}, nextHandler = 1, calls = {}, centered = nil, sent = {} }

local function note(name, ...) S.calls[#S.calls + 1] = { name, ... } end

function addRoom(id) if S.rooms[id] then return false end S.rooms[id] = { id = id, exits = {}, stubs = {}, special = {}, doors = {} } note("addRoom", id) return true end
function roomExists(id) return S.rooms[id] ~= nil end
function setRoomName(id, name) S.rooms[id].name = name return true end
function setRoomArea(id, area) S.rooms[id].area = area return true end
function getRoomArea(id) return S.rooms[id] and S.rooms[id].area end
function setRoomEnv(id, env) S.rooms[id].env = env return true end
function addAreaName(name)
  if S.areas[name] then return nil, "area name already exists" end
  local id = S.nextArea
  S.nextArea = S.nextArea + 1
  S.areas[name] = id
  S.areaNames[id] = name
  return id
end
function getAreaTable() local copy = {} for k, v in pairs(S.areas) do copy[k] = v end return copy end
function getAreaRooms(area) local out = {} for id, r in pairs(S.rooms) do if r.area == area then out[#out + 1] = id end end return out end
function setRoomCoordinates(id, x, y, z) S.rooms[id].x, S.rooms[id].y, S.rooms[id].z = x, y, z return true end
function getRoomCoordinates(id) local r = S.rooms[id] return r.x, r.y, r.z end
function getRoomsByPosition(area, x, y, z)
  local out = {}
  for id, r in pairs(S.rooms) do if r.area == area and r.x == x and r.y == y and r.z == z then out[#out + 1] = id end end
  return out
end
function setExit(from, to, dir) S.rooms[from].exits[dir] = to return true end
function setExitStub(from, dir, on) S.rooms[from].stubs[dir] = on and true or nil return true end
function addSpecialExit(from, to, command) S.rooms[from].special[command] = to return true end
function getSpecialExitsSwap(from) local copy = {} for k, v in pairs(S.rooms[from].special) do copy[k] = v end return copy end
function setDoor(id, command, status) S.rooms[id].doors[command] = status return true end
function centerview(id) S.centered = id end
function updateMap() end
function cecho() end
function sendGMCP(text) S.sent[#S.sent + 1] = text end
function tempTimer(_, fn) fn() end
function registerAnonymousEventHandler(event, fn)
  local id = S.nextHandler
  S.nextHandler = S.nextHandler + 1
  S.handlers[id] = { event = event, fn = fn }
  return id
end
function killAnonymousEventHandler(id) S.handlers[id] = nil return true end
function getMainWindowSize() return 800, 600 end
function setBorderBottom(n) S.border = n end

-- Geyser: containers and gauges that remember what they were told
Geyser = { Container = {}, Gauge = {} }
function Geyser.Container:new(cons) local o = { cons = cons } setmetatable(o, { __index = self }) return o end
function Geyser.Gauge:new(cons, parent)
  local o = { cons = cons, parent = parent, value = nil }
  setmetatable(o, { __index = self })
  S.gauges = S.gauges or {}
  S.gauges[cons.name] = o
  return o
end
function Geyser.Gauge:setColor(r, g, b) self.color = { r, g, b } end
function Geyser.Gauge:setValue(now, max, text) self.now, self.max, self.text = now, max, text end

-- Fire the handlers registered for an event, with `gmcp` set as Mudlet sets it.
function S.fire(event, ...)
  for _, h in pairs(S.handlers) do if h.event == event then h.fn(event, ...) end end
end
function S.setGmcp(path, value)
  gmcp = gmcp or {}
  local node = gmcp
  local keys = {}
  for k in string.gmatch(path, "[^.]+") do keys[#keys + 1] = k end
  for i = 1, #keys - 1 do node[keys[i]] = node[keys[i]] or {} node = node[keys[i]] end
  node[keys[#keys]] = value
end
function S.room(info)
  S.setGmcp("Room.Info", info)
  S.fire("gmcp.Room.Info")
end

return S
