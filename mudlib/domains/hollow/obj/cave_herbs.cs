// @inherit /std/node

/// <summary>A crowd of pale healing herbs in a crack of the Mushroom Cavern wall, hidden (Mushroom Cavern's HideNode, found by feel). Two charges, each back in 15 minutes.</summary>
public class CaveHerbs : Node
{
    public override void Create()
    {
        Name = "Cave Herbs";
        ShortDesc = "a crowd of pale herbs in a crack";
        PickedShort = "a crack in the wall with a few pale stalks left";
        Kind = "herb patch";
        Charges = 2;
        RegrowSeconds = 900;
        Difficulty = 3;
        FullDesc = "A handful of pale-leaved herbs grows in a crack at the foot of the wall, fed by the seep and the spore dust. Not many; the smell of them is a healing herb's, for all their colour.";
        PickedDesc = "A crack in the wall, with a few pale stalks left.";
    }
}
