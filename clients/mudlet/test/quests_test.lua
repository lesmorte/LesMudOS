-- Tests of the Hollowmere quests panel without Mudlet (the API is stubbed in stub.lua).
-- Run from clients/mudlet:   lua test/quests_test.lua      (Lua 5.1 or newer; also LuaJIT)
local here = arg and arg[0] and arg[0]:match("^(.*)/test/[^/]*$") or "."
package.path = here .. "/test/?.lua;" .. package.path

local S = dofile(here .. "/test/stub.lua")
dofile(here .. "/src/layout.lua")
dofile(here .. "/src/map.lua")
dofile(here .. "/src/gauges.lua")
dofile(here .. "/src/quests.lua")
local fixture = dofile(here .. "/test/quests_fixture.lua")

local Q = Hollowmere.quests
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

local function has(text, part) return text:find(part, 1, true) ~= nil end
local function at(text, part) return (text:find(part, 1, true)) end

local function deliver(package, data)
  S.setGmcp(package, data)
  S.fire("gmcp." .. package)
end

local ui = Q.ui
local function summary() return S.consoles.HollowmereQuestsSummary end
local function detail() return S.consoles.HollowmereQuestsDetail end
local function row(i) return S.labels["HollowmereQuestsRow" .. i] end
local function tab(state) return S.labels["HollowmereQuestsTab_" .. state] end

-- ---------- the panel is built, and waits ----------
check("the panel is built when the script loads", ui ~= nil and S.labels.HollowmereQuestsTop ~= nil and S.borderRight == Q.width)
check("it has a Quests tab and three sections", has(S.labels.HollowmereQuestsTop.text, "Quests") and tab("active").text == "Active (0)" and tab("completed").text == "Completed (0)" and tab("abandoned").text == "Abandoned (0)")
check("before anything arrives it says it waits", has(summary().text, "Waiting for the game's quest journal."), summary().text)
check("the handlers are registered for the two packages, the status and a disconnection", (function()
  local n = 0
  for _, h in pairs(S.handlers) do
    if h.event == "gmcp.Char.Quests.List" or h.event == "gmcp.Char.Quests.Log" or h.event == "gmcp.Char.Status" or h.event == "sysDisconnectionEvent" then n = n + 1 end
  end
  return n == 4
end)())

-- ---------- the game's own messages, in order ----------
local index = 0
local function nextMessage()
  index = index + 1
  local m = fixture[index]
  return m
end
local function play(count)
  for _ = 1, count do
    local m = nextMessage()
    if m.package == "reconnect" then
      S.fire("sysDisconnectionEvent")
    else
      deliver(m.package, m.data)
    end
  end
end

-- 1: a newcomer's list
play(1)
check("a list with no quests: the panel says No quests yet.", has(summary().text, "No quests yet."), summary().text)
Q.showTab("completed")
check("in every section", has(summary().text, "No quests yet."))
Q.showTab("active")

-- 2-3: the first quest accepted (log, then list)
play(2)
check("an active quest is listed with its progress", row(1).visible and has(row(1).text, "Recover the Overseer's Orders") and has(row(1).text, "0 of 1"), row(1).text)
check("the tab counts follow", tab("active").text == "Active (1)", tab("active").text)
check("the quest is chosen and its detail shown", Q.data.selected == "overseers-orders" and detail().visible and not summary().visible)
local d = detail().text
check("the objective and progress are at the top", at(d, "Objective") < at(d, "Conversation") and has(d, "[ ] Find the Overseer's Orders in the Forgotten Cellars"), d)
check("with who gave it, where and when", has(d, "Given by Rowan in Vincent's Hollow.") and has(d, "Taken on the 1st Day of"), d)
check("the conversation has the question, Rowan's offer and the acceptance", has(d, "<yellow>You<reset>: ask Rowan about quests") and has(d, "<cyan>Rowan<reset>: Read the board.") and has(d, "Quest taken on: Recover the Overseer's Orders."), d)
check("speaker names are highlighted", has(d, "<cyan>Rowan<reset>"))
check("the date is a heading", has(d, "<grey>-- the 1st Day of"))

-- the entries that follow, one at a time: newest at the bottom
play(4)
d = detail().text
local lastAsk = d:match(".*()ask Rowan about quest")
check("appended entries come at the bottom, in order", lastAsk ~= nil and lastAsk > at(d, "Quest taken on"), d)
check("the log has no hole", not has(d, "may be missing"))

-- 11: the orders carried: progress moves
play(1)
check("the objective ticks and the list shows it", has(row(1).text, "1 of 1") and has(detail().text, "<green>[x]<reset> Find the Overseer's Orders"), detail().text)

