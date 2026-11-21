// @inherit /std/skill

/// <summary>Priest spell. +1 to hit and +1 armor for 60 seconds, on the caster or someone in the room.</summary>
public class BlessingSpell : Skill
{
    public override void Create()
    {
        Title = "Blessing";
        Command = "bless";
        Kind = "spell";
        Summary = "+1 to hit and +1 armor for 60 seconds, on you or someone in the room.";
        FocusCost = 25;
        Preparation = 0.5;
        Recovery = 1.0;
        WindUp = "speaks a blessing";
        Target = "friend";
    }

    public override void Resolve(ObjectRef user, ObjectRef? target)
    {
        target ??= user;
        target.Call("AddEffect", "blessing", "Blessing", 60, "tohit=1;armor=1", 1.0, 0);
        TellObject(user, target == user ? "A blessing settles over you.\n" : $"You bless {target.Call<string>("QueryLabel")}.\n");
        if (target != user) TellObject(target, $"{user.Call<string>("QueryLabel")} blesses you. You feel protected.\n");
    }
}
