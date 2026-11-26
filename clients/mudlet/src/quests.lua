-- Hollowmere quest journal: a Quests panel on the right of the main window, filled only from the game's GMCP Char.Quests.List and
-- Char.Quests.Log (docs/specs/quest-journal.md, docs/specs/gmcp.md). Three sections: Active (a list; the chosen quest shows its
-- objective and progress above its conversation, newest at the bottom), Completed and Abandoned (a name, a date, the giver and a
-- small summary each). No trigger reads the game's text: what is shown is what the game keeps and sends, and it sends it again at
-- every login.
-- Mudlet functions and objects used: Geyser.Container:new, Geyser.Label:new (:setStyleSheet, :echo, :setClickCallback, :show,
-- :hide), Geyser.MiniConsole:new (:clear, :cecho, :setFontSize, :enableAutoWrap, :show, :hide), setBorderRight, getMainWindowSize,
-- registerAnonymousEventHandler, killAnonymousEventHandler, tempTimer, cecho; the events gmcp.Char.Quests.List,
-- gmcp.Char.Quests.Log, gmcp.Char.Status, sysDisconnectionEvent and the table gmcp. Needs gauges.lua (loaded before this file).
-- Written for Lua 5.1.

Hollowmere = Hollowmere or {}
local H = Hollowmere

H.quests = H.quests or {}
local Q = H.quests

Q.width = 380            -- the panel, in pixels, taken from the right of the main window
Q.tabHeight = 24
Q.rowHeight = 20
Q.pageSize = 6           -- quests listed at a time in Active (the buttons below page through more)
Q.waitSeconds = 5        -- after the game says who you are, how long to wait for a journal before saying the game does not send one
Q.maxEntries = 200       -- what is kept of one conversation here (the game keeps 80)

Q.tabs = { "active", "completed", "abandoned" }
Q.titles = { active = "Active", completed = "Completed", abandoned = "Abandoned" }
Q.states = { active = true, completed = true, abandoned = true }

-- What the panel knows: the last list, one log per quest, what is chosen. Nothing is saved; the game sends it all again at login.
function Q.reset()
  Q.data = { list = nil, quests = {}, order = {}, logs = {}, selected = nil, tab = "active", page = 1, note = nil, stale = false, waiting = false }
end
if not Q.data then Q.reset() end

local function say(text)
  if cecho then cecho("\n<grey>[Hollowmere quests] " .. text .. "\n") end
end
Q.say = say

-- cecho reads <word> as a colour: a "<" in the game's text is shown as a single angle quote instead.
local function plain(text)
  if type(text) ~= "string" then return "" end
  return (text:gsub("<", "\226\128\185"):gsub("[\1-\8\11-\31]", ""))
end
Q.plain = plain

local function validQuest(q)
  return type(q) == "table" and type(q.id) == "string" and q.id ~= "" and type(q.name) == "string" and Q.states[q.state] == true
end

local function validList(msg)
  if type(msg) ~= "table" or type(msg.quests) ~= "table" then return false end
  return true
end
Q.validList = validList

-- ---------- what the game says ----------

function Q.counts()
  local n = { active = 0, completed = 0, abandoned = 0 }
  for _, id in ipairs(Q.data.order) do
    local q = Q.data.quests[id]
    n[q.state] = n[q.state] + 1
  end
  return n
end

