// @inherit /std/skill

/// <summary>Mage spell. 6-9 damage that ignores armor, needs no hit roll, and can be dodged.</summary>
public class ArcaneBoltSpell : Skill
{
    public override void Create()
    {
        Title = "Arcane Bolt";
        Command = "bolt";
        Kind = "spell";
        Summary = "6-9 damage that ignores armor. It needs no hit roll, but the target can dodge it.";
        FocusCost = 20;
        Preparation = 0.5;
        Recovery = 1.0;
        WindUp = "gathers crackling energy";
        Target = "enemy";
    }

    public override void Resolve(ObjectRef user, ObjectRef? target)
    {
        if (target == null) { TellObject(user, "Your bolt finds nothing to hit.\n"); return; }
        var room = Environment(user);
        var theirs = target.Call<string>("QueryLabel") ?? "it";
        var label = user.Call<string>("QueryLabel") ?? "Someone";
        if (target.Call<bool>("RollDodge"))
        {
            TellObject(user, $"{char.ToUpper(theirs[0]) + theirs.Substring(1)} dodges your bolt.\n");
            TellObject(target, $"You dodge {label}'s bolt.\n");
            if (room != null) TellRoom(room, $"{char.ToUpper(theirs[0]) + theirs.Substring(1)} dodges {label}'s bolt.\n", user, target);
            return;
        }
        var damage = user.Call<int>("ScaleSpellDamage", 6 + Random(4));
        var applied = Math.Max(0, target.Call<int>("ApplyDamage", damage, user));
        TellObject(user, $"Your bolt of arcane force strikes {theirs} for {damage} damage." + (user.Call<string>("DamageNote", applied, true) ?? "") + "\n");
        TellObject(target, $"{label}'s bolt of arcane force strikes you." + (target.Call<string>("DamageNote", applied, false) ?? "") + "\n");
        if (room != null) TellRoom(room, $"{label}'s bolt of arcane force strikes {theirs}.\n", user, target);
        target.Call("FinishDamage", user);
    }
}
