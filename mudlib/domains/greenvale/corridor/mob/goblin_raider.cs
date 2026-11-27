// @inherit /std/monster

/// <summary>
/// Goblin Raider, level 7. Part of the Greenvale Transit Corridor. Numbers and the balance reasoning:
/// docs/CORRIDOR_BALANCE.md and tests/balance_sim.py (python3 tests/balance_sim.py check compares them with this file).
/// It has no respawn of its own: the room that spawns it (AddSpawn) or lets it wander in (AddEncounter) brings a new one at its next reset.
/// </summary>
public class CmGoblinRaider : Monster
{
    private readonly string[] opening = new string[0];
    private readonly string[] cycle = { "hack" };
    private readonly int[] triggerVitality = { 50 };
    private readonly string[] triggerAction = { "whoop" };
    private int step;
    private int fired;
    private string chosen = "";

    /// <summary>For the coming crafting system: the kind of skin this creature yields ("wolf"), or empty. Nothing uses it yet.</summary>
    public string Skin { get; set; } = "";

    /// <summary>For the coming crafting system: the kind of blood this creature yields ("wolf", "goblin"), or empty. Nothing uses it yet.</summary>
    public string Blood { get; set; } = "goblin";

    public override void Create()
    {
        Name = "Goblin Raider";
        Ids.Add("goblin");
        Ids.Add("raider");
        Ids.Add("goblin raider");
        ShortDesc = "a goblin raider";
        Tags.Add("goblin"); Tags.Add("melee"); Tags.Add("slashing"); Tags.Add("axe");
        LongDesc = "A heavy-browed goblin in a stolen carter's jerkin, his arms ringed with looted copper, swings a notched cleaver as he watches the forest road. Scars cross his forearms and his grin shows broken teeth. He has the swagger of one who has lived off other people's wagons for a long time.";
        Level = 7;
        MaxVitality = 53;
        DamageMin = 7;
        DamageMax = 11;
        ToHit = 2;
        Armor = 3;
        XpValue = 170;
        GoldMin = 4;
        GoldMax = 10;
        Aggressive = true;
        BaseReaction = 3;
        AddCombatAction("hack", "Cleaver Hack", preparation: 0, recovery: 1);
        AddCombatAction("whoop", "War Whoop", preparation: 0.5, recovery: 0.5, windUp: "throws back his head to howl a raiding cry", kind: "buff");
        AddLoot("/domains/hollow/obj/rusty_sword", 15);
        AddLoot("/domains/hollow/obj/training_spear", 15);
        AddLoot("/domains/hollow/obj/healing_potion", 4);
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
        if (action.Id == "whoop")
        {
            AddEffect("whoop", "War Whoop", 25, "damage=3", 0.85, 0);
            TellRoom(room, "The goblin raider whoops wildly, his eyes alight with bloodlust. His blows will fall faster and harder!\n");
        }
    }

    protected override void FightEnded()
    {
        step = 0;
        fired = 0;
        chosen = "";
    }
}
