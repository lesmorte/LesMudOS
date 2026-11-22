-- Tests of the Hollowmere layout and map logic without Mudlet (the API is stubbed in stub.lua).
-- Run from clients/mudlet:   lua test/map_test.lua      (Lua 5.1 or newer; also LuaJIT)
local here = arg and arg[0] and arg[0]:match("^(.*)/test/[^/]*$") or "."
package.path = here .. "/test/?.lua;" .. package.path

local S = dofile(here .. "/test/stub.lua")
dofile(here .. "/src/layout.lua")
dofile(here .. "/src/map.lua")
dofile(here .. "/src/gauges.lua")

local H = Hollowmere
local L = H.layout
local passed, failed = 0, 0

local function check(label, condition, detail)
  if condition then
    passed = passed + 1
    print("PASS  " .. label)
  else
    failed = failed + 1
    print("FAIL  " .. label .. (detail and ("  " .. tostring(detail)) or ""))
  end
end

local function pos(id) local r = S.rooms[id] return r.x .. "," .. r.y .. "," .. r.z end

-- ---------- layout.lua alone ----------
check("compass: north is one step up the grid", L.direction("north").dy == 1 and L.direction("n").short == "n")
check("compass: a named exit has no direction", L.direction("lane") == nil and L.direction("in") == nil and L.direction("out") == nil and L.direction("enter") == nil)
local taken = { ["0,1,0"] = true }
local function occ(x, y, z) return taken[x .. "," .. y .. "," .. z] end
local x, y = L.free(occ, 0, 1, 0)
check("free: a taken spot gives the nearest free one, not the spot", not (x == 0 and y == 1) and math.abs(x) <= 1 and math.abs(y - 1) <= 1, x .. "," .. y)
local px, py, pz = L.place({ 5, 5, 0 }, "east", function() return false end, nil)
check("place: east of 5,5 is 6,5", px == 6 and py == 5 and pz == 0)
px, py, pz = L.place({ 5, 5, 0 }, "up", function() return false end, nil)
check("place: up is one level up", px == 5 and py == 5 and pz == 1)
px, py, pz = L.place({ 5, 5, 0 }, "lane", function() return false end, nil)
check("place: a named exit is beside the old room, not on it", not (px == 5 and py == 5) and math.abs(px - 5) <= 1 and math.abs(py - 5) <= 1 and pz == 0, px .. "," .. py)
px, py, pz = L.place(nil, nil, function() return false end, function() return nil end)
check("place: the first room of an area is 0,0,0", px == 0 and py == 0 and pz == 0)
px, py, pz = L.place(nil, nil, function() return false end, function() return 7 end)
check("place: a room with no neighbour goes to the right of the area", px == 9 and py == 0 and pz == 0, px .. "," .. py)
-- Fill a 9 x 9 block and ask for a spot in the middle: still finds a free one, never an occupied one.
local block = {}
for bx = -4, 4 do for by = -4, 4 do block[bx .. "," .. by .. ",0"] = true end end
px, py = L.place({ 0, 0, 0 }, "lane", function(a, b, c) return block[a .. "," .. b .. "," .. c] end, nil)
check("place: with a full block around, a named exit still lands on a free spot", px ~= nil and not block[px .. "," .. py .. ",0"], px .. "," .. py)
check("exitTo: prefers a compass exit, deterministic", L.exitTo({ lane = 5, north = 5, alley = 5 }, 5) == "north" and L.exitTo({ lane = 5, alley = 5 }, 5) == "alley" and L.exitTo({ a = 1 }, 9) == nil)

