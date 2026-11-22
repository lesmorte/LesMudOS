// @inherit /std/monster

/// <summary>
/// Black Bear, level 8. Part of the Greenvale Transit Corridor. Numbers and the balance reasoning:
/// docs/CORRIDOR_BALANCE.md and tests/balance_sim.py (python3 tests/balance_sim.py check compares them with this file).
/// It has no respawn of its own: the room that spawns it (AddSpawn) or lets it wander in (AddEncounter) brings a new one at its next reset.
/// </summary>
public class CmBlackBear : Monster
{
    private readonly string[] opening = new string[0];
    private readonly string[] cycle = { "swipe", "swipe", "maul" };
    private readonly int[] triggerVitality = { 50 };
    private readonly string[] triggerAction = { "roar" };
    private int step;
    private int fired;
    private string chosen = "";

    /// <summary>For the coming crafting system: the kind of skin this creature yields ("wolf"), or empty. Nothing uses it yet.</summary>
    public string Skin { get; set; } = "bear";

    /// <summary>For the coming crafting system: the kind of blood this creature yields ("wolf", "goblin"), or empty. Nothing uses it yet.</summary>
    public string Blood { get; set; } = "bear";

    public override void Create()
    {
        Name = "Black Bear";
        Ids.Add("bear");
        Ids.Add("black bear");
        ShortDesc = "a black bear";
        LongDesc = "A full-grown black bear, glossy and thick-necked, stands on its hind legs to scratch its back against an ancient trunk. Its small eyes are mild until they are not. The claw marks scored in the bark would flay a man.";
        Level = 8;
        MaxVitality = 54;
        DamageMin = 8;
        DamageMax = 13;
        ToHit = 3;
        Armor = 3;
        XpValue = 280;
        Aggressive = false;
        BaseReaction = 4.5;
        AddCombatAction("swipe", "Swipe", preparation: 0, recovery: 1.5);
        AddCombatAction("maul", "Maul", preparation: 1, recovery: 2, windUp: "rears up on its hind legs, claws spread wide", damageMultiplier: 2, strike: "comes down in a crushing maul!");
        AddCombatAction("roar", "Bellowing Roar", preparation: 0.5, recovery: 0.5, windUp: "fills its great chest", kind: "buff");
    }

    /// <summary>The once-only triggers first (by Vitality), then the opening actions, then the cycle, round and round.</summary>
    protected override CombatAction ChooseAction(ObjectRef target)
    {
        for (var i = 0; i < triggerVitality.Length; i++)
        {
            if ((fired & (1 << i)) == 0 && Vitality * 100 <= triggerVitality[i] * QueryMaxVitality())
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
        if (action.Id == "roar")
        {
            AddEffect("roar", "Bellowing Roar", 30, "damage=3", 0.85, 0);
            TellRoom(room, "The black bear lets out a bellowing roar that shakes the leaves. Wounded and furious, it fights faster and harder!\n");
        }
    }

    protected override void FightEnded()
    {
        step = 0;
        fired = 0;
        chosen = "";
    }
}