-- the quest is completed: list, then the log reset
play(2)
check("the quest leaves Active", tab("active").text == "Active (0)" and tab("completed").text == "Completed (1)", tab("active").text .. tab("completed").text)
check("its conversation is dropped", Q.data.logs["overseers-orders"] == nil)
check("Active says there are none", has(summary().text, "No active quests."), summary().text)
Q.showTab("completed")
local s = summary().text
check("Completed shows name, date, giver, reward and summary", has(s, "Recover the Overseer's Orders") and has(s, "the 1st Day of") and has(s, "Given by Rowan in Vincent's Hollow.") and has(s, "Reward: 200 experience, 75 gold.") and has(s, "You carried the Overseer's Orders back to Rowan"), s)
check("and no conversation", not has(s, "Conversation") and not has(s, "ask Rowan"), s)
Q.showTab("abandoned")
check("Abandoned has none", has(summary().text, "No abandoned quests."), summary().text)

-- a second quest taken, then given up
play(2)
play(1)
play(1)
play(1)
Q.showTab("abandoned")
s = summary().text
check("an abandoned quest shows in Abandoned with its summary", tab("abandoned").text == "Abandoned (1)" and has(s, "The Quiet Offering") and has(s, "You left the shrine's offering undone") and has(s, "Given by Sister Merewyn"), s)
check("and not a reward", not has(s, "Reward:"), s)
Q.showTab("active")

-- retaken: fresh log, no longer abandoned
play(3)
check("a retaken quest is active again and no longer abandoned", tab("abandoned").text == "Abandoned (0)" and tab("active").text == "Active (1)" and Q.data.selected == "the-quiet-offering", tab("abandoned").text .. tab("active").text)
d = detail().text
check("with its objectives and a fresh conversation", has(d, "Bring Sister Merewyn a wildflower") and has(d, "(0 of 3 done)") and has(d, "Quest taken on: The Quiet Offering."), d)

