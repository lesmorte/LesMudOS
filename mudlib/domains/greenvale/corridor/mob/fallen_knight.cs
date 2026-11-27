// @inherit /std/monster

/// <summary>
/// Fallen Knight, level 10. Part of the Greenvale Transit Corridor. Numbers and the balance reasoning:
/// docs/CORRIDOR_BALANCE.md and tests/balance_sim.py (python3 tests/balance_sim.py check compares them with this file).
/// It has no respawn of its own: the room that spawns it (AddSpawn) or lets it wander in (AddEncounter) brings a new one at its next reset.
/// </summary>
public class CmFallenKnight : Monster
{
    private readonly string[] opening = { "ward" };
    private readonly string[] cycle = { "cleave", "cleave", "crush" };
    private int step;
    private string chosen = "";

    /// <summary>For the coming crafting system: the kind of skin this creature yields ("wolf"), or empty. Nothing uses it yet.</summary>
    public string Skin { get; set; } = "";

    /// <summary>For the coming crafting system: the kind of blood this creature yields ("wolf", "goblin"), or empty. Nothing uses it yet.</summary>
    public string Blood { get; set; } = "";

    public override void Create()
    {
        Name = "Fallen Knight";
        Ids.Add("knight");
        Ids.Add("fallen knight");
        ShortDesc = "the Fallen Knight";
        Tags.Add("undead"); Tags.Add("melee"); Tags.Add("slashing"); Tags.Add("sword");
        LongDesc = "A knight in a tarnished suit of ancient plate kneels beside a long stone bier, a notched greatsword in his gauntlets and the cracked remains of a faded blue tabard hanging from his shoulders. His helmet is closed, and the darkness inside is full of pale light. When he rises it is slowly, as one obeying a long-given order.";
        Level = 10;
        MaxVitality = 155;
        DamageMin = 6;
        DamageMax = 10;
        ToHit = 5;
        Armor = 5;
        XpValue = 1550;
        GoldMin = 20;
        GoldMax = 40;
        Aggressive = true;
        Boss = true;
        DeathMessage = "The Fallen Knight sags to his knees, the pale light dying in his helm, and lies still beside the bier.";
        BaseReaction = 3.5;
        AddCombatAction("cleave", "Rusted Cleave", preparation: 0, recovery: 1);
        AddCombatAction("crush", "Crushing Blow", preparation: 1, recovery: 1.5, windUp: "raises the greatsword two-handed above his head", damageMultiplier: 2, strike: "brings the greatsword down in a crushing blow!");
        AddCombatAction("ward", "Shield of the Fallen", preparation: 0.5, recovery: 0.5, windUp: "plants his shield before him", kind: "buff");
        AddLoot("/domains/hollow/obj/healing_potion", 50);
    }

    /// <summary>The once-only triggers first (by Vitality), then the opening actions, then the cycle, round and round.</summary>
    protected override CombatAction ChooseAction(ObjectRef target)
    {
        var n = step++;
        chosen = n < opening.Length ? opening[n] : cycle[(n - opening.Length) % cycle.Length];
        return FindAction(chosen)!;
    }

    protected override void Special(CombatAction action, ObjectRef? target)
    {
        var room = Environment();
        if (room == null) return;
        if (action.Id == "ward")
        {
            AddEffect("ward", "Shield of the Fallen", 15, "armor=2", 0.9, 0);
            TellRoom(room, "The Fallen Knight sets his shield, and a cold light runs along his armor. He is harder to hit and faster to strike!\n");
        }
    }

    protected override void FightEnded()
    {
        step = 0;
        chosen = "";
    }
}
