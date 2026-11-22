// @inherit /std/monster

/// <summary>
/// The boss of the Forgotten Cellars. Four actions in a fixed pattern that players can learn:
/// Battle Cry first (his damage is +2 for the next 30 seconds), then Axe Slash, Axe Slash, Power Strike
/// (double damage, announced 2 seconds before it lands) and round again; and once, as his next action after
/// his Vitality first falls to half, Summon Goblin Minion.
/// </summary>
public class GoblinOverseer : Monster
{
    /// <summary>The kind of skin this creature yields to the crafting system (none), or empty. See docs/CRAFTING.md.</summary>
    public string Skin { get; set; } = "";

    /// <summary>The kind of blood this creature yields to the crafting system, or empty.</summary>
    public string Blood { get; set; } = "goblin";

    private const string MinionPath = "/domains/hollow/mob/goblin_minion";

    private long cryUntil; // the Battle Cry bonus lasts until this time (ms)
    private bool summoned; // Summon used in this life
    private int attacks; // Axe Slashes and Power Strikes chosen in this fight

    public override void Create()
    {
        Name = "Goblin Overseer";
        ChronicleEvent = "boss.goblin_overseer";
        ChronicleSubject = "goblin-overseer";
        Ids.Add("overseer");
        Ids.Add("goblin");
        ShortDesc = "the Goblin Overseer";
        LongDesc = "Taller and better armed than any goblin you have seen, the Overseer wears a crow-marked sash over scavenged mail. His eyes are sharp, and he looks at you like a problem to be solved.";
        Level = 5;
        MaxVitality = 114;
        DamageMin = 4;
        DamageMax = 7;
        ToHit = 2;
        Armor = 3;
        XpValue = 250;
        Aggressive = true;
        Boss = true;
        DeathMessage = "The Goblin Overseer staggers, drops to his knees, and falls dead among his maps.";
        BaseReaction = 4.0;
        AddCombatAction("slash", "Axe Slash", preparation: 0, recovery: 1.0);
        AddCombatAction("power", "Power Strike", preparation: 1.0, recovery: 2.0, windUp: "draws his axe far back", damageMultiplier: 2,
            strike: "brings the axe down in a crushing POWER STRIKE!");
        AddCombatAction("cry", "Battle Cry", preparation: 0, recovery: 0.5, kind: "cry");
        AddCombatAction("summon", "Summon Goblin Minion", preparation: 0.5, recovery: 1.0, windUp: "draws a deep breath to call for help", kind: "summon");

        AddLoot("/domains/hollow/obj/overseers_axe", 100);
        AddLoot("/domains/hollow/obj/commanders_vest", 100);
        AddLoot("/domains/hollow/obj/bag_of_coins", 100);
        AddLoot("/domains/hollow/obj/overseers_orders", 100);
    }

    /// <summary>Battle Cry as the first action of a fight; Summon once, when his Vitality has first fallen to half; otherwise Axe Slash, Axe Slash, Power Strike.</summary>
    protected override CombatAction ChooseAction(ObjectRef target)
    {
        if (ActionsTaken() == 0) return FindAction("cry")!;
        if (!summoned && Vitality * 2 <= QueryMaxVitality())
        {
            summoned = true;
            return FindAction("summon")!;
        }
        attacks++;
        return FindAction(attacks % 3 == 0 ? "power" : "slash")!;
    }

    protected override void Special(CombatAction action, ObjectRef? target)
    {
        var room = Environment();
        if (room == null) return;
        if (action.Kind == "cry")
        {
            cryUntil = TimeMs() + 30000;
            TellRoom(room, "The Goblin Overseer throws back his head and bellows a BATTLE CRY! His eyes blaze, and his next blows will fall harder.\n");
        }
        else if (action.Kind == "summon" && target != null)
        {
            TellRoom(room, "The Goblin Overseer snarls an order, and a goblin minion scrambles out of the shadows!\n");
            var minion = CloneObject(MinionPath);
            MoveObject(minion, room);
            minion.Call("JoinFight", target, Self); // drawn in: it waits its own Reaction Time before the first blow
        }
    }

    protected override int ModifyDamage(int damage, ObjectRef target) => TimeMs() < cryUntil ? damage + 2 : damage;

    protected override void FightEnded()
    {
        cryUntil = 0;
        attacks = 0;
    }
}
