// @inherit /std/node

/// <summary>A berry bush on the Greenvale corridor: 3 charges, each back in 10 minutes.</summary>
public class CnBerryBush : Node
{
    public override void Create()
    {
        Name = "Berry Bush";
        Ids.Add("bush");
        Ids.Add("berry bush");
        Ids.Add("bushes");
        Ids.Add("berries");
        ShortDesc = "a berry bush";
        PickedShort = "a picked-over berry bush";
        Kind = "berry bush";
        Charges = 3;
        RegrowSeconds = 600;
        Difficulty = 1;
        FullDesc = "A tangled bush heavy with dark red berries, the nearest ones already bruised by passing sleeves. Bees work the low blossoms and the thorns are long.";
        PickedDesc = "A tangled bush that has been picked clean. A few shrivelled berries cling to the lowest twigs, and the best branches have been bent back and broken.";
    }
}
