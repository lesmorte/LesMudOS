// @inherit /std/node

/// <summary>A thistle patch on the Greenvale corridor: 3 charges, each back in 10 minutes.</summary>
public class CnThistlePatch : Node
{
    public override void Create()
    {
        Name = "Thistle Patch";
        Ids.Add("thistle");
        Ids.Add("thistles");
        Ids.Add("patch");
        Ids.Add("thistle patch");
        ShortDesc = "a patch of thistles";
        PickedShort = "a patch of stripped thistles";
        Kind = "thistle patch";
        Charges = 3;
        RegrowSeconds = 600;
        Difficulty = 2;
        FullDesc = "A patch of thistles stands shoulder high, grey-green and wicked with spines, each head a purple brush humming with insects. Anyone who wants the heads will have to pay for them in scratches.";
        PickedDesc = "A patch of thistles from which the heads have been cut. The stalks stand bare and spiny, and a few seeds drift off them on the wind.";
    }
}
