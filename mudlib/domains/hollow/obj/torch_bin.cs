// @inherit /std/find

/// <summary>A bin of old torches by the stair in the Tavern Cellar: every character may take one Rag Torch, once, free (docs/specs/light.md, section 11).</summary>
public class TorchBin : Find
{
    public override void Create()
    {
        Name = "Torch Bin";
        Ids.Add("bin");
        Ids.Add("torches");
        Ids.Add("torch bin");
        Ids.Add("old torches");
        Ids.Add("torch");
        ShortDesc = "a bin of old torches by the stair";
        LongDesc = "A wooden bin by the foot of the stair holds a sheaf of rag torches, bound with twine, and a card above it says to take one.";
        TakenShort = "a bin of old torches by the stair (you took one)";
        TakenHere = "A bin of old torches stands by the stair; you have taken your one.";
        TakenLong = "A wooden bin by the stair holds a sheaf of rag torches. You have taken your one.";
        EmptyHere = "A bin of old torches stands by the stair.";
        ItemLine = "You take %item% from the bin.";
        ItemPath = "/domains/hollow/obj/rag_torch";
    }
}
