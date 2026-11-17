// @inherit /std/monster

/// <summary>
/// The boss of the Forgotten Cellars. Three abilities, all built on the
/// Living hooks: Battle Cry (first round, then +2 damage for 5 rounds),
/// Power Strike (20% of hits do double damage) and Summon Goblin Minion
/// (once, the first time his HP falls to half).
/// </summary>
public class GoblinOverseer : Monster
{
    private const string MinionPath = "/domains/hollow/mob/goblin_minion";

    private bool cried; // Battle Cry used in this fight
    private int cryRounds; // rounds of bonus damage left
    private bool cryActive; // the bonus applies to this round
    private bool summoned; // Summon used in this life

    public override void Create()
    {
        Name = "Goblin Overseer";
        Ids.Add("overseer");
        Ids.Add("goblin");
        ShortDesc = "the Goblin Overseer";
        LongDesc = "Taller and better armed than any goblin you have seen, the Overseer wears a crow-marked sash over scavenged mail. His eyes are sharp, and he looks at you like a problem to be solved.";
        Level = 5;
        MaxHp = 54;
        DamageMin = 4;
        DamageMax = 6;
        ToHit = 2;
        Armor = 3;
        XpValue = 250;
        Aggressive = true;
        DeathMessage = "The Goblin Overseer staggers, drops to his knees, and falls dead among his maps.";

        AddLoot("/domains/hollow/obj/overseers_axe", 100);
        AddLoot("/domains/hollow/obj/commanders_vest", 100);
        AddLoot("/domains/hollow/obj/bag_of_coins", 100);
        AddLoot("/domains/hollow/obj/overseers_orders", 100);
    }

    protected override void CombatRound(ObjectRef target)
    {
        cryActive = false;
        var room = Environment();
        if (room == null) return;

        // Summon Goblin Minion: once per life, when his HP first falls to half.
        if (!summoned && Hp * 2 <= QueryMaxHp())
        {
            summoned = true;
            TellRoom(room, "The Goblin Overseer snarls an order, and a goblin minion scrambles out of the shadows!\n");
            var minion = CloneObject(MinionPath);
            MoveObject(minion, room);
            minion.Call("StartFight", target);
        }

        // Battle Cry: instead of the first attack of a fight.
        if (!cried)
        {
            cried = true;
            cryRounds = 5;
            SkipAttack = true;
            TellRoom(room, "The Goblin Overseer throws back his head and bellows a BATTLE CRY! His eyes blaze, and his next blows will fall harder.\n");
            return;
        }
        if (cryRounds > 0)
        {
            cryRounds--;
            cryActive = true;
        }
    }

    protected override int ModifyDamage(int damage, ObjectRef target)
    {
        if (cryActive) damage += 2;
        if (Random(100) < 20)
        {
            damage *= 2;
            var room = Environment();
            if (room != null) TellRoom(room, "The Goblin Overseer sets his feet and delivers a crushing POWER STRIKE!\n");
        }
        return damage;
    }

    protected override void FightEnded()
    {
        cried = false;
        cryRounds = 0;
        cryActive = false;
    }
}
