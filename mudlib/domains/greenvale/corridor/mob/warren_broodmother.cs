// @inherit /std/monster

/// <summary>
/// Warren Broodmother, level 10. Part of the Greenvale Transit Corridor. Numbers and the balance reasoning:
/// docs/CORRIDOR_BALANCE.md and tests/balance_sim.py (python3 tests/balance_sim.py check compares them with this file).
/// It has no respawn of its own: the room that spawns it (AddSpawn) or lets it wander in (AddEncounter) brings a new one at its next reset.
/// </summary>
public class CmWarrenBroodmother : Monster
{
    private readonly string[] opening = new string[0];
    private readonly string[] cycle = { "fang", "fang", "spit" };
    private readonly int[] triggerHp = { 66, 33 };
    private readonly string[] triggerAction = { "brood", "brood" };
    private int step;
    private int fired;
    private string chosen = "";
    private readonly string[] fxAction = { "spit" };
    private readonly string[] fxKind = { "slow" };
    private readonly double[] fxValue = { 1.25 };
    private readonly string[] fxMods = { "" };
    private readonly int[] fxSeconds = { 10 };

    /// <summary>For the coming crafting system: the kind of skin this creature yields ("wolf"), or empty. Nothing uses it yet.</summary>
    public string Skin { get; set; } = "";

    /// <summary>For the coming crafting system: the kind of blood this creature yields ("wolf", "goblin"), or empty. Nothing uses it yet.</summary>
    public string Blood { get; set; } = "";

    public override void Create()
    {
        Name = "Warren Broodmother";
        Ids.Add("broodmother");
        Ids.Add("warren broodmother");
        Ids.Add("mother");
        ShortDesc = "the Warren Broodmother";
        LongDesc = "A swollen, pale creature the size of a cart squats in the heart of the warren, its many-jointed legs folded beneath a bloated body. Its jaws drip a greasy fluid, and the walls around it are thick with webbing and egg sacs. It lifts its head to the sound of your steps with a slow, wet hiss.";
        Level = 10;
        MaxHp = 80;
        DamageMin = 6;
        DamageMax = 10;
        ToHit = 3;
        Armor = 3;
        XpValue = 1550;
        Aggressive = true;
        DeathMessage = "The Warren Broodmother shudders, curls her legs inward, and goes still.";
        BaseReaction = 4;
        AddCombatAction("fang", "Venom Fang", preparation: 0, recovery: 1);
        AddCombatAction("spit", "Webbed Spit", preparation: 0.5, recovery: 1.5, windUp: "rears up, mandibles clicking");
        FindAction("spit")!.DamageFactor = 0.75;
        AddCombatAction("brood", "Call the Brood", preparation: 0.5, recovery: 1, windUp: "scrapes at the egg sacs with a foreleg", kind: "summon");
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
        if (action.Id == "brood")
        {
            TellRoom(room, "The egg sacs burst, and a tunnel rat scrambles out to defend its mother!\n");
            var helper = CloneObject("/domains/greenvale/corridor/mob/tunnel_rat");
            MoveObject(helper, room);
            if (target != null) helper.Call("JoinFight", target, Self); // drawn in: it waits its own Reaction Time
        }
    }

    /// <summary>What a hit with one of the special attacks does besides damage: slow, stagger or weaken.</summary>
    protected override int ModifyDamage(int damage, ObjectRef target)
    {
        for (var i = 0; i < fxAction.Length; i++)
        {
            if (chosen != fxAction[i]) continue;
            if (fxKind[i] == "slow") target.Call("AddSpeedEffect", "slow-warren_broodmother", fxValue[i], fxSeconds[i]);
            else if (fxKind[i] == "stagger") target.Call("Stagger", fxValue[i], Self);
            else target.Call("AddEffect", "weaken-warren_broodmother", "Weakened", fxSeconds[i], fxMods[i], 1.0, 0);
            TellObject(target, FxMessage(chosen) + "\n");
        }
        return damage;
    }

    private string FxMessage(string id)
    {
        if (id == "spit") return "A rope of sticky webbing smacks into you. Your limbs drag; you are slowed!";
        return "";
    }

    protected override void FightEnded()
    {
        step = 0;
        fired = 0;
        chosen = "";
    }
}
