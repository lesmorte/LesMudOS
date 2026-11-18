// @inherit /std/monster

/// <summary>
/// Rogue Goblin Veteran, level 9. Part of the Greenvale Transit Corridor. Numbers and the balance reasoning:
/// docs/CORRIDOR_BALANCE.md and tests/balance_sim.py (python3 tests/balance_sim.py check compares them with this file).
/// It has no respawn of its own: the room that spawns it (AddSpawn) or lets it wander in (AddEncounter) brings a new one at its next reset.
/// </summary>
public class CmRogueGoblinVeteran : Monster
{
    private readonly string[] opening = new string[0];
    private readonly string[] cycle = { "slash", "slash", "daze" };
    private int step;
    private string chosen = "";
    private readonly string[] fxAction = { "daze" };
    private readonly string[] fxKind = { "weaken" };
    private readonly double[] fxValue = { 0 };
    private readonly string[] fxMods = { "tohit=-2;damage=-2" };
    private readonly int[] fxSeconds = { 12 };

    /// <summary>For the coming crafting system: the kind of skin this creature yields ("wolf"), or empty. Nothing uses it yet.</summary>
    public string Skin { get; set; } = "";

    /// <summary>For the coming crafting system: the kind of blood this creature yields ("wolf", "goblin"), or empty. Nothing uses it yet.</summary>
    public string Blood { get; set; } = "goblin";

    public override void Create()
    {
        Name = "Rogue Goblin Veteran";
        Ids.Add("goblin");
        Ids.Add("veteran");
        Ids.Add("rogue goblin veteran");
        ShortDesc = "a rogue goblin veteran";
        LongDesc = "A scarred, gap-toothed goblin in a patchwork of looted plate swings a heavy flanged mace with the ease of long practice. Dozens of notches on the haft count his kills. He has outlived every warband that ever employed him.";
        Level = 9;
        MaxHp = 53;
        DamageMin = 7;
        DamageMax = 10;
        ToHit = 4;
        Armor = 5;
        XpValue = 280;
        GoldMin = 6;
        GoldMax = 16;
        Aggressive = true;
        BaseReaction = 3.5;
        AddCombatAction("slash", "Slash", preparation: 0, recovery: 1);
        AddCombatAction("daze", "Dazing Blow", preparation: 0, recovery: 1.5);
        FindAction("daze")!.DamageFactor = 0.75;
        AddLoot("/domains/hollow/obj/healing_potion", 7);
        AddLoot("/domains/hollow/obj/woodcutter_axe", 10);
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
            if (fxKind[i] == "slow") target.Call("AddSpeedEffect", "slow-rogue_goblin_veteran", fxValue[i], fxSeconds[i]);
            else if (fxKind[i] == "stagger") target.Call("Stagger", fxValue[i], Self);
            else target.Call("AddEffect", "weaken-rogue_goblin_veteran", "Weakened", fxSeconds[i], fxMods[i], 1.0, 0);
            TellObject(target, FxMessage(chosen) + "\n");
        }
        return damage;
    }

    private string FxMessage(string id)
    {
        if (id == "daze") return "The mace rings off your skull and the world swims. Your aim and your blows are dulled!";
        return "";
    }

    protected override void FightEnded()
    {
        step = 0;
        chosen = "";
    }
}
