// @inherit /std/skill

/// <summary>Scout skill. Lists the creatures in each adjacent room.</summary>
public class TrackingSkill : Skill
{
    public override void Create()
    {
        Title = "Tracking";
        Command = "track";
        Summary = "Lists the creatures in each adjacent room.";
        StaminaCost = 5;
        Recovery = 0.5;
    }

    public override void Resolve(ObjectRef user, ObjectRef? target)
    {
        var room = Environment(user);
        if (room == null) return;
        var lines = new List<string>();
        foreach (var direction in room.Call<List<string>>("QueryExits") ?? new List<string>())
        {
            var path = room.Call<string>("QueryExitPath", direction);
            var here = new List<string>();
            if (path != null)
            {
                try
                {
                    foreach (var ob in AllInventory(LoadObject(path)))
                        if (ob.Call<bool>("IsCreature") && !Interactive(ob) && ob.Call<string>("Short") is { } desc) here.Add(desc);
                }
                catch (MudRuntimeException) { }
            }
            lines.Add($"  {{cmd}}{direction}{{/}}: " + (here.Count == 0 ? "nothing stirs." : string.Join(", ", here) + "."));
        }
        TellObject(user, "You read the signs and listen at every way out.\n" + (lines.Count == 0 ? "There is no way out to track.\n" : string.Join("\n", lines) + "\n"));
    }
}
