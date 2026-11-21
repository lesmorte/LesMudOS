// @inherit /std/weapon

/// <summary>The fine step of the spear line: a rare drop of the goblin warriors in the Goblin Barracks.</summary>
public class GoblinWarSpear : Weapon
{
    public override void Create()
    {
        Name = "Goblin War-Spear";
        Ids.Add("spear");
        Ids.Add("war-spear");
        Ids.Add("war spear");
        Ids.Add("goblin spear");
        Ids.Add("goblin war-spear");
        ShortDesc = "a goblin war-spear";
        LongDesc = "A heavy spear of black ash, its leaf-shaped head hammered out of a human sword and riveted to the shaft. Crude crow feathers are tied under the head. It is ugly, and it is well balanced, and that is the worst thing about it.";
        Category = "spear";
        DamageMin = 13;
        DamageMax = 18;
        ToHit = 1;
        ReactionMod = 0.5;
        Recovery = 1.0;
        Value = 70;
    }
}
