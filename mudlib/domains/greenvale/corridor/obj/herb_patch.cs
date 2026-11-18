// @inherit /std/node

/// <summary>A herb patch on the Greenvale corridor: 3 charges, each back in 10 minutes.</summary>
public class CnHerbPatch : Node
{
    public override void Create()
    {
        Name = "Herb Patch";
        ShortDesc = "a patch of herbs";
        PickedShort = "a trampled patch of herbs";
        Kind = "herb patch";
        Charges = 3;
        RegrowSeconds = 600;
        Difficulty = 2;
        FullDesc = "A patch of low green herbs grows thick in the shelter of the verge, grey-leaved and sharp-smelling, the kind a healer would cross a field for. The stems are tender and the best leaves are small.";
        PickedDesc = "A patch of herbs, stripped to the stems. The ground between them is scuffed bare and the sharp smell has faded to green.";
    }
}
