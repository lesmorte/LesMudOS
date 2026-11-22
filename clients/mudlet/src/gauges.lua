-- Hollowmere gauges: Vitality, Endurance and Focus, drawn from the game's GMCP Char.Vitals with Geyser gauges at the bottom of
-- the main window. Uses Geyser.Container, Geyser.Gauge (:setValue, :setColor), getMainWindowSize, setBorderBottom,
-- registerAnonymousEventHandler, killAnonymousEventHandler. Written for Lua 5.1.

Hollowmere = Hollowmere or {}
local H = Hollowmere

H.gauges = H.gauges or {}
local G = H.gauges

G.height = 22

-- name, the field names in Char.Vitals, the label, the colour of the bar
G.rows = {
  { key = "vitality",  now = "vitality",  max = "maxvitality",  label = "Vitality",  color = { 190, 40, 40 } },
  { key = "endurance", now = "endurance", max = "maxendurance", label = "Endurance", color = { 200, 170, 40 } },
  { key = "focus",     now = "focus",     max = "maxfocus",     label = "Focus",     color = { 50, 90, 200 } },
}

-- The text on the Vitality bar says what the character is doing.
function G.stateText(vitals)
  local parts = {}
  if vitals.resting then parts[#parts + 1] = "resting" end
  if vitals.sneaking then parts[#parts + 1] = "sneaking" end
  if vitals.fighting then parts[#parts + 1] = "fighting" end
  if #parts == 0 then return "" end
  return " (" .. table.concat(parts, ", ") .. ")"
end

function G.text(row, vitals)
  local text = row.label .. " " .. tostring(vitals[row.now]) .. "/" .. tostring(vitals[row.max])
  if row.key == "vitality" then text = text .. G.stateText(vitals) end
  return text
end

-- Make the gauges, once (a script that is loaded again reuses them).
function G.create()
  if G.built then return end
  local _, windowHeight = getMainWindowSize()
  local total = G.height * #G.rows
  setBorderBottom(total)
  G.container = Geyser.Container:new({ name = "HollowmereGauges", x = 0, y = windowHeight - total, width = "100%", height = total })
  G.bars = {}
  for i, row in ipairs(G.rows) do
    local bar = Geyser.Gauge:new({ name = "Hollowmere_" .. row.key, x = 0, y = (i - 1) * G.height, width = "100%", height = G.height }, G.container)
    bar:setColor(row.color[1], row.color[2], row.color[3])
    bar:setValue(0, 1, row.label)
    G.bars[row.key] = bar
  end
  G.built = true
end

function G.onVitals(vitals)
  if type(vitals) ~= "table" then return end
  if not G.built then G.create() end
  for _, row in ipairs(G.rows) do
    local now, max = tonumber(vitals[row.now]), tonumber(vitals[row.max])
    if now and max and max > 0 then
      G.bars[row.key]:setValue(now, max, G.text(row, vitals))
    end
  end
end

local function onVitalsEvent()
  local ok, err = pcall(G.onVitals, gmcp and gmcp.Char and gmcp.Char.Vitals)
  if not ok and cecho then cecho("\n<grey>[Hollowmere gauges] " .. tostring(err) .. "\n") end
end

function G.install()
  if G.handler then killAnonymousEventHandler(G.handler) end
  G.handler = registerAnonymousEventHandler("gmcp.Char.Vitals", onVitalsEvent)
end

G.install()
