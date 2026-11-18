// @inherit /std/node
public class CtHerbPatch : Node
{
    public override void Create()
    {
        Name = "herb patch";
        ShortDesc = "a herb patch";
        FullDesc = "Healing herbs grow thick here.";
        Kind = "herb patch";
        Charges = 30;
        RegrowSeconds = 600;
        Difficulty = 1;
    }
}
