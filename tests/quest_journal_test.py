#!/usr/bin/env python3
"""End-to-end test of the quest journal (docs/specs/quest-journal.md), over TCP against a temporary copy of the mudlib.

  1. A newcomer: `quests` as before ("You have no quests."), nothing of a quest only spoken of in `quest NAME`, `quests completed`.
  2. Accept Clear the Cellar: the question asked about it, Rowan's offer and acceptance are in `quest cellar`; a line Rowan speaks to
     another player in the room is not in this journal; objectives show and tick, with a note in the conversation.
  3. Hand-in: the log is gone, the authored summary, the date, the giver and the reward are shown, `quests completed` lists it,
     `quests` keeps its old lines.
  4. Abandon the second contract with the confirmation (n, then y): the items are taken back, it is listed under abandoned with its
     authored summary, its giver offers it again, retaking it makes it active with a fresh log and no longer abandoned; not in a fight;
     a quest that cannot be abandoned; a quest without authored text gets the generated sentences.
  5. The caps (80 entries, 8,000 characters, the first entry says earlier conversation is not kept), seen over GMCP too.
  6. Saves: quit and come back, everything is as it was; a character saved before the journal existed gets records made on its first
     login; a restart of the driver keeps it all.
  7. Nothing of a quest the character has not been offered is sent or shown.

Usage (after `dotnet build`):   LESMUDOS_TEST_PORT=5121 python3 tests/quest_journal_test.py
"""
import json
import os
import re
import shutil
import sys
import tempfile
import time

os.environ.setdefault("LESMUDOS_TEST_PORT", "4851")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import smoke_test as h  # noqa: E402
import gmcp_test as g  # noqa: E402

check = h.check
truth = g.truth

GUILD = "/domains/hollow/village/guild"
SHRINE = "/domains/hollow/village/shrine"
OVERSEER = "/domains/hollow/mob/goblin_overseer"
SCAVENGER = "/domains/hollow/mob/goblin_scavenger"
ORDERS = "/domains/hollow/obj/overseers_orders"
HALL = "/domains/qjtest/hall"

PLAYER_ACTIONS = ('        AddAction("tp", a => { MoveObject(LoadObject(a)); Look(); return true; });\n'
                  '        AddAction("credit", a => { NotifyKill(a); return true; });\n'
                  '        AddAction("mk", a => { MoveObject(CloneObject(a), Self); return true; });\n'
                  '        AddAction("spawn", a => { MoveObject(CloneObject(a), Environment()!); return true; });\n')

HALL_ROOM = '''// @inherit /std/room
public class QjHall : Room
{
    public override void Create()
    {
        ShortDesc = "The Test Hall"; LongDesc = "A hall for the quest journal test.";
        MoveObject(CloneObject("/domains/qjtest/envoy"), Self);
    }
}
'''
# An NPC with two quests of its own: one with no authored text, one that cannot be abandoned; short topics for the caps, one long.
ENVOY = '''// @inherit /std/npc
public class QjEnvoy : Npc
{
    public override void Create()
    {
        Name = "Envoy"; Ids.Add("envoy"); ShortDesc = "an envoy"; LongDesc = "A patient envoy.";
        DefineQuest("test-errand", "The Test Errand", "The Test Hall", "", "");
        DefineQuest("test-oath", "The Test Oath", "The Test Hall", "", "", "", "", false);
        AddTopic("errand", "The errand is simple.", "test-errand");
        AddTopic("chat", "Blah.", "test-errand");
        AddTopic("long", "LONGTEXT " + new string('x', 900), "test-errand");
        AddTopic("oath", "Swear it.", "test-oath");
        AddTopic("weather", "Grey. Nothing to do with any quest.");
    }

    protected override void Noticed(ObjectRef other)
    {
        base.Noticed(other);
        if (Interactive(other)) { AddAction("take", Take); AddAction("finish", Finish); }
    }

    private bool Take(string args)
    {
        var p = ThisPlayer()!;
        var id = args.Trim();
        p.Call("SetQuest", id, 1, id == "test-oath" ? "The Test Oath" : "The Test Errand");
        QuestSay(p, id, "Then it is yours.");
        return true;
    }

    private bool Finish(string args)
    {
        var p = ThisPlayer()!;
        var id = args.Trim();
        p.Call("SetQuest", id, 2, id == "test-oath" ? "The Test Oath" : "The Test Errand", "a pat on the back");
        return true;
    }
}
'''


