// @inherit /std/monster

/// <summary>
/// The Overseer's chosen bodyguards: big, well-armoured goblins who guard the trophy hall as a mini-boss.
/// Three Slashes and then a Shield Bash it half-winds-up, round and round.
/// </summary>
public class GoblinEliteGuard : Monster
{
    /// <summary>The kind of skin this creature yields to the crafting system (none), or empty. See docs/CRAFTING.md.</summary>
    public string Skin { get; set; } = "";

    /// <summary>The kind of blood this creature yields to the crafting system, or empty.</summary>
    public string Blood { get; set; } = "goblin";

    public override void Create()
    {
        Name = "Goblin Elite Guard";
        Ids.Add("goblin");
        Ids.Add("elite");
        Ids.Add("elite guard");
        ShortDesc = "a goblin elite guard";
        LongDesc = "A goblin twice the size of its kin, wearing a helm of black iron and a breastplate studded with bone. It carries a long-hafted axe in both hands and stands between you and the Overseer's door as if it has been waiting all its life for you to try.";
        Level = 4;
        MaxVitality = 40;
        DamageMin = 3;
        DamageMax = 5;
        ToHit = 2;
        Armor = 3;
        XpValue = 85;
        GoldMin = 12;
        GoldMax = 22;
        Aggressive = true;
        Alert = true;
        BaseReaction = 3.5;
        AddCombatAction("slash", "Slash", preparation: 0, recovery: 1.0);
        AddLoot("/domains/hollow/obj/captains_sabre", 10);
        AddCombatAction("bash", "Shield Bash", preparation: 0.5, recovery: 1.5, windUp: "drops a shoulder behind the shield", damageMultiplier: 2);
    }

    /// <summary>Slash, Slash, Slash, Shield Bash, and round again.</summary>
    protected override CombatAction ChooseAction(ObjectRef target) => FindAction(ActionsTaken() % 4 == 3 ? "bash" : "slash")!;
}
