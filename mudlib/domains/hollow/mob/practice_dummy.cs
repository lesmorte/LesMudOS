// @inherit /std/monster

/// <summary>
/// The Practice Yard's training dummy. It can be attacked, so that a player sees what their blows do (with {cmd}verbose on{/}, every hit
/// shows its damage), and it cannot be hurt for good: Die is overridden so that it never leaves a corpse, pays no experience,
/// gives no kill credit and makes no Chronicle entry, and it is made whole again the moment a fight is over. It never strikes back:
/// its one action does nothing but sway on its post. Everything is done in this file; /std/monster is unchanged.
/// </summary>
public class PracticeDummy : Monster
{
    public override void Create()
    {
        Name = "Training Dummy";
        Ids.Add("dummy");
        Ids.Add("training dummy");
        Ids.Add("target");
        ShortDesc = "a training dummy";
        LongDesc = "A man-shaped dummy of sacking and straw on a stout post, its chest painted with a red ring and its arms bound out in a permanent shrug. It is scarred from a hundred blades and has never once complained. It will stand there as long as you care to hit it.";
        Level = 1;
        MaxHp = 10000;
        DamageMin = 0;
        DamageMax = 0;
        ToHit = 0;
        Armor = -10;            // straw: a blow that is not a natural 1 lands, so that a player sees their damage
        XpValue = 0;
        GoldMin = 0;
        GoldMax = 0;
        Aggressive = false;
        Assist = false;
        BaseReaction = 6.0;
        AddCombatAction("sway", "Sway", preparation: 0, recovery: 3.0, kind: "sway");
    }

    /// <summary>Its only action: it sways. No blow, no damage.</summary>
    protected override void Special(CombatAction action, ObjectRef? target)
    {
        if (action.Kind == "sway" && Environment() is { } room)
            TellRoom(room, "The training dummy rocks on its post and settles again.\n");
    }

    /// <summary>It cannot die: when its Vitality would run out it is simply made whole. No experience, no loot, no corpse, no kill credit, no Chronicle.</summary>
    public override void Die(ObjectRef? killer)
    {
        Heal(MaxHp);
    }

    /// <summary>A fight is over: the straw is patched at once, so the next player starts on a whole dummy.</summary>
    protected override void FightEnded()
    {
        Heal(MaxHp);
    }
}
