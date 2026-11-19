// @inherit /std/room

/// <summary>
/// The Craftsman's Workshop: home of the Workbench (a crafting station object, which answers to "bench" itself, so the room
/// has no detail of that name) and of Tobin Fettle.
/// </summary>
public class VhWorkshop : Room
{
    public override void Create()
    {
        ShortDesc = "The Craftsman's Workshop";
        LongDesc = "Shavings crunch underfoot in this cluttered workshop, and every wall is hung with tools, half-mended chairs and things nobody has yet found a name for. A heavy workbench of black oak runs along the back, with a great iron vice bolted to one end. Pegboards of chisels, awls and pliers climb to the rafters, and a lamp on a chain swings low over the work. A cracked slate by the door lists the jobs in hand.";
        Exits["out"] = "/domains/hollow/village/square";
        Safe = true;

        AddDetail("vice,vise,iron vice", "A great iron vice with a screw as thick as a wrist. Its jaws are padded with scraps of leather, and it has a way of holding things that you would not like to test with a finger.");
        AddDetail("tools,pegboard,pegboards,chisels,awls,pliers", "Chisels, awls, pliers, files and planes, each on its own peg with its outline painted behind it so the empty ones show at once. Tobin knows every gap by heart.");
        AddDetail("chairs,chair,half-mended chairs,clutter,things", "Three chairs in three stages of repair, a cartwheel with no cart, a music box missing its handle and a lantern with a very guilty look about it.");
        AddDetail("lamp,chain", "A tin lamp on a long chain, swung low over the bench so that the light falls just where the work is. It sways slightly whenever the door opens.");
        AddReadable("slate,cracked slate,jobs,list", "The slate reads:\n  \"IN HAND: Benson's gate hinge. Mara's stool. The guard's boot-heel.\n  WAITING: Harland's cellar door (he has not told me why).\n  NOT TAKEN: anything that was once a clock.\"", "A cracked slate hangs by the door. You could {cmd}read{/} it.");

        AddSpawn("/domains/hollow/obj/craft/workbench");
        AddSpawn("/domains/hollow/npc/tobin");
    }
}
