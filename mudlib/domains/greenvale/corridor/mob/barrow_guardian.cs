// @inherit /std/monster

/// <summary>
/// Barrow Guardian, level 10. Part of the Greenvale Transit Corridor. Numbers and the balance reasoning:
/// docs/CORRIDOR_BALANCE.md and tests/balance_sim.py (python3 tests/balance_sim.py check compares them with this file).
/// It has no respawn of its own: the room that spawns it (AddSpawn) or lets it wander in (AddEncounter) brings a new one at its next reset.
/// </summary>
public class CmBarrowGuardian : Monster
{
    private readonly string[] opening = new string[0];
    private readonly string[] cycle = { "slam", "slam", "quake" };
    private readonly int[] triggerHp = { 50 };
    private readonly string[] triggerAction = { "stone" };
    private int step;
    private int fired;
    private string chosen = "";
    private readonly string[] fxAction = { "quake" };
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
        Name = "Barrow Guardian";
        Ids.Add("guardian");
        Ids.Add("barrow guardian");
        Ids.Add("statue");
        ShortDesc = "a barrow guardian";
        LongDesc = "A massive figure of weathered grey stone, vaguely like a man, stands against the barrow wall with its fists resting on the pommel of a stone sword as long as a door. Chipped runes glow faintly across its chest. It does not breathe, and yet it seems to watch.";
        Level = 10;
        MaxHp = 150;
        DamageMin = 10;
        DamageMax = 15;
        ToHit = 3;
        Armor = 4;
        XpValue = 880;
        GoldMin = 10;
        GoldMax = 24;
        Aggressive = true;
        DeathMessage = "The Barrow Guardian cracks along its length and collapses into rubble.";
        BaseReaction = 4.5;
        AddCombatAction("slam", "Stone Fist", preparation: 0, recovery: 1.5);
        AddCombatAction("quake", "Earthshaker", preparation: 1, recovery: 2, windUp: "lifts both stone fists high", damageMultiplier: 2, strike: "brings both fists down and the floor jumps!");
        AddCombatAction("stone", "Stone Skin", preparation: 0.5, recovery: 0.5, windUp: "lets the runes on its chest flare", kind: "buff");
    }

    /// <summary>The once-only triggers first (by HP), then the opening actions, then the cycle, round and round.</summary>
    protected override CombatAction ChooseAction(ObjectRef target)
    {
        for (var i = 0; i < triggerHp.Length; i++)
        {
            if ((fired & (1 << i)) == 0 && Hp * 100 <= triggerHp[i] * QueryMaxHp())
            {
                fired |= 1 << i;
                chosen = triggerAction[i];
                return FindAction(chosen)!;
            }
        }
        var n = step++;
        chosen = n < opening.Length ? opening[n] : cycle[(n - opening.Length) % cycle.Length];
        return FindAction(chosen)!;
    }

    protected override void Special(CombatAction action, ObjectRef? target)
    {
        var room = Environment();
        if (room == null) return;
        if (action.Id == "stone")
        {
            AddEffect("stone", "Stone Skin", 15, "armor=3", 1, 0);
            TellRoom(room, "The runes on the guardian's chest flare, and its surface hardens like polished granite!\n");
        }
    }

    /// <summary>What a hit with one of the special attacks does besides damage: slow, stagger or weaken.</summary>
    protected override int ModifyDamage(int damage, ObjectRef target)
    {
        for (var i = 0; i < fxAction.Length; i++)
        {
            if (chosen != fxAction[i]) continue;
            if (fxKind[i] == "slow") target.Call("AddSpeedEffect", "slow-barrow_guardian", fxValue[i], fxSeconds[i]);
            else if (fxKind[i] == "stagger") target.Call("Stagger", fxValue[i], Self);
            else target.Call("AddEffect", "weaken-barrow_guardian", "Weakened", fxSeconds[i], fxMods[i], 1.0, 0);
            TellObject(target, FxMessage(chosen) + "\n");
        }
        return damage;
    }

    private string FxMessage(string id)
    {
        if (id == "quake") return "The shock of the blow flings you off balance!";
        return "";
    }

    protected override void FightEnded()
    {
        step = 0;
        fired = 0;
        chosen = "";
    }
}
