// @inherit /std/living

/// <summary>
/// A non-player character. Peaceful by default; talks when spoken to, answers
/// questions, and does small idle actions while a player is watching.
/// </summary>
public class Npc : Living
{
    /// <summary>Peaceful creatures cannot be attacked.</summary>
    protected bool Peaceful = true;

    private readonly List<string> chat = new();
    private readonly List<string> topicKeys = new();
    private readonly List<string> topicLines = new();
    private readonly List<string> emoteLines = new();
    private readonly List<int> emotePercents = new();
    private readonly Dictionary<string, int> chatIndex = new();
    private readonly List<string> topicQuests = new(); // parallel to topicKeys: the quests (comma-separated ids) a topic's answer is part of, or ""
    private readonly List<string> chatQuests = new();  // parallel to chat
    private readonly Dictionary<string, Dictionary<string, string>> questDefs = new();
    private string questChatId = "";
    private string questChatLine = "";

    public Npc()
    {
        EnableCommands(); // creatures are living: they are offered commands and receive room messages
    }

    public override bool QueryPeaceful() => Peaceful;

    /// <summary>A line for "talk". Lines come in the order added and wrap round; the first is the main dialogue.</summary>
    /// <summary>quests (comma-separated quest ids) says the line is part of those quests: a character who has been spoken to keeps it in their quest journal.</summary>
    protected void AddChat(string line, string quests = "")
    {
        chat.Add(line);
        chatQuests.Add(quests);
    }

    /// <summary>The reply to "ask <npc> about <keyword>". keywords is comma-separated.</summary>
    /// <summary>quests (comma-separated quest ids) says the answer is part of those quests: it, and what was asked, go in the asker's quest journal.</summary>
    protected void AddTopic(string keywords, string line, string quests = "")
    {
        topicKeys.Add(keywords.ToLowerInvariant());
        topicLines.Add(line);
        topicQuests.Add(quests);
    }

    // ---- Quests (docs/specs/quest-journal.md) ----

    /// <summary>
    /// Describe a quest this NPC gives, for the quest journal. title is what the quest log shows; area where it is given; completed and abandoned
    /// are one or two sentences in the game's voice for the journal's summary of a quest the character finished or gave up (without them a plain
    /// sentence is made); items are the program paths (comma-separated) of what the quest has the character carry, taken back when it is abandoned;
    /// objectives are lines "kill|PATH|text" (that creature kind has been killed) or "have|PATH|text|COUNT" (that many of that item carried),
    /// one per line; a quest that must not be given up sets abandonable false.
    /// </summary>
    protected void DefineQuest(string id, string title, string area, string completed, string abandoned, string items = "", string objectives = "", bool abandonable = true)
    {
        questDefs[id] = new Dictionary<string, string>
        {
            ["title"] = title, ["area"] = area, ["completed"] = completed, ["abandoned"] = abandoned, ["items"] = items, ["objectives"] = objectives,
            ["abandonable"] = abandonable ? "yes" : "no", ["giver"] = Name,
        };
    }

    /// <summary>One line about how far this character has got in an active quest of this giver, shown by the journal under the objectives; null for none (docs/specs/bureaucrat.md section 7.7).</summary>
    public virtual string? QueryProgressNote(string questId, ObjectRef who) => null;

    /// <summary>What this NPC says of one of its quests (title, area, completed, abandoned, items, objectives, abandonable, giver), or null. The journal reads it.</summary>
    public Dictionary<string, string>? QueryQuestDefinition(string id) => questDefs.TryGetValue(id, out var d) ? new Dictionary<string, string>(d) : null;

    /// <summary>Say a line to the room as this NPC, as part of quests (comma-separated ids) for one character: it is also written in that character's quest journal.</summary>
    protected void QuestSay(ObjectRef? who, string quests, string line)
    {
        if (Environment() is { } room) TellRoom(room, Speech(line) + "\n");
        if (who != null) RecordQuest(who, quests, "", line);
    }

    /// <summary>
    /// A line of narration (an action, not speech) as part of quests for one character: toYou is told to that character and written in their quest journal;
    /// toRoom, if given, is what everyone else here is told (the character is left out).
    /// </summary>
    protected void NarrateTo(ObjectRef who, string quests, string toYou, string toRoom = "")
    {
        TellObject(who, toYou + "\n");
        if (toRoom != "" && Environment() is { } room) TellRoom(room, toRoom + "\n", who);
        RecordQuest(who, quests, "", toYou);
    }

