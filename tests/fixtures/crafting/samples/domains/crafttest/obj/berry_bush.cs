// @inherit /std/node
public class CtBerryBush : Node
{
    public override void Create()
    {
        Name = "berry bush";
        ShortDesc = "a berry bush";
        PickedShort = "a picked-over berry bush";
        FullDesc = "A bramble heavy with berries.";
        PickedDesc = "Not a berry left.";
        Kind = "berry bush";
        Charges = 2;
        RegrowSeconds = 15;
        Difficulty = 1;
    }
}
