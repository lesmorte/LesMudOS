// @inherit /std/monster

/// <summary>
/// A huge, slow-looking toad that lurks in the flooded tunnel and takes a great deal of punishment.
/// Two actions in a fixed rhythm: two Tongue Lashes, then a Crushing Leap that it winds up for 2 seconds.
/// </summary>
public class GiantCaveToad : Monster
{
    /// <summary>The kind of skin this creature yields to the crafting system ("toad"), or empty. See docs/CRAFTING.md.</summary>
    public string Skin { get; set; } = "toad";

    /// <summary>The kind of blood this creature yields to the crafting system, or empty.</summary>
    public string Blood { get; set; } = "toad";

    public override void Create()
    {
        Name = "Giant Cave Toad";
        Ids.Add("toad");
        Ids.Add("cave toad");
        ShortDesc = "a giant cave toad";
        LongDesc = "A toad the size of a sheep, its warty back slick with black water and its throat pulsing slowly. Its pale eyes do not blink, and it waits in the shallows with a patience that is much more unsettling than speed would be.";
        Level = 3;
        DarkSight = false; // pale, unblinking eyes: it feels the water, not the shape (docs/specs/light.md)
        MaxVitality = 66;
        DamageMin = 2;
        DamageMax = 4;
        ToHit = 0;
        Armor = 0;
        XpValue = 50;
        GoldMin = 0;
        GoldMax = 0;
        Aggressive = true;
        BaseReaction = 4.5;
        AddCombatAction("lash", "Tongue Lash", preparation: 0, recovery: 1.0);
        AddCombatAction("leap", "Crushing Leap", preparation: 1.0, recovery: 2.0, windUp: "gathers its legs under it, throat swelling", damageMultiplier: 2);
    }

    /// <summary>Tongue Lash, Tongue Lash, Crushing Leap, and round again.</summary>
    protected override CombatAction ChooseAction(ObjectRef target) => FindAction(ActionsTaken() % 3 == 2 ? "leap" : "lash")!;
}
