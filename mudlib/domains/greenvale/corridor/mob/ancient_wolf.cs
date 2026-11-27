// @inherit /std/monster

/// <summary>
/// Ancient Wolf, level 9. Part of the Greenvale Transit Corridor. Numbers and the balance reasoning:
/// docs/CORRIDOR_BALANCE.md and tests/balance_sim.py (python3 tests/balance_sim.py check compares them with this file).
/// It has no respawn of its own: the room that spawns it (AddSpawn) or lets it wander in (AddEncounter) brings a new one at its next reset.
/// </summary>
public class CmAncientWolf : Monster
{
    private readonly string[] opening = new string[0];
    private readonly string[] cycle = { "bite" };
    private readonly int[] triggerVitality = { 60 };
    private readonly string[] triggerAction = { "howl" };
    private int step;
    private int fired;
    private string chosen = "";

    /// <summary>For the coming crafting system: the kind of skin this creature yields ("wolf"), or empty. Nothing uses it yet.</summary>
    public string Skin { get; set; } = "wolf";

    /// <summary>For the coming crafting system: the kind of blood this creature yields ("wolf", "goblin"), or empty. Nothing uses it yet.</summary>
    public string Blood { get; set; } = "wolf";

    public override void Create()
    {
        Name = "Ancient Wolf";
        Ids.Add("wolf");
        Ids.Add("ancient wolf");
        ShortDesc = "an ancient wolf";
        Tags.Add("beast"); Tags.Add("melee"); Tags.Add("piercing"); Tags.Add("bite");
        LongDesc = "A huge wolf, grey to the point of white about the muzzle, lies in its dell as though the hollow had been shaped around it. One ear is gone and an old scar runs the length of its flank. It lifts its great head and meets your eyes as if it had been expecting you.";
        Level = 9;
        MaxVitality = 69;
        DamageMin = 8;
        DamageMax = 13;
        ToHit = 3;
        Armor = 3;
        XpValue = 550;
        Aggressive = true;
        BaseReaction = 3;
        AddCombatAction("bite", "Rending Bite", preparation: 0, recovery: 1);
        AddCombatAction("howl", "Howl", preparation: 0.5, recovery: 0.5, windUp: "throws back its great head", kind: "buff");
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
        if (action.Id == "howl")
        {
            AddEffect("howl", "Howl", 25, "damage=2", 0.75, 0);
            TellRoom(room, "The ancient wolf's howl rolls through the trees and its eyes blaze. It fights with a young wolf's speed!\n");
        }
    }

    protected override void FightEnded()
    {
        step = 0;
        fired = 0;
        chosen = "";
    }
}
