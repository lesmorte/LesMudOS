// @inherit /std/skill

/// <summary>
/// Mage spell. Damage that ignores armor, needs no hit roll, and can be dodged. Ranks (docs/specs/skill-ranks.md, the Rank tables):
/// I 6-9 for 20 Focus, II 8-12 for 23, III 10-14 for 26; later ranks follow the default step. Arcane Bolt II is taught at the
/// Hedge-Scholar's Loft in Vincent's Hollow and asks for ten goblins slain (the offer is the trainer's, not this file's).
/// </summary>
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
        // The rank table: rank I is the numbers above and these two.
        Effect("min", 6);
        Effect("max", 9);
        RankRow(2, "min=8;max=12;focus=23");
        RankRow(3, "min=10;max=14;focus=26");
    }

    public override string QuerySummaryAt(int rank) =>
        $"{RankInt("min", rank)}-{RankInt("max", rank)} damage that ignores armor. It needs no hit roll, but the target can dodge it.";

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
        var rank = RankOf(user);
        var low = RankInt("min", rank);
        var high = Math.Max(low, RankInt("max", rank));
        var damage = user.Call<int>("ScaleSpellDamage", low + Random(high - low + 1));
        var applied = Math.Max(0, target.Call<int>("ApplyDamage", damage, user));
        TellObject(user, $"Your bolt of arcane force strikes {theirs} for {damage} damage." + (user.Call<string>("DamageNote", applied, true) ?? "") + "\n");
        TellObject(target, $"{label}'s bolt of arcane force strikes you." + (target.Call<string>("DamageNote", applied, false) ?? "") + "\n");
        if (room != null) TellRoom(room, $"{label}'s bolt of arcane force strikes {theirs}.\n", user, target);
        target.Call("FinishDamage", user);
    }
}
