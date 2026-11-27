// @inherit /std/monster

/// <summary>
/// Highway Bandit, level 8. Part of the Greenvale Transit Corridor. Numbers and the balance reasoning:
/// docs/CORRIDOR_BALANCE.md and tests/balance_sim.py (python3 tests/balance_sim.py check compares them with this file).
/// It has no respawn of its own: the room that spawns it (AddSpawn) or lets it wander in (AddEncounter) brings a new one at its next reset.
/// </summary>
public class CmHighwayBandit : Monster
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
        Name = "Highway Bandit";
        Ids.Add("bandit");
        Ids.Add("highwayman");
        Ids.Add("highway bandit");
        ShortDesc = "a highway bandit";
        Tags.Add("human"); Tags.Add("melee"); Tags.Add("slashing"); Tags.Add("sword");
        LongDesc = "A road-hardened man in a travel-stained cloak, a sabre at his belt and a crossbow slung across his back, leans against a milestone as though waiting for a friend. He makes no secret of what he wants. A brass-buckled belt marks him as one of the camp's regulars.";
        Level = 8;
        MaxVitality = 59;
        DamageMin = 6;
        DamageMax = 9;
        ToHit = 3;
        Armor = 3;
        XpValue = 195;
        GoldMin = 5;
        GoldMax = 13;
        Aggressive = true;
        BaseReaction = 3;
        AddCombatAction("slash", "Slash", preparation: 0, recovery: 1);
        AddCombatAction("pommel", "Pommel Strike", preparation: 0, recovery: 1.5, reach: "melee", damage: "blunt", tagKind: "club");
        FindAction("pommel")!.DamageFactor = 0.5;
        AddLoot("/domains/hollow/obj/rusty_sword", 15);
        AddLoot("/domains/hollow/obj/healing_potion", 6);
        AddLoot("/domains/hollow/obj/trail_ration", 20);
    }

    /// <summary>The once-only triggers first (by Vitality), then the opening actions, then the cycle, round and round.</summary>
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
            if (fxKind[i] == "slow") target.Call("AddSpeedEffect", "slow-highway_bandit", fxValue[i], fxSeconds[i]);
            else if (fxKind[i] == "stagger") target.Call("Stagger", fxValue[i], Self);
            else target.Call("AddEffect", "weaken-highway_bandit", "Weakened", fxSeconds[i], fxMods[i], 1.0, 0);
            TellObject(target, FxMessage(chosen) + "\n");
        }
        return damage;
    }

    private string FxMessage(string id)
    {
        if (id == "pommel") return "The sabre's pommel cracks against your head and you reel!";
        return "";
    }

    protected override void FightEnded()
    {
        step = 0;
        chosen = "";
    }
}