-- the reconnect: everything is sent again
local before = detail().text
play(1)
check("a disconnection says the journal is as it was", has(detail().text, "Disconnected: this is the journal as it was."), detail().text)
play(2)
check("after the login burst the same conversation is shown, without the note", not has(detail().text, "Disconnected") and detail().text == before, detail().text)
check("the whole fixture was played", index == #fixture, index .. " of " .. #fixture)

-- ---------- sequence holes ----------
local function logMsg(id, reset, entries) return { id = id, reset = reset, entries = entries } end
local function entry(seq, text, kind) return { seq = seq, ts = 1, date = "the 2nd Day of Deepfrost, Year 1", kind = kind or "npc", speaker = "Rowan", text = text } end
deliver("Char.Quests.Log", logMsg("the-quiet-offering", true, { entry(1, "one"), entry(2, "two") }))
deliver("Char.Quests.Log", logMsg("the-quiet-offering", false, { entry(4, "four") }))
d = detail().text
check("a hole in the sequence numbers is shown", has(d, "(a line may be missing here)") and at(d, "two") < at(d, "(a line may be missing here)") and at(d, "(a line may be missing here)") < at(d, "four"), d)
deliver("Char.Quests.Log", logMsg("the-quiet-offering", false, { entry(4, "four again") }))
check("a line already seen is not added twice", not has(detail().text, "four again"))
deliver("Char.Quests.Log", logMsg("the-quiet-offering", true, { entry(1, "fresh") }))
check("a reset starts the log again and clears the hole", has(detail().text, "fresh") and not has(detail().text, "may be missing") and not has(detail().text, "four"), detail().text)
deliver("Char.Quests.Log", logMsg("the-quiet-offering", true, { { seq = 0, kind = "gap", text = "(earlier conversation not kept)", date = "" }, entry(9, "nine"), entry(10, "ten") }))
d = detail().text
check("the game's gap entry opens a log that starts at any number", has(d, "(earlier conversation not kept)") and has(d, "nine") and has(d, "ten") and not has(d, "may be missing"), d)

-- ---------- text that is not ours ----------
deliver("Char.Quests.Log", logMsg("the-quiet-offering", false, { entry(11, "a <red>forged<reset> colour") }))
check("angle brackets in the game's text cannot start a colour", not has(detail().text, "<red>forged") and has(detail().text, "\226\128\185red>forged"), detail().text)

-- ---------- malformed messages change nothing and raise nothing ----------
local snapshot = detail().text .. tab("active").text
local ok1 = pcall(deliver, "Char.Quests.List", nil)
local ok2 = pcall(deliver, "Char.Quests.List", { quests = "no" })
local ok3 = pcall(deliver, "Char.Quests.List", "text")
local ok4 = pcall(deliver, "Char.Quests.Log", { id = 5 })
local ok5 = pcall(deliver, "Char.Quests.Log", { id = "x", entries = "no" })
local ok6 = pcall(deliver, "Char.Quests.Log", nil)
check("malformed messages raise nothing", ok1 and ok2 and ok3 and ok4 and ok5 and ok6)
check("and change nothing", detail().text .. tab("active").text == snapshot)
deliver("Char.Quests.List", { quests = { { id = 1 }, { id = "bad", name = "Bad", state = "weird" }, "nope" } })
check("a list of nothing usable is an empty list", tab("active").text == "Active (0)" and tab("completed").text == "Completed (0)", tab("active").text)
check("(and says so)", has(summary().text, "No quests yet."), summary().text)

-- ---------- paging ----------
local many = {}
for i = 1, 9 do many[i] = { id = "q" .. i, name = "Quest " .. i, state = "active", giver = "Rowan", area = "Here", started_date = "the 1st Day of Deepfrost, Year 1", progress = "0 of 1", objectives = {} } end
deliver("Char.Quests.List", { quests = many })
check("nine active quests: six rows and a pager", row(6).visible and has(row(6).text, "Quest 6") and S.labels.HollowmereQuestsNext.visible and has(S.labels.HollowmereQuestsPage.text, "Page 1 of 2"), S.labels.HollowmereQuestsPage.text)
S.labels.HollowmereQuestsNext:press()
check("the next page lists the rest", has(row(1).text, "Quest 7") and row(3).visible and not row(4).visible and has(S.labels.HollowmereQuestsPage.text, "Page 2 of 2"), row(1).text)
row(2):press()
check("a click chooses a quest", Q.data.selected == "q8" and has(detail().text, "Quest 8") and has(detail().text, "Nothing has been said yet."), detail().text)
S.labels.HollowmereQuestsPrev:press()
check("and the pager goes back", has(row(1).text, "Quest 1") and Q.data.selected == "q8")
tab("completed"):press()
check("a tab click shows its section", Q.data.tab == "completed" and summary().visible and not detail().visible)

-- ---------- an older game ----------
Q.reset()
Q.showTab("active")
S.fire("gmcp.Char.Status")
check("a game that sends no journal after login: the panel says it is an older version", has(summary().text, "older version") and has(summary().text, "Char.Quests"), summary().text)
deliver("Char.Quests.List", { quests = {} })
check("and the note goes when a journal does arrive", has(summary().text, "No quests yet.") and not has(summary().text, "older version"), summary().text)
Q.reset()
deliver("Char.Quests.List", { quests = {} })
S.fire("gmcp.Char.Status")
check("a game that has sent its journal is never called old", not has(summary().text, "older version"))

-- ---------- hiding and showing the panel ----------
local button = S.labels.HollowmereQuestsButton
check("a hide button exists and is not shown while the panel is", button ~= nil and button.visible == false and Q.hidden ~= true)
check("the header says how to hide", has(S.labels.HollowmereQuestsTop.text, "click to hide"))
S.labels.HollowmereQuestsTop:press()
check("clicking the header hides the panel and gives the width back", Q.hidden == true and S.borderRight == 0 and S.labels.HollowmereQuestsTop.visible == false and tab("active").visible == false and detail().visible == false and summary().visible == false and row(1).visible == false)
check("a small Quests button is left, with the number of active quests", button.visible == true and has(button.text, "Quests (" .. Q.counts().active .. ")"), button.text)
check("the choice is remembered", (S.files[getMudletHomeDir() .. "/hollowmere_quests.lua"] or {}).hidden == true)
Q.refresh()
check("a refresh while hidden shows nothing", tab("active").visible == false and detail().visible == false and summary().visible == false and row(1).visible == false)
button:press()
check("clicking the button brings the panel back", Q.hidden == false and S.borderRight == Q.width and button.visible == false and S.labels.HollowmereQuestsTop.visible == true and tab("active").visible == true and (detail().visible or summary().visible))
check("and that is remembered too", S.files[getMudletHomeDir() .. "/hollowmere_quests.lua"].hidden == false)
local alias
for _, a in pairs(S.aliases) do if a.pattern == "^qpanel$" then alias = a end end
check("typing qpanel toggles it", alias ~= nil and (function() alias.fn() local was = Q.hidden alias.fn() return was == true and Q.hidden == false end)())
S.files[getMudletHomeDir() .. "/hollowmere_quests.lua"] = { hidden = true }
Q.ui = nil
Q.hidden = nil
Q.build()
check("a panel left hidden starts hidden next time", Q.hidden == true and S.borderRight == 0 and S.labels.HollowmereQuestsButton.visible == true and S.labels.HollowmereQuestsTop.visible == false)
Q.setHidden(false)

-- ---------- the package asks for it ----------
S.sent = {}
Hollowmere.map.sendSupports()
check("the package already asks for the Char package, which covers Char.Quests", has(S.sent[1] or "", '"Char 1"'), S.sent[1])

print(string.format("\n%d passed, %d failed", passed, failed))
os.exit(failed == 0 and 0 or 1)
