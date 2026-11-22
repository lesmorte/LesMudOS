// @inherit /std/skill

/// <summary>
/// Scout skill. Lists the creatures in the rooms the character could walk to now (docs/specs/skill-ranks.md, "Tracking"). It follows
/// only exits the character can use at this moment: never through a closed or locked door, never through a hidden way they have not
/// found (one they have found is an ordinary exit). Rank I reads the adjacent rooms; rank II, the last, reads two rooms away and, when
/// creatures are one room away behind a closed door or an unfound hidden way, adds one line with no direction, name or count.
/// Tracking never marks a hidden exit as found and reports nothing to the Chronicle.
/// </summary>
public class TrackingSkill : Skill
{
    public const string SomethingClose = "Something is close by, though you cannot tell where.";

    public override void Create()
    {
        Title = "Tracking";
        Command = "track";
        Summary = "Lists the creatures in each room you could walk to from here.";
        EnduranceCost = 5;
        Recovery = 0.5;
        MaxRank = 2; // rank II is the last
        Effect("range", 1);
        FixedKeys.Add("range");
        RankRow(2, "range=2");
    }

    public override string QuerySummaryAt(int rank) => RankInt("range", rank) >= 2
        ? "Lists the creatures in each room you could walk to from here, and in the rooms beyond them, and senses what waits behind a closed way."
        : Summary;

    private List<string> CreaturesIn(string? path)
    {
        var here = new List<string>();
        if (path == null) return here;
        try
        {
            foreach (var ob in AllInventory(LoadObject(path)))
                if (ob.Call<bool>("IsCreature") && !Interactive(ob) && ob.Call<string>("Short") is { } desc) here.Add(desc);
        }
        catch (MudRuntimeException) { }
        return here;
    }

    public override void Resolve(ObjectRef user, ObjectRef? target)
    {
        var room = Environment(user);
        if (room == null) return;
        var range = RankInt("range", RankOf(user));
        var lines = new List<string>();
        foreach (var direction in room.Call<List<string>>("QueryUsableExits", user) ?? new List<string>())
        {
            var path = room.Call<string>("QueryExitDestination", user, direction);
            var here = CreaturesIn(path);
            var line = $"  {{cmd}}{direction}{{/}}: " + (here.Count == 0 ? "nothing stirs." : string.Join(", ", here) + ".");
            if (range >= 2 && path != null)
            {
                ObjectRef? next = null;
                try { next = LoadObject(path); } catch (MudRuntimeException) { }
                var beyond = new List<string>();
                if (next != null)
                    foreach (var onward in next.Call<List<string>>("QueryUsableExits", user) ?? new List<string>())
                    {
                        var far = next.Call<string>("QueryExitDestination", user, onward);
                        if (far == null || far == room.ProgramPath || far == room.Name) continue;
                        foreach (var c in CreaturesIn(far)) beyond.Add($"{c} ({onward})");
                    }
                if (beyond.Count > 0) line += " Further on: " + string.Join(", ", beyond) + ".";
            }
            lines.Add(line);
        }
        var text = "You read the signs and listen at every way out.\n" + (lines.Count == 0 ? "There is no way out to track.\n" : string.Join("\n", lines) + "\n");
        if (range >= 2 && (room.Call<List<string>>("QueryClosedDestinations", user) ?? new List<string>()).Any(p => CreaturesIn(p).Count > 0))
            text += SomethingClose + "\n";
        TellObject(user, text);
    }
}
