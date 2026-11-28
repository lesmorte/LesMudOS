// @inherit /std/monster

/// <summary>
/// Bandit Leader, level 10. Part of the Greenvale Transit Corridor. Numbers and the balance reasoning:
/// docs/CORRIDOR_BALANCE.md and tests/balance_sim.py (python3 tests/balance_sim.py check compares them with this file).
/// It has no respawn of its own: the room that spawns it (AddSpawn) or lets it wander in (AddEncounter) brings a new one at its next reset.
/// </summary>
public class CmBanditLeader : Monster
{
    private readonly string[] opening = new string[0];
    private readonly string[] cycle = { "slash", "slash", "smash" };
    private readonly int[] triggerVitality = { 50 };
    private readonly string[] triggerAction = { "rally" };
    private int step;
    private int fired;
    private string chosen = "";
    private readonly string[] fxAction = { "smash" };
    private readonly string[] fxKind = { "stagger" };
    private readonly double[] fxValue = { 3 };
    private readonly string[] fxMods = { "" };
    private readonly int[] fxSeconds = { 0 };

    /// <summary>For the coming crafting system: the kind of skin this creature yields ("wolf"), or empty. Nothing uses it yet.</summary>
    public string Skin { get; set; } = "";

    /// <summary>For the coming crafting system: the kind of blood this creature yields ("wolf", "goblin"), or empty. Nothing uses it yet.</summary>
    public string Blood { get; set; } = "";

    public override void Create()
    {
        Name = "Bandit Leader";
        Ids.Add("leader");
        Ids.Add("bandit leader");
        Ids.Add("bandit");
        ShortDesc = "the Bandit Leader";
        Tags.Add("human"); Tags.Add("melee"); Tags.Add("slashing"); Tags.Add("sword");
        LongDesc = "A tall, silver-bearded man in a dark coat trimmed with fox fur sits at the head of the camp like a lord at his own table, a curved sabre across his knees and a round shield at his side. The rings on his fingers were taken from other people's hands. His cool gaze takes your measure and finds it wanting.";
        Level = 10;
        MaxVitality = 100;
        DamageMin = 6;
        DamageMax = 10;
        ToHit = 4;
        Armor = 4;
        XpValue = 1550;
        GoldMin = 30;
        GoldMax = 60;
        Aggressive = true;
        Boss = true;
        DeathMessage = "The Bandit Leader sinks to one knee, lets the sabre fall from his fingers, and topples over.";
        BaseReaction = 3.5;
        AddCombatAction("slash", "Sabre Slash", preparation: 0, recovery: 1);
        AddCombatAction("smash", "Shield Smash", preparation: 0.5, recovery: 1.5, windUp: "swings his shield up and round", reach: "melee", damage: "blunt", tagKind: "slam");
        FindAction("smash")!.DamageFactor = 0.75;
        AddCombatAction("rally", "Call the Pack", preparation: 0.5, recovery: 1, windUp: "puts two fingers to his mouth", kind: "summon");
        AddLoot("/domains/hollow/obj/bag_of_coins", 100);
        AddLoot("/domains/hollow/obj/healing_potion", 60);
        AddLoot("/domains/hollow/obj/enchanted_dagger", 10);
    }

    /// <summary>The once-only triggers first (by Vitality), then the opening actions, then the cycle, round and round.</summary>
    protected override CombatAction ChooseAction(ObjectRef target)
    {
        for (var i = 0; i < triggerVitality.Length; i++)
        {
            // A trigger is spent when its action really happens (Special), not when it is chosen: a barred or interrupted call for help comes back.
            if ((fired & (1 << i)) == 0 && Vitality * 100 <= triggerVitality[i] * QueryMaxVitality() && !QueryActionBarred(FindAction(triggerAction[i])!.Kind))
            {
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
        if (action.Id == "rally")
        {
            for (var i = 0; i < triggerAction.Length; i++) if (triggerAction[i] == action.Id) fired |= 1 << i;
            TellRoom(room, "A shrill whistle splits the air, and a lookout comes crashing out of the brush!\n");
            var helper = CloneObject("/domains/greenvale/corridor/mob/bandit_lookout");
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
            if (fxKind[i] == "slow") target.Call("AddSpeedEffect", "slow-bandit_leader", fxValue[i], fxSeconds[i]);
            else if (fxKind[i] == "stagger") target.Call("Stagger", fxValue[i], Self);
            else target.Call("AddEffect", "weaken-bandit_leader", "Weakened", fxSeconds[i], fxMods[i], 1.0, 0);
            TellObject(target, FxMessage(chosen) + "\n");
        }
        return damage;
    }

    private string FxMessage(string id)
    {
        if (id == "smash") return "The shield slams into you and you stagger!";
        return "";
    }

    protected override void FightEnded()
    {
        step = 0;
        fired = 0;
        chosen = "";
    }
}