    /// <summary>Does the asked topic name one of these keys (comma-separated)? Equal to a key, or containing it.</summary>
    protected static bool TopicHas(string topic, string keys)
    {
        topic = (topic ?? "").ToLowerInvariant().Trim();
        foreach (var raw in keys.Split(','))
        {
            var key = raw.Trim();
            if (key != "" && (topic == key || topic.Contains(key))) return true;
        }
        return false;
    }

    /// <summary>The one place quest speech is written down: the quest journal of the character it was said to (never of anyone else who heard it).</summary>
    protected void RecordQuest(ObjectRef who, string quests, string asked, string line)
    {
        if (quests == "" || who == null || !who.IsAlive || !Interactive(who)) return;
        who.Call("QuestHeard", quests, asked, Name, line);
    }

    /// <summary>An idle action shown to the room, with this percent chance per heartbeat while a player is present.</summary>
    protected void AddEmote(string line, int percent)
    {
        emoteLines.Add(line);
        emotePercents.Add(percent);
    }

    /// <summary>
    /// Once a player has completed the quest (state 2), the main line (the first one added) is replaced
    /// for that player by this one, and the other chat lines stay as they are. For thanks after a quest.
    /// </summary>
    protected void AddChatAfterQuest(string questId, string line)
    {
        questChatId = questId;
        questChatLine = line;
    }

    /// <summary>Replaces the next chat line for this player (for example after a quest). Null for the normal line.</summary>
    protected virtual string? TalkOverride(ObjectRef who) => null;

    /// <summary>Called when someone talks to this NPC or asks it something. A patrol stops walking for a moment here.</summary>
    protected virtual void Spoken(ObjectRef who) { }

    /// <summary>Called when the room resets. Shopkeepers restock here.</summary>
    public virtual void OnReset() { }

    /// <summary>Leave the game quietly (used by rooms for encounters).</summary>
    public void WanderOff() => Destruct();

    protected string Speech(string line) => $"{Name} says: \"{line}\"";

    /// <summary>Called by the "talk" command. Returns what is shown to the player.</summary>
    public string? Talk(ObjectRef who)
    {
        Spoken(who);
        var line = TalkOverride(who);
        if (line == null && chat.Count > 0)
        {
            var key = who.Call<string>("QueryName") ?? "";
            var index = chatIndex.GetValueOrDefault(key);
            line = chat[index % chat.Count];
            if (index % chat.Count == 0 && questChatId != "" && who.Call<int>("QueryQuest", questChatId) == 2) line = questChatLine;
            else RecordQuest(who, chatQuests[index % chat.Count], "", line);
            chatIndex[key] = index + 1;
        }
        return line == null ? $"{Name} has nothing to say to you." : Speech(line);
    }

    /// <summary>Called by the "ask" command.</summary>
    public virtual string? Ask(ObjectRef who, string topic)
    {
        Spoken(who);
        topic = topic.ToLowerInvariant().Trim();
        // The best match wins, not the first: an exact key; else the longest key found in what was asked
        // ("oakenfell" before "oak"); else the first key that contains what was asked (three letters or more).
        var best = -1; var bestLength = 0; var loose = -1;
        for (var i = 0; i < topicKeys.Count; i++)
        {
            foreach (var raw in topicKeys[i].Split(','))
            {
                var key = raw.Trim();
                if (key == "") continue;
                if (key == topic) return Answer(who, topic, i);
                if (topic.Contains(key) && key.Length > bestLength) { best = i; bestLength = key.Length; }
                else if (loose < 0 && topic.Length >= 3 && key.Contains(topic)) loose = i;
            }
        }
        if (best >= 0) return Answer(who, topic, best);
        if (loose >= 0) return Answer(who, topic, loose);
        return $"{Name} shrugs. \"I can't tell you anything about that.\"";
    }

    /// <summary>The reply to a topic. A topic that belongs to quests is also written, with the question, in the asker's quest journal.</summary>
    private string Answer(ObjectRef who, string topic, int i)
    {
        RecordQuest(who, topicQuests[i], $"ask {Name} about {topic.Replace("{", "{{")}", topicLines[i]);
        return Speech(topicLines[i]);
    }

    private bool PlayerPresent()
    {
        var room = Environment();
        return room != null && AllInventory(room).Any(o => Interactive(o));
    }

    protected override void Noticed(ObjectRef other)
    {
        if (emoteLines.Count > 0 && Interactive(other)) SetHeartBeat(true);
    }

    protected override bool NeedsHeartBeat() => emoteLines.Count > 0 && PlayerPresent();

    protected override void Tick()
    {
        if (emoteLines.Count == 0 || !PlayerPresent() || Environment() is not { } room) return;
        var i = Random(emoteLines.Count);
        if (Random(100) < emotePercents[i]) TellRoom(room, emoteLines[i] + "\n");
    }
}
