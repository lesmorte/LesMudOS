// @inherit /std/item

/// <summary>The Quiet Hand's blessing, sold by Sister Merewyn. It is never handed over: buying it restores the buyer on the spot.</summary>
public class ShrineBlessing : Item
{
    public override void Create()
    {
        Name = "Blessing of the Quiet Hand";
        Ids.Add("blessing");
        Ids.Add("blessing of the quiet hand");
        ShortDesc = "the Quiet Hand's blessing";
        LongDesc = "A moment at the open stone hand above the shrine door, a cupped palm of water and a few quiet words from Sister Merewyn. It leaves you rested and whole.";
        Value = 0;
    }
}