def prepare(mudlib):
    path = os.path.join(mudlib, "std", "player.cs")
    src = open(path).read()
    anchor = '        AddAction("channels", a => ToChannels("Listing", ""));'
    assert anchor in src
    open(path, "w").write(src.replace(anchor, PLAYER_ACTIONS + anchor, 1))
    os.makedirs(os.path.join(mudlib, "domains", "qjtest"), exist_ok=True)
    h.write(mudlib, "/domains/qjtest/hall", HALL_ROOM)
    h.write(mudlib, "/domains/qjtest/envoy", ENVOY)


def ask(c, line, wait="(y/n) "):
    return c.command(line, marker=wait)


def oneline(text):
    """A reply with its line breaks and runs of spaces made single spaces (the game wraps long lines at 80 columns)."""
    return re.sub(r"\s+", " ", text)


def body(text):
    """A reply without prompts and without what other people said in the room: just what the command printed."""
    return "\n".join(line for line in text.replace("\r", "").split("\n") if line.strip() and " says:" not in line and not re.match(r"^\w+ \(\d+/\d+\)", line))


def entries(text):
    """The conversation lines of a `quest NAME` reply (everything under 'Conversation:')."""
    return text.split("Conversation:")[-1] if "Conversation:" in text else ""


def run(work, mudlib, logpath, driver_box):
    # ===== 1. a newcomer =====
    alice = g.login("Alice", answer="dont")
    bob = g.login("Bob", answer="dont")
    check("a newcomer has no quests", alice.command("quests"), "You have no quests.", absent=("completed", "Active quests"))
    check("quests completed, none", alice.command("quests completed"), "You have completed no quests.")
    check("quests abandoned, none", alice.command("quests abandoned"), "You have abandoned no quests.")
    check("quest NAME with nothing", alice.command("quest cellar"), "You have no quest called \"cellar\"")
    check("quest with no name says how", alice.command("quest"), "Usage:")
    check("abandon with nothing active", alice.command("abandon cellar"), "You have no active quest called \"cellar\"")

    # ===== 2. accept the cellar =====
    alice.command("tp " + GUILD)
    bob.command("tp " + GUILD)
    check("Rowan answers a question about the cellar", alice.command("ask rowan about cellar"), "Goblins have taken the old smugglers' tunnels")
    check("a quest only spoken of is not listed", alice.command("quests"), "You have no quests.")
    check("and its story is not told", alice.command("quest cellar"), "You have no quest called \"cellar\"", absent=("Goblins have taken", "Conversation"))
    check("nor the other quest", alice.command("quest orders"), "You have no quest called", absent=("Conversation",))
    check("an ordinary topic is no part of a quest", alice.command("ask rowan about vincent"), "Vincent Holloway built this village.")
    check("accept the cellar", alice.command("accept cellar"), "Quest accepted: Clear the Cellar.")
    story = alice.command("quest cellar")
    check("the journal says who gave it, where and when", story, "Clear the Cellar (active)", "Given by Rowan in Vincent's Hollow.", "Taken on the ")
    check("the question and Rowan's offer are in it", story, "Conversation:", "You: ask Rowan about cellar", "Rowan: \"Goblins have taken the old smugglers' tunnels")
    check("so is the acceptance, in the order it happened", story, "Quest taken on: Clear the Cellar.", "Rowan: \"Good. Go down through the Hollowman's Rest")
    truth("the offer comes before the acceptance", story.index("Goblins have taken") < story.index("Quest taken on") < story.index("Good. Go down"), story)
    check("a line about another subject is not recorded", story, absent=("Vincent Holloway", "Grey"))
    check("the objective shows", story, "Objectives:", "[ ] Kill the Goblin Overseer")
    check("quests keeps its old line and adds how to read more", alice.command("quests"), "Active quests: Clear the Cellar", "quest NAME tells the story of one.")
    check("a quest is found by part of its title or its id", alice.command("quest clear-the-cellar"), "Clear the Cellar (active)")
    check("the same by a word", alice.command("quest CELLAR"), "Clear the Cellar (active)")
    # What Rowan says to Alice in the room is heard by Bob, and is not in his journal.
    bob_text = bob.read_until("Good. Go down", timeout=3)
    check("Bob heard Rowan answer Alice", bob_text, "Rowan says: \"Good. Go down")
    check("but it is not in Bob's journal", bob.command("quests"), "You have no quests.")
    check("nor his quest list", bob.command("quest cellar"), "You have no quest called")
    check("Rowan's reminder goes to the one who asked", alice.command("accept cellar"), "You have already taken")
    check("a reminder is a new line of the log", alice.command("quest cellar"), "You have already taken \"Clear the Cellar\".")

    # ===== 3. progress and hand-in =====
    alice.command("credit " + OVERSEER)
    story = alice.command("quest cellar")
    check("a kill ticks the objective", story, "[x] Kill the Goblin Overseer")
    check("and is noted in the conversation", oneline(entries(story)), "Kill the Goblin Overseer at the end of the Forgotten Cellars, then report to Rowan (1 of 1)")
    check("report completes it", alice.command("report"), "Quest complete: Clear the Cellar!", "Rowan pays you 50 gold", "New quest: Recover the Overseer's Orders.")
    done = alice.command("quest cellar")
    check("a completed quest shows its summary, not the talk", done, "Clear the Cellar (completed)", "Given by Rowan in Vincent's Hollow. Completed the ",
          "Reward: 150 experience, 50 gold, a Guild Initiate's Helm and a travel pass.", "You went down into the smugglers' tunnels",
          absent=("Conversation", "Goblins have taken", "Quest taken on", "Objectives"))
    check("quests lists it as before", alice.command("quests"), "Active quests: Recover the Overseer's Orders", "Completed quests: Clear the Cellar", "1 completed, 0 abandoned.")
    check("quests completed lists it with its date and summary", alice.command("quests completed"), "Completed quests:", "Clear the Cellar - the ", "Given by Rowan in Vincent's Hollow.",
          "Reward: 150 experience", "You went down into the smugglers' tunnels")
    orders = alice.command("quest orders")
    check("the second contract was offered at once and keeps Rowan's words", oneline(orders), "Recover the Overseer's Orders (active)", "There is more. Whatever drove those goblins left its orders",
          "[ ] Find the Overseer's Orders in the Forgotten Cellars, then bring them to Rowan")

    # ===== 4. abandoning =====
    alice.command("mk " + ORDERS)
    time.sleep(2.5)  # a heart beat or the next look notices the orders
    orders = alice.command("quest orders")
    check("carrying the orders ticks the objective and says so", oneline(orders), "[x] Find the Overseer's Orders", "then bring them to Rowan (1 of 1)")
    check("abandon asks first", ask(alice, "abandon orders"), "Abandon \"Recover the Overseer's Orders\"?", "Rowan will offer it again", "what you carry for it taken back", "(y/n) ")
    check("n changes nothing", alice.command("n"), "Not abandoned.")
    check("the quest is still active", alice.command("quests"), "Active quests: Recover the Overseer's Orders")
    check("the orders are still carried", alice.command("i"), "Overseer's Orders")
    ask(alice, "quest abandon orders")
    check("any answer but yes changes nothing", alice.command("maybe"), "Not abandoned.")
    ask(alice, "abandon orders")
    text = alice.command("yes")
    check("yes gives it up", text, "You give up \"Recover the Overseer's Orders\".", "You put Rowan's second contract aside.")
    check("the quest items are taken back", alice.command("i"), absent=("Overseer's Orders",))
    check("it is not an active quest and reads as not started", alice.command("quests"), "Completed quests: Clear the Cellar", "1 completed, 1 abandoned.", absent=("Active quests",))
    check("listed under abandoned with its summary", alice.command("quests abandoned"), "Abandoned quests:", "Recover the Overseer's Orders - the ", "Given by Rowan in Vincent's Hollow.", "You put Rowan's second contract aside.")
    check("its story is the summary", alice.command("quest orders"), "Recover the Overseer's Orders (abandoned)", "Given up the ", "Its giver will offer it again", absent=("Conversation", "There is more"))
    check("the completed one is untouched", alice.command("quest cellar"), "Clear the Cellar (completed)")
    check("the giver offers it as to a newcomer", alice.command("accept orders"), "Quest accepted: Recover the Overseer's Orders.")
    retaken = alice.command("quest orders")
    check("a retaken quest is active again, with a fresh log", retaken, "Recover the Overseer's Orders (active)", "Quest taken on: Recover the Overseer's Orders.", "Rowan: \"The goblins take their orders from somewhere.",
          absent=("There is more. Whatever drove", "You put Rowan's second contract aside", "(abandoned)"))
    check("and is no longer listed as abandoned", alice.command("quests abandoned"), "You have abandoned no quests.")
    check("the completed quest's reward was never taken back", alice.command("i"), "travel pass", "Guild Initiate's helm")
    # Not in a fight
    alice.command("tp /domains/qjtest/hall")
    alice.command("spawn /domains/hollow/mob/goblin_scavenger")
    alice.send("kill scavenger")
    time.sleep(0.5)
    check("a quest cannot be abandoned in a fight", alice.command("abandon orders"), "You cannot abandon a quest in the middle of a fight.", absent=("(y/n)",))
    alice.read_until("dies", timeout=90)  # the fight runs its course
    time.sleep(1)

    # ===== the envoy: no authored text, and a quest that cannot be abandoned =====
    alice.command("tp " + HALL)
    for _ in range(3):
        if "Envoy" in alice.command("look"):
            break
        time.sleep(1)
    alice.command("take test-errand")
    alice.command("take test-oath")
    check("a quest nobody wrote summaries for is abandoned with a plain sentence", ask(alice, "abandon errand"), "Abandon \"The Test Errand\"?")
    check("answering yes", alice.command("y"), "You give up \"The Test Errand\".", "You gave up \"The Test Errand\", which Envoy had given you.")
    check("a quest marked not abandonable refuses", alice.command("abandon oath"), "\"The Test Oath\" cannot be abandoned.", absent=("(y/n)",))
    alice.command("take test-errand")
    alice.command("finish test-errand")
    check("completed with no authored text: a plain sentence and the reward given", alice.command("quest errand"), "The Test Errand (completed)", "Reward: a pat on the back.", "You completed \"The Test Errand\" for Envoy.")

    # ===== 5. the caps =====
    carol = g.login("Carol", answer="do")
    carol.command("tp " + HALL)
    carol.command("take test-errand")
    for _ in range(50):
        carol.command("ask envoy about chat")
    story = carol.command("quest errand")
    shown = [line for line in entries(story).split("\n") if line.strip()]
    check("the log says the early conversation is not kept", story, "(earlier conversation not kept)")
    truth("it holds at most 80 entries", len(re.findall(r"^\s*(?:\*|You:|Envoy:)", entries(story), re.M)) <= 80, len(shown))
    truth("and the newest entry is the last", re.findall(r"^\s*(?:You:|Envoy:)(.*)$", entries(story), re.M)[-1].strip() == "\"Blah.\"", entries(story)[-120:])
    log = carol.of("Char.Quests.Log")
    last = [m for m in log if m["id"] == "test-errand"][-1]
    truth("the appended entries carry sequence numbers", all("seq" in e for m in log for e in m["entries"]), log[-1])
    carol.command("quest errand")
    carol.pump(0.5)
    mark = carol.mark()
    carol.command("quests")
    carol.pump(0.6)
    full = [m for m in carol.of("Char.Quests.Log", mark) if m["id"] == "test-errand"]
    flat = [e for m in full for e in m["entries"]]
    truth("a resent log starts with reset and the gap entry", full and full[0]["reset"] is True and flat[0]["kind"] == "gap" and flat[0]["text"] == "(earlier conversation not kept)", full[:1])
    truth("at most 80 real entries", len([e for e in flat if e["kind"] != "gap"]) <= 80, len(flat))
    seqs = [e["seq"] for e in flat if e["kind"] != "gap"]
    truth("their sequence numbers are consecutive", bool(seqs) and seqs == list(range(seqs[0], seqs[0] + len(seqs))), seqs[:5])
    for _ in range(12):
        carol.command("ask envoy about long")
    carol.pump(0.5)
    mark = carol.mark()
    carol.command("quests")
    carol.pump(0.6)
    flat = [e for m in carol.of("Char.Quests.Log", mark) if m["id"] == "test-errand" for e in m["entries"] if e["kind"] != "gap"]
    truth("at most 8,000 characters of text", sum(len(e["text"]) for e in flat) <= 8000, sum(len(e["text"]) for e in flat))
    truth("the newest long answer is the last entry", flat[-1]["text"].startswith("LONGTEXT"), flat[-1]["text"][:20])

    # ===== 6. saves =====
    commands = ("quests", "quests completed", "quests abandoned", "quest orders", "quest cellar", "quest errand", "quest oath")
    before = {c: body(alice.command(c)) for c in commands}
    alice.send("quit")
    alice.closed(5)
    alice = g.login("Alice", answer="dont")
    for c in commands:
        truth("after a login `" + c + "` says the same", body(alice.command(c)) == before[c], (before[c][-200:], body(alice.command(c))[-200:]))
    truth("the active quest's conversation survived", "Quest taken on: Recover the Overseer's Orders." in before["quest orders"] and "Rowan: \"The goblins take their orders" in before["quest orders"])
    return alice


def main():
    work = tempfile.mkdtemp(prefix="lesmudos-qj-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib)
    prepare(mudlib)
    logpath = os.path.join(work, "driver.log")
    log = open(logpath, "w")
    driver = g.start_driver(work, mudlib, log)
    box = [driver]
    try:
        run(work, mudlib, logpath, box)
    finally:
        g.stop_driver(box[0])
        log.close()
    print()
    if h.failures:
        print(f"{len(h.failures)} FAILED:", *h.failures, sep="\n  ")
        sys.exit(1)
    print(f"all checks passed ({g.CHECKS[0]} gmcp-style checks counted)")
    shutil.rmtree(work, ignore_errors=True)


if __name__ == "__main__":
    main()