function Q.idsIn(state)
  local ids = {}
  for _, id in ipairs(Q.data.order) do
    if Q.data.quests[id].state == state then ids[#ids + 1] = id end
  end
  return ids
end

-- A new list replaces the old one. A quest that is no longer active loses its conversation; a chosen quest that is gone is unchosen.
function Q.onList(msg)
  if not validList(msg) then return false end
  local quests, order = {}, {}
  for _, q in ipairs(msg.quests) do
    if validQuest(q) and not quests[q.id] then
      quests[q.id] = q
      order[#order + 1] = q.id
    end
  end
  Q.data.quests, Q.data.order, Q.data.list = quests, order, msg
  Q.data.stale, Q.data.note, Q.data.waiting = false, nil, false
  for id in pairs(Q.data.logs) do
    if not quests[id] or quests[id].state ~= "active" then Q.data.logs[id] = nil end
  end
  local sel = Q.data.selected
  if not sel or not quests[sel] or quests[sel].state ~= "active" then
    Q.data.selected = Q.idsIn("active")[1]
  end
  Q.refresh()
  return true
end

-- A log message: the whole log (reset = true, then perhaps more in further messages), or entries to append. A hole in the sequence
-- numbers is shown in the conversation; the next reset (the game sends one at every login) puts it right.
function Q.onLog(msg)
  if type(msg) ~= "table" or type(msg.id) ~= "string" or type(msg.entries) ~= "table" then return false end
  local log = Q.data.logs[msg.id]
  if msg.reset == true or not log then
    log = { entries = {}, last = nil }
    Q.data.logs[msg.id] = log
  end
  for _, e in ipairs(msg.entries) do
    if type(e) == "table" and type(e.text) == "string" and type(e.seq) == "number" then
      if e.kind == "gap" then
        log.entries[#log.entries + 1] = e
      elseif not log.last or e.seq > log.last then
        if log.last and e.seq ~= log.last + 1 then
          log.entries[#log.entries + 1] = { seq = 0, kind = "hole", text = "(a line may be missing here)", date = "" }
        end
        log.entries[#log.entries + 1] = e
        log.last = e.seq
      end
    end
  end
  while #log.entries > Q.maxEntries do table.remove(log.entries, 1) end
  if msg.id == Q.data.selected then Q.refreshDetail() end
  return true
end

function Q.onStatus()
  -- The game has said who you are: if it has a journal it has sent it by now. If not, it is an older game.
  if Q.data.list or Q.data.waiting then return end
  Q.data.waiting = true
  tempTimer(Q.waitSeconds, function()
    Q.data.waiting = false
    if not Q.data.list then
      Q.data.note = "This game does not send a quest journal (Char.Quests). It is an older version of Legends of Hollowmere: type quests in the game to read yours."
      Q.refresh()
    end
  end)
end

function Q.onDisconnect()
  Q.data.stale = true
  Q.refresh()
end

-- ---------- what is shown (text for cecho; no Mudlet calls) ----------

local function colour(name, text) return "<" .. name .. ">" .. text .. "<reset>" end

local function dateLine(date)
  return colour("grey", "-- " .. plain(date) .. " --")
end

-- One line of a conversation: the speaker highlighted.
function Q.entryText(e)
  local text = plain(e.text)
  if e.kind == "npc" then return colour("cyan", plain(e.speaker)) .. ": " .. text end
  if e.kind == "you" then return colour("yellow", "You") .. ": " .. text end
  if e.kind == "gap" or e.kind == "hole" then return colour("grey", text) end
  return colour("green", "*") .. " " .. text
end

-- The objective and progress, then the conversation, newest at the bottom.
function Q.renderDetail(id)
  local q = Q.data.quests[id]
  if not q then return "" end
  local out = { colour("white", plain(q.name)) }
  local who = {}
  if q.giver and q.giver ~= "" then who[#who + 1] = "Given by " .. plain(q.giver) .. (q.area and q.area ~= "" and (" in " .. plain(q.area)) or "") .. "." end
  if q.started_date and q.started_date ~= "" then who[#who + 1] = "Taken on " .. plain(q.started_date) .. "." end
  if #who > 0 then out[#out + 1] = table.concat(who, " ") end
  if type(q.objectives) == "table" and #q.objectives > 0 then
    out[#out + 1] = colour("white", "Objective") .. (q.progress and q.progress ~= "" and (" (" .. plain(q.progress) .. " done)") or "")
    for _, o in ipairs(q.objectives) do
      local done = type(o.have) == "number" and type(o.need) == "number" and o.have >= o.need
      local counts = (type(o.need) == "number" and o.need > 1) and (" (" .. tostring(o.have) .. " of " .. tostring(o.need) .. ")") or ""
      out[#out + 1] = (done and colour("green", "[x]") or "[ ]") .. " " .. plain(o.text) .. counts
    end
  elseif q.objective and q.objective ~= "" then
    out[#out + 1] = colour("white", "Objective") .. ": " .. plain(q.objective)
  end
  out[#out + 1] = ""
  out[#out + 1] = colour("white", "Conversation")
  local log = Q.data.logs[id]
  if not log or #log.entries == 0 then
    out[#out + 1] = colour("grey", "Nothing has been said yet.")
  else
    local lastDate = nil
    for _, e in ipairs(log.entries) do
      if type(e.date) == "string" and e.date ~= "" and e.date ~= lastDate then
        out[#out + 1] = dateLine(e.date)
        lastDate = e.date
      end
      out[#out + 1] = Q.entryText(e)
    end
  end
  return table.concat(out, "\n") .. "\n"
end

-- Completed and Abandoned: a name, a date, the giver and the small summary, one quest after another.
function Q.renderSummaries(state)
  local out = {}
  for _, id in ipairs(Q.idsIn(state)) do
    local q = Q.data.quests[id]
    local head = colour("white", plain(q.name))
    if q.ended_date and q.ended_date ~= "" then head = head .. " - " .. plain(q.ended_date) end
    out[#out + 1] = head
    if q.giver and q.giver ~= "" then
      out[#out + 1] = colour("grey", "Given by " .. plain(q.giver) .. (q.area and q.area ~= "" and (" in " .. plain(q.area)) or "") .. ".")
    end
    if state == "completed" and q.reward and q.reward ~= "" then out[#out + 1] = "Reward: " .. plain(q.reward) .. "." end
    if q.summary and q.summary ~= "" then out[#out + 1] = plain(q.summary) end
    out[#out + 1] = ""
  end
  return table.concat(out, "\n")
end

function Q.tabText(state)
  return Q.titles[state] .. " (" .. tostring(Q.counts()[state]) .. ")"
end

-- What the body of the panel says when there is nothing to list, or why there is none.
function Q.emptyText(state)
  if Q.data.note then return Q.data.note end
  if not Q.data.list then return "Waiting for the game's quest journal." end
  local n = Q.counts()
  if n.active + n.completed + n.abandoned == 0 then return "No quests yet." end
  return "No " .. state .. " quests."
end

-- ---------- the panel (Mudlet calls) ----------

local tabStyle = "background-color: rgb(40,40,40); color: rgb(200,200,200); border: 1px solid rgb(90,90,90); padding-left: 4px;"
local tabOn = "background-color: rgb(70,70,100); color: white; border: 1px solid rgb(150,150,200); padding-left: 4px;"
local rowStyle = "background-color: rgb(25,25,25); color: rgb(220,220,220); padding-left: 4px;"
local rowOn = "background-color: rgb(60,60,90); color: white; padding-left: 4px;"

function Q.build()
  if Q.ui then return end
  local windowWidth, windowHeight = getMainWindowSize()
  local width = Q.width
  local gaugeHeight = (H.gauges and H.gauges.height or 22) * (H.gauges and #H.gauges.rows or 3)
  local height = math.max(200, windowHeight - gaugeHeight)
  setBorderRight(width)
  local ui = {}
  ui.box = Geyser.Container:new({ name = "HollowmereQuests", x = windowWidth - width, y = 0, width = width, height = height })
  ui.top = Geyser.Label:new({ name = "HollowmereQuestsTop", x = 0, y = 0, width = "100%", height = Q.tabHeight }, ui.box)
  ui.top:setStyleSheet(tabOn)
  ui.top:echo("Quests")
  ui.tabs = {}
  for i, state in ipairs(Q.tabs) do
    local label = Geyser.Label:new({ name = "HollowmereQuestsTab_" .. state, x = (i - 1) * (width / 3), y = Q.tabHeight, width = width / 3, height = Q.tabHeight }, ui.box)
    label:setClickCallback(function() Q.showTab(state) end)
    ui.tabs[state] = label
  end
  local bodyY = Q.tabHeight * 2
  ui.rows = {}
  for i = 1, Q.pageSize do
    local row = Geyser.Label:new({ name = "HollowmereQuestsRow" .. i, x = 0, y = bodyY + (i - 1) * Q.rowHeight, width = "100%", height = Q.rowHeight }, ui.box)
    ui.rows[i] = row
  end
  local pagerY = bodyY + Q.pageSize * Q.rowHeight
  ui.prev = Geyser.Label:new({ name = "HollowmereQuestsPrev", x = 0, y = pagerY, width = "25%", height = Q.rowHeight }, ui.box)
  ui.prev:setStyleSheet(tabStyle)
  ui.prev:echo("< Prev")
  ui.prev:setClickCallback(function() Q.page(-1) end)
  ui.next = Geyser.Label:new({ name = "HollowmereQuestsNext", x = "75%", y = pagerY, width = "25%", height = Q.rowHeight }, ui.box)
  ui.next:setStyleSheet(tabStyle)
  ui.next:echo("Next >")
  ui.next:setClickCallback(function() Q.page(1) end)
  ui.pageText = Geyser.Label:new({ name = "HollowmereQuestsPage", x = "25%", y = pagerY, width = "50%", height = Q.rowHeight }, ui.box)
  ui.pageText:setStyleSheet(rowStyle)
  local detailY = pagerY + Q.rowHeight
  ui.detail = Geyser.MiniConsole:new({ name = "HollowmereQuestsDetail", x = 0, y = detailY, width = "100%", height = height - detailY, color = "black", autoWrap = true }, ui.box)
  ui.summary = Geyser.MiniConsole:new({ name = "HollowmereQuestsSummary", x = 0, y = bodyY, width = "100%", height = height - bodyY, color = "black", autoWrap = true }, ui.box)
  for _, console in ipairs({ ui.detail, ui.summary }) do
    console:setFontSize(11)
    if console.enableAutoWrap then pcall(console.enableAutoWrap, console) end
  end
  Q.ui = ui
  Q.refresh()
end

local function show(widget, on)
  if not widget then return end
  if on then widget:show() else widget:hide() end
end

-- The rows of the Active list on the current page.
function Q.pageIds()
  local ids = Q.idsIn("active")
  local pages = math.max(1, math.ceil(#ids / Q.pageSize))
  if Q.data.page > pages then Q.data.page = pages end
  if Q.data.page < 1 then Q.data.page = 1 end
  local first = (Q.data.page - 1) * Q.pageSize
  local out = {}
  for i = first + 1, math.min(#ids, first + Q.pageSize) do out[#out + 1] = ids[i] end
  return out, pages
end

function Q.refreshDetail()
  local ui = Q.ui
  if not ui then return end
  ui.detail:clear()
  local id = Q.data.selected
  if Q.data.stale then ui.detail:cecho(colour("grey", "Disconnected: this is the journal as it was.") .. "\n") end
  if id and Q.data.quests[id] then ui.detail:cecho(Q.renderDetail(id)) end
end

function Q.refresh()
  local ui = Q.ui
  if not ui then return end
  local tab = Q.data.tab
  for _, state in ipairs(Q.tabs) do
    ui.tabs[state]:setStyleSheet(state == tab and tabOn or tabStyle)
    ui.tabs[state]:echo(Q.tabText(state))
  end
  local ids, pages = Q.pageIds()
  local showActive = tab == "active" and #ids > 0 and not Q.data.note
  for i, row in ipairs(ui.rows) do
    local id = showActive and ids[i] or nil
    if id then
      local q = Q.data.quests[id]
      row:setStyleSheet(id == Q.data.selected and rowOn or rowStyle)
      row:echo(plain(q.name) .. (q.progress and q.progress ~= "" and ("  (" .. plain(q.progress) .. ")") or ""))
      row:setClickCallback(function() Q.select(id) end)
    end
    show(row, id ~= nil)
  end
  show(ui.prev, showActive and pages > 1)
  show(ui.next, showActive and pages > 1)
  show(ui.pageText, showActive and pages > 1)
  if showActive and pages > 1 then ui.pageText:echo("Page " .. Q.data.page .. " of " .. pages) end
  show(ui.detail, showActive)
  ui.summary:clear()
  if showActive then
    Q.refreshDetail()
    show(ui.summary, false)
  else
    show(ui.summary, true)
    local text = ""
    if not Q.data.note and Q.data.list and tab ~= "active" then text = Q.renderSummaries(tab) end
    if text == "" then text = colour("grey", Q.emptyText(tab)) .. "\n" end
    if Q.data.stale and not Q.data.note then text = colour("grey", "Disconnected: this is the journal as it was.") .. "\n" .. text end
    ui.summary:cecho(text)
  end
end

function Q.showTab(state)
  if not Q.states[state] then return end
  Q.data.tab = state
  Q.refresh()
end

function Q.select(id)
  if not Q.data.quests[id] or Q.data.quests[id].state ~= "active" then return end
  Q.data.selected = id
  Q.refresh()
end

function Q.page(delta)
  Q.data.page = Q.data.page + delta
  Q.refresh()
end

-- ---------- events ----------

local function onListEvent()
  local ok, err = pcall(Q.onList, gmcp and gmcp.Char and gmcp.Char.Quests and gmcp.Char.Quests.List)
  if not ok then say("could not use Char.Quests.List: " .. tostring(err)) end
end

local function onLogEvent()
  local ok, err = pcall(Q.onLog, gmcp and gmcp.Char and gmcp.Char.Quests and gmcp.Char.Quests.Log)
  if not ok then say("could not use Char.Quests.Log: " .. tostring(err)) end
end

function Q.install()
  if Q.handlers then
    for _, id in ipairs(Q.handlers) do killAnonymousEventHandler(id) end
  end
  Q.handlers = {
    registerAnonymousEventHandler("gmcp.Char.Quests.List", onListEvent),
    registerAnonymousEventHandler("gmcp.Char.Quests.Log", onLogEvent),
    registerAnonymousEventHandler("gmcp.Char.Status", function() Q.onStatus() end),
    registerAnonymousEventHandler("sysDisconnectionEvent", function() Q.onDisconnect() end),
  }
  local ok, err = pcall(Q.build)
  if not ok then say("could not build the quests panel: " .. tostring(err)) end
end

Q.install()
