-- Hollowmere map layout: pure Lua, no Mudlet calls, so it can be tested outside Mudlet (clients/mudlet/test).
-- Written for Lua 5.1 (what Mudlet embeds): no goto, no integer division, no bit operators.

Hollowmere = Hollowmere or {}
local H = Hollowmere

H.layout = {}
local L = H.layout

-- The ten directions that have a place on the map: the server's names, the short names Mudlet's setDoor and setExit use,
-- the number Mudlet's setExit uses, and the step on the grid. y grows to the north, as in Mudlet.
L.compass = {
  north     = { short = "n",  number = 1,  dx = 0,  dy = 1,  dz = 0,  opposite = "south" },
  northeast = { short = "ne", number = 2,  dx = 1,  dy = 1,  dz = 0,  opposite = "southwest" },
  northwest = { short = "nw", number = 3,  dx = -1, dy = 1,  dz = 0,  opposite = "southeast" },
  east      = { short = "e",  number = 4,  dx = 1,  dy = 0,  dz = 0,  opposite = "west" },
  west      = { short = "w",  number = 5,  dx = -1, dy = 0,  dz = 0,  opposite = "east" },
  south     = { short = "s",  number = 6,  dx = 0,  dy = -1, dz = 0,  opposite = "north" },
  southeast = { short = "se", number = 7,  dx = 1,  dy = -1, dz = 0,  opposite = "northwest" },
  southwest = { short = "sw", number = 8,  dx = -1, dy = -1, dz = 0,  opposite = "northeast" },
  up        = { short = "u",  number = 9,  dx = 0,  dy = 0,  dz = 1,  opposite = "down" },
  down      = { short = "d",  number = 10, dx = 0,  dy = 0,  dz = -1, opposite = "up" },
}

-- Names the game's exits may also be typed with; the server sends the long names, so these are only for robustness.
L.alias = { n = "north", ne = "northeast", nw = "northwest", e = "east", w = "west", s = "south", se = "southeast", sw = "southwest", u = "up", d = "down" }

-- The compass entry for an exit name, or nil if it is a named exit ("lane", "shed", "in", "out", "enter") that has no place on the grid.
function L.direction(name)
  if type(name) ~= "string" then return nil end
  return L.compass[name] or L.compass[L.alias[name] or ""]
end

-- Where the slots around a spot are tried, nearest first: the four beside it, then the diagonals, then the next ring out.
-- Ring r holds every (dx, dy) whose larger coordinate is r, in a fixed order, so the same map always lays out the same way.
function L.ring(r)
  local out = {}
  if r == 0 then
    out[1] = { 0, 0 }
    return out
  end
  -- the four squares straight out first, so "beside" is tidy
  out[#out + 1] = { r, 0 }
  out[#out + 1] = { -r, 0 }
  out[#out + 1] = { 0, -r }
  out[#out + 1] = { 0, r }
  for d = 1, r do
    for _, s in ipairs({ { r, -d }, { r, d }, { -r, -d }, { -r, d }, { -d, -r }, { d, -r }, { -d, r }, { d, r } }) do
      if not (s[1] == 0 or s[2] == 0) or d < r then
        local dup = false
        for _, e in ipairs(out) do
          if e[1] == s[1] and e[2] == s[2] then dup = true end
        end
        if not dup then out[#out + 1] = s end
      end
    end
  end
  return out
end

-- The first free spot at or around (x, y, z). occupied(x, y, z) says whether a room already stands there.
-- Searches outwards on the same level; gives up (returns nil) after maxRadius rings.
function L.free(occupied, x, y, z, maxRadius)
  maxRadius = maxRadius or 12
  for r = 0, maxRadius do
    for _, step in ipairs(L.ring(r)) do
      local px, py = x + step[1], y + step[2]
      if not occupied(px, py, z) then return px, py, z end
    end
  end
  return nil
end

-- Where a new room goes. from is {x, y, z} of the room the player came from (or nil: first room, or a jump with no known source);
-- direction is the exit name that was taken (or nil). occupied(x, y, z) says whether a spot is taken; extent() returns the largest x
-- used on this level (or nil if the area is empty), for a room with no known neighbour.
--   - A compass exit puts the room one step that way; if that spot is taken, the nearest free spot to it.
--   - A named exit (lane, shed, in, out, enter) puts the room beside the old one, on the same level: the nearest free spot to it.
--   - No source: the first room of an area is at 0, 0, 0; otherwise to the right of everything already in the area.
function L.place(from, direction, occupied, extent)
  if not from then
    local right = extent and extent()
    if right == nil then return 0, 0, 0 end
    local x, y, z = L.free(occupied, right + 2, 0, 0)
    return x or right + 2, y or 0, z or 0
  end
  local fx, fy, fz = from[1], from[2], from[3]
  local c = L.direction(direction)
  local x, y, z
  if c then
    x, y, z = L.free(occupied, fx + c.dx, fy + c.dy, fz + c.dz)
  else
    -- beside the old room, never on it
    for r = 1, 12 do
      for _, step in ipairs(L.ring(r)) do
        if not occupied(fx + step[1], fy + step[2], fz) then
          x, y, z = fx + step[1], fy + step[2], fz
          break
        end
      end
      if x then break end
    end
  end
  if x then return x, y, z end
  return fx + 20, fy, fz -- nowhere near free: far to the side rather than on top of anything
end

-- The exit of room `from` (a table of exit name -> room number) that leads to `target`, preferring a compass exit, then the
-- alphabetically first named one, so that the answer does not depend on table order. nil if none.
function L.exitTo(exits, target)
  local best, bestCompass
  for name, number in pairs(exits or {}) do
    if number == target then
      local isCompass = L.direction(name) ~= nil
      if not best or (isCompass and not bestCompass) or (isCompass == bestCompass and name < best) then
        best, bestCompass = name, isCompass
      end
    end
  end
  return best
end

-- Mudlet's colour classes for room environments (setRoomEnv).
L.environment = { outdoors = 2, indoors = 3, underground = 8, road = 7, water = 4 }

-- Doors: the game's word to Mudlet's setDoor status (0 none, 1 open, 2 closed, 3 locked).
L.doorStatus = { open = 1, closed = 2, locked = 3 }