-- ---------- map.lua: a small world like the game's ----------
-- Village Square (1): north 2 (Gate), east 3, lane 9 (Fletcher's Lane, named), and a shed (in) 10.
-- The messages are as the game sends them: exits is a table direction -> number, doors direction -> state.
local function info(num, name, area, exits, doors, env)
  return { num = num, name = name, area = area or "The Village of Vincent's Hollow", environment = env or "outdoors", exits = exits, doors = doors or {} }
end

S.room(info(1, "Village Square", nil, { north = 2, east = 3, lane = 9, ["in"] = 10, south = 5 }))
check("the first room is made, named, in its area, at 0,0,0", S.rooms[1] and S.rooms[1].name == "Village Square" and pos(1) == "0,0,0" and S.areaNames[S.rooms[1].area] == "The Village of Vincent's Hollow")
check("the player is followed (centerview)", S.centered == 1)
check("an exit to a room not seen yet is a stub", S.rooms[1].stubs[1] == true and S.rooms[1].stubs[4] == true and S.rooms[1].stubs[6] == true)
check("a named exit to a room not seen yet is not a special exit yet", next(S.rooms[1].special) == nil)

S.room(info(2, "North Gate", nil, { south = 1, north = 20 }))
check("walking north puts the new room one step north", pos(2) == "0,1,0", pos(2))
check("the exit north of the square is now a real exit, and the stub is gone", S.rooms[1].exits[1] == 2 and S.rooms[1].stubs[1] == nil)
check("the gate's way back south to the square is linked", S.rooms[2].exits[6] == 1)

S.room(info(1, "Village Square", nil, { north = 2, east = 3, lane = 9, ["in"] = 10, south = 5 }))
check("going back changes no coordinates", pos(1) == "0,0,0" and pos(2) == "0,1,0")
S.room(info(3, "East Road", nil, { west = 1 }))
check("east puts the room one step east", pos(3) == "1,0,0", pos(3))

-- Named exits: the lane. The new room goes beside the square, on a free spot, and the exit is a special exit.
S.room(info(1, "Village Square", nil, { north = 2, east = 3, lane = 9, ["in"] = 10, south = 5 }))
S.room(info(9, "Fletcher's Lane", nil, { out = 1 }))
local lane = S.rooms[9]
check("the lane room is not on top of any other room", (function()
  for id, r in pairs(S.rooms) do if id ~= 9 and r.x == lane.x and r.y == lane.y and r.z == lane.z then return false end end
  return true
end)(), pos(9))
check("the lane room is beside the square (at most one step away)", math.abs(lane.x) <= 1 and math.abs(lane.y) <= 1, pos(9))
check("the square's lane exit is a special exit with the command lane", S.rooms[1].special["lane"] == 9)
check("the lane's out is a special exit back with the command out", S.rooms[9].special["out"] == 1)

S.room(info(1, "Village Square", nil, { north = 2, east = 3, lane = 9, ["in"] = 10, south = 5 }))
S.room(info(10, "A Tool Shed", nil, { out = 1 }, nil, "indoors"))
check("a shed through 'in' is a special exit and a free spot beside the square", S.rooms[1].special["in"] == 10 and pos(10) ~= pos(9) and pos(10) ~= pos(1) and pos(10) ~= pos(2) and pos(10) ~= pos(3))
check("environment is set from the game's word", S.rooms[10].env == L.environment.indoors and S.rooms[1].env == L.environment.outdoors)

-- No two rooms share a spot, whatever happened.
local seen, clash = {}, nil
for id, r in pairs(S.rooms) do
  local key = r.area .. ":" .. pos(id)
  if seen[key] then clash = key end
  seen[key] = id
end
check("no two rooms on the map share a spot", clash == nil, clash)

-- A room reached with no exit of the old room leading to it (a teleport, a respawn) is not laid out from it.
S.room(info(77, "Somewhere Else", nil, { west = 78 }))
local rightmost = -99
for id, r in pairs(S.rooms) do if id ~= 77 and r.area == S.rooms[77].area and r.x > rightmost then rightmost = r.x end end
check("a jump puts the room to the right of everything in the area, away from the old room", S.rooms[77].x > rightmost, pos(77))

-- Pending links: a room that was left as a stub is linked when the other side arrives (both ways as learned).
S.room(info(78, "Westwatch", nil, { east = 77 }))
check("the stub west of room 77 became an exit when 78 appeared", S.rooms[77].exits[5] == 78 and S.rooms[77].stubs[5] == nil)
check("room 78 is one step west of 77", S.rooms[78].x == S.rooms[77].x - 1 and S.rooms[78].y == S.rooms[77].y, pos(78))
check("and 78's way east to 77 is linked", S.rooms[78].exits[4] == 77)

-- A room in another area starts its own sheet at 0,0,0.
S.room(info(500, "Cellar Steps", "The Forgotten Cellars", { up = 77 }, nil, "underground"))
check("the first room of a new area is at 0,0,0 in that area", pos(500) == "0,0,0" and S.areaNames[S.rooms[500].area] == "The Forgotten Cellars")

-- A special exit whose target appears later is linked then.
S.room(info(11, "Hall", nil, { enter = 12 }))
S.room(info(12, "Inside", nil, { out = 11 }))
check("a named exit to a room that was unknown is a special exit once that room exists", S.rooms[11].special["enter"] == 12 and S.rooms[12].special["out"] == 11)

-- Doors.
S.room(info(20, "Door Room", nil, { east = 21, lane = 22 }, { east = "closed", lane = "locked" }))
check("a door on a compass exit is set with the short direction", S.rooms[20].doors["e"] == 2)
check("a door on a named exit whose room is unknown yet is not set (known limit)", S.rooms[20].doors["lane"] == nil)
S.room(info(20, "Door Room", nil, { east = 21, lane = 22 }, { east = "open", lane = "locked" }))
check("the door's state is updated when it changes", S.rooms[20].doors["e"] == 1)
S.room(info(22, "Lane End", nil, { out = 20 }))
S.room(info(20, "Door Room", nil, { east = 21, lane = 22 }, { east = "open", lane = "locked" }))
check("a door on a named exit is set once that room is on the map", S.rooms[20].doors["lane"] == 3)

-- Bad input does nothing and does not raise.
local before = 0
for _ in pairs(S.rooms) do before = before + 1 end
H.map.onRoomInfo(nil)
H.map.onRoomInfo("text")
H.map.onRoomInfo({ num = "x", name = "n", exits = {} })
H.map.onRoomInfo({ num = -4, name = "n", exits = {} })
H.map.onRoomInfo({ num = 1.5, name = "n", exits = {} })
H.map.onRoomInfo({ num = 5, name = "n" })
S.setGmcp("Room.Info", 5)
S.fire("gmcp.Room.Info")
local after = 0
for _ in pairs(S.rooms) do after = after + 1 end
check("a malformed Room.Info makes no room and raises nothing", before == after)

-- Registering again does not double the handlers.
local count = 0
for _, h in pairs(S.handlers) do if h.event == "gmcp.Room.Info" then count = count + 1 end end
H.map.install()
local count2 = 0
for _, h in pairs(S.handlers) do if h.event == "gmcp.Room.Info" then count2 = count2 + 1 end end
check("installing twice leaves one Room.Info handler", count == 1 and count2 == 1, count .. "," .. count2)
check("the package asked the game for Room, Char and Comm.Channel", S.sent[1] and S.sent[1]:find("Room 1", 1, true) and S.sent[1]:find("Char 1", 1, true))

-- ---------- gauges ----------
S.setGmcp("Char.Vitals", { vitality = 23, maxvitality = 30, endurance = 60, maxendurance = 70, focus = 70, maxfocus = 70, resting = true, sneaking = false, fighting = false })
S.fire("gmcp.Char.Vitals")
local g = S.gauges
check("three gauges are made", g and g.Hollowmere_vitality and g.Hollowmere_endurance and g.Hollowmere_focus)
check("the Vitality gauge shows 23 of 30 and says resting", g.Hollowmere_vitality.now == 23 and g.Hollowmere_vitality.max == 30 and g.Hollowmere_vitality.text == "Vitality 23/30 (resting)", g.Hollowmere_vitality.text)
check("the Endurance and Focus gauges show their numbers", g.Hollowmere_endurance.now == 60 and g.Hollowmere_focus.text == "Focus 70/70")
S.setGmcp("Char.Vitals", { vitality = 5, maxvitality = 30, endurance = 60, maxendurance = 70, focus = 70, maxfocus = 70, resting = false, sneaking = true, fighting = true })
S.fire("gmcp.Char.Vitals")
check("the gauge text follows the state", g.Hollowmere_vitality.text == "Vitality 5/30 (sneaking, fighting)", g.Hollowmere_vitality.text)
S.setGmcp("Char.Vitals", { vitality = "x", maxvitality = 0 })
S.fire("gmcp.Char.Vitals")
check("a malformed Char.Vitals changes nothing and raises nothing", g.Hollowmere_vitality.now == 5)

-- ---------- real messages: a walk of the village captured from the game (test/village_fixture.lua) ----------
do
  local ok, frames = pcall(dofile, here .. "/test/village_fixture.lua")
  if not ok then
    check("the captured village walk loads", false, frames)
  else
    -- a fresh map: forget everything the earlier checks made
    for id in pairs(S.rooms) do S.rooms[id] = nil end
    S.areas, S.areaNames, S.nextArea = {}, {}, 1
    H.map.current, H.map.exitsOf, H.map.pending, H.map.doorsOf = nil, {}, {}, {}
    for _, frame in ipairs(frames) do S.room(frame) end
    local rooms, names = 0, {}
    for _, frame in ipairs(frames) do names[frame.num] = frame end
    local missing, wrongName, unlinked, specials, overlap = nil, nil, nil, 0, nil
    local spots = {}
    for num, frame in pairs(names) do
      rooms = rooms + 1
      local r = S.rooms[num]
      if not r then missing = num else
        if r.name ~= frame.name then wrongName = num end
        local key = r.area .. ":" .. pos(num)
        if spots[key] then overlap = key .. " " .. num .. " " .. spots[key] end
        spots[key] = num
        for name, target in pairs(frame.exits) do
          if S.rooms[target] then
            local c = L.direction(name)
            local linked = c and r.exits[c.number] == target or (not c and r.special[name] == target)
            if not linked then unlinked = num .. " " .. name end
            if not c then specials = specials + 1 end
          end
        end
      end
    end
    check("the real walk made every room it visited (" .. rooms .. ")", rooms >= 10 and missing == nil, missing)
    check("with the game's names", wrongName == nil, wrongName)
    check("no two real rooms share a spot", overlap == nil, overlap)
    check("every exit to a room on the map is linked, by exit or by special exit", unlinked == nil, unlinked)
    check("the real walk had named exits and they became special exits", specials > 0, specials)
    check("the player ends in the last room of the walk", S.centered == frames[#frames].num)
  end
end

print(string.format("\n%d checks, %d failure(s)", passed + failed, failed))
os.exit(failed == 0 and 0 or 1)
