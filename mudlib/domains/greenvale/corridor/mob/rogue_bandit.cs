// @inherit /std/monster

/// <summary>
/// Rogue Bandit, level 8. Part of the Greenvale Transit Corridor. Numbers and the balance reasoning:
/// docs/CORRIDOR_BALANCE.md and tests/balance_sim.py (python3 tests/balance_sim.py check compares them with this file).
/// It has no respawn of its own: the room that spawns it (AddSpawn) or lets it wander in (AddEncounter) brings a new one at its next reset.
/// </summary>
public class CmRogueBandit : Monster
{
    private readonly string[] opening = new string[0];
    private readonly string[] cycle = { "slash", "slash", "pommel" };
    private int step;
    private string chosen = "";
    private readonly string[] fxAction = { "pommel" };
    private readonly string[] fxKind = { "stagger" };
    private readonly double[] fxValue = { 2 };
    private readonly string[] fxMods = { "" };
    private readonly int[] fxSeconds = { 0 };

    /// <summary>For the coming crafting system: the kind of skin this creature yields ("wolf"), or empty. Nothing uses it yet.</summary>
    public string Skin { get; set; } = "";

    /// <summary>For the coming crafting system: the kind of blood this creature yields ("wolf", "goblin"), or empty. Nothing uses it yet.</summary>
    public string Blood { get; set; } = "";

    public override void Create()
    {
        Name = "Rogue Bandit";
        Ids.Add("bandit");
        Ids.Add("rogue");
        Ids.Add("rogue bandit");
        ShortDesc = "a rogue bandit";
        LongDesc = "A broad man in mismatched leather, his face half hidden by a scarf and his sword hand scarred, who has plainly been cast out by every band he ever joined. He fights as he lives: for himself, and dirty. A cudgel hangs at one hip beside the sword.";
        Level = 8;
        MaxHp = 60;
        DamageMin = 10;
        DamageMax = 16;
        ToHit = 3;
        Armor = 3;
        XpValue = 300;
        GoldMin = 5;
        GoldMax = 12;
        Aggressive = true;
        BaseReaction = 3;
        AddCombatAction("slash", "Slash", preparation: 0, recovery: 1);
        AddCombatAction("pommel", "Pommel Strike", preparation: 0, recovery: 1.5);
        FindAction("pommel")!.DamageFactor = 0.5;
        AddLoot("/domains/hollow/obj/rusty_sword", 15);
        AddLoot("/domains/hollow/obj/healing_potion", 5);
        AddLoot("/domains/hollow/obj/trail_ration", 20);
    }

    /// <summary>The once-only triggers first (by HP), then the opening actions, then the cycle, round and round.</summary>
    protected override CombatAction ChooseAction(ObjectRef target)
    {
        var n = step++;
        chosen = n < opening.Length ? opening[n] : cycle[(n - opening.Length) % cycle.Length];
        return FindAction(chosen)!;
    }

    /// <summary>What a hit with one of the special attacks does besides damage: slow, stagger or weaken.</summary>
    protected override int ModifyDamage(int damage, ObjectRef target)
    {
        for (var i = 0; i < fxAction.Length; i++)
        {
            if (chosen != fxAction[i]) continue;
            if (fxKind[i] == "slow") target.Call("AddSpeedEffect", "slow-rogue_bandit", fxValue[i], fxSeconds[i]);
            else if (fxKind[i] == "stagger") target.Call("Stagger", fxValue[i], Self);
            else target.Call("AddEffect", "weaken-rogue_bandit", "Weakened", fxSeconds[i], fxMods[i], 1.0, 0);
            TellObject(target, FxMessage(chosen) + "\n");
        }
        return damage;
    }

    private string FxMessage(string id)
    {
        if (id == "pommel") return "The pommel cracks against your head and you reel!";
        return "";
    }

    protected override void FightEnded()
    {
        step = 0;
        chosen = "";
    }
}
