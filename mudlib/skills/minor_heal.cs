// @inherit /std/skill

/// <summary>Priest spell. Restores 15 HP to the caster or to someone in the room (Divine Knowledge adds 25%).</summary>
public class MinorHealSpell : Skill
{
    public override void Create()
    {
        Title = "Minor Heal";
        Command = "heal";
        Kind = "spell";
        Summary = "Restores 15 HP to you or to someone in the room.";
        ManaCost = 20;
        Preparation = 0.5;
        Recovery = 1.0;
        WindUp = "murmurs a prayer";
        Target = "friend";
    }

    public override void Resolve(ObjectRef user, ObjectRef? target)
    {
        target ??= user;
        var amount = user.Call<int>("ScaleHealing", 15);
        var healed = target.Call<int>("Heal", amount);
        var room = Environment(user);
        var who = target == user ? "yourself" : target.Call<string>("QueryLabel") ?? "it";
        TellObject(user, $"Warm light flows from you to {who}, restoring {healed} hit points.\n");
        if (target != user) TellObject(target, $"{user.Call<string>("QueryLabel")} heals you for {healed} hit points.\n");
        if (room != null) TellRoom(room, $"{user.Call<string>("QueryLabel")} heals {(target == user ? "themselves" : target.Call<string>("QueryLabel"))}.\n", user, target);
    }
}
