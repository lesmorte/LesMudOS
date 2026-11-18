// @inherit /std/room

public class GrTheWagonHalt : Room
{
    public override void Create()
    {
        ShortDesc = "The Green Road, the Wagon Halt";
        LongDesc = "The trees draw back for a space big enough to turn a wagon, and the ground is trampled flat and rutted by many halts. A hitching rail runs along one side, black with years of rope, and a stone trough fed by a thin spring stands beside it. The light is a little better here, grey instead of green. A path leads west into the trees past a stack of rotting cordwood. It is a place that is always about to be left.";
        Exits["south"] = "/domains/greenvale/corridor/green_road/m06";
        Exits["north"] = "/domains/greenvale/corridor/green_road/m08";
        Exits["west"] = "/domains/greenvale/corridor/green_road/w5";
        Safe = false;
        DangerBand(6, 7);

        AddDetail("halt,clearing,space,turning place", "A broad patch of bare ground where a road widens to let wagons pass each other and stop. Axle grease stains the ground and a cart wheel has been left leaning against a tree, abandoned.");
        AddDetail("rail,hitching rail,rope,ropes,hitching", "A squared timber on posts, chewed along its top by bored horses and worn with rope.");
        AddDetail("trough,spring,water,stone trough", "A stone trough with a trickle running into it from a pipe of hollowed wood. The water is cold and sweet, and a carved frog sits at the lip.");
        AddDetail("path,west,trail,cordwood,wood,stack", "A track leaves the halt westward past a stack of cordwood that nobody has come back for. Fresh boot prints go in; fewer come out.");
        AddDetail("wheel,cart wheel,abandoned wheel", "A cartwheel with three spokes broken, propped against a tree. Someone carved a name into the rim and then scratched it out.");
        AddDetail("tracks,ruts,prints,marks", "Dozens of wagon turns are written into the dirt like handwriting. The newest ones are tidy, and head north.");

        AddSpawn("/domains/greenvale/corridor/green_road/npc/fulk");
    }
}
