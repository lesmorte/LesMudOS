// @inherit /std/node

/// <summary>A wildflowers on the Greenvale corridor: 3 charges, each back in 10 minutes.</summary>
public class CnWildflowers : Node
{
    public override void Create()
    {
        Name = "Wildflowers";
        Ids.Add("flowers");
        Ids.Add("wildflowers");
        Ids.Add("wildflower");
        Ids.Add("blooms");
        ShortDesc = "a drift of wildflowers";
        PickedShort = "a drift of trampled wildflowers";
        Kind = "wildflowers";
        Charges = 3;
        RegrowSeconds = 600;
        Difficulty = 1;
        FullDesc = "A drift of wildflowers nods in the grass, yellow and blue and white, with bees going from head to head. They are in full bloom and plentiful.";
        PickedDesc = "A drift of wildflowers, picked over. Bare stalks and a few crushed petals are left where the best of the blooms were taken.";
    }
}
