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
    private string questChatId = "";
    private string questChatLine = "";

    public Npc()
    {
        EnableCommands(); // creatures are living: they are offered commands and receive room messages
    }

    public override bool QueryPeaceful() => Peaceful;

    /// <summary>A line for "talk". Lines come in the order added and wrap round; the first is the main dialogue.</summary>
    protected void AddChat(string line) => chat.Add(line);

    /// <summary>The reply to "ask <npc> about <keyword>". keywords is comma-separated.</summary>
    protected void AddTopic(string keywords, string line)
    {
        topicKeys.Add(keywords.ToLowerInvariant());
        topicLines.Add(line);
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

    private string Speech(string line) => $"{Name} says: \"{line}\"";

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
                if (key == topic) return Speech(topicLines[i]);
                if (topic.Contains(key) && key.Length > bestLength) { best = i; bestLength = key.Length; }
                else if (loose < 0 && topic.Length >= 3 && key.Contains(topic)) loose = i;
            }
        }
        if (best >= 0) return Speech(topicLines[best]);
        if (loose >= 0) return Speech(topicLines[loose]);
        return $"{Name} shrugs. \"I can't tell you anything about that.\"";
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
