// @inherit /std/room

public class VrMeadows : Room
{
    public override void Create()
    {
        ShortDesc = "Vale Road, the Long Meadows";
        LongDesc = "The road runs on between long meadows, rutted by cart wheels and edged with hawthorn hedges thick with birds. Sheep graze behind low drystone walls, and a farm dog barks from somewhere out of sight. Meadowlarks rise and drop over the grass, and the air smells of hay and wet earth. At a bend in the road stands a waystone with an oak carved on its face, the first sign that a great town lies ahead.";
        Exits["south"] = "/domains/greenvale/vale_road/road";
        Exits["north"] = "/domains/greenvale/vale_road/river_bend";
        ResetSeconds = 120;

        AddDetail("road,ruts,cart ruts", "The road is packed earth and gravel, rutted deep by carts and mended each spring with fresh stone. The ruts all run the same way, north.");
        AddDetail("meadows,meadow,grass,fields", "Long green meadows roll away on either side, patched with clover and buttercups. Somebody's hay is drying in windrows near the far wall.");
        AddDetail("sheep,flock", "A flock of grey-faced sheep tears at the grass behind the nearest wall, indifferent to travelers. Each has a daub of blue dye on its shoulder: a farmer's mark.");
        AddDetail("hedge,hedges,hawthorn", "The hawthorn is laid and woven tight by someone who knew the craft, thick enough to turn a calf. Small birds quarrel in it.");
        AddDetail("wall,walls,drystone,drystone walls", "Low walls of fieldstone, built without mortar and older than anyone who farms behind them. Lichen has sewn them to the ground.");
        AddDetail("dog,farm dog", "You catch no sight of the dog, only its voice, patient and bored, from behind a distant barn.");
        AddReadable("waystone,stone,oak,carving", "A great oak is cut into the stone, its roots spreading as wide as its crown. Beneath it the mason has carved:\n\"OAKENFELL. TWO MILES.\"\nThe tree has been recut more than once, and the newest cuts are pale against the old.", "A waist-high waystone carved with an oak stands by the road. You could {cmd}read{/} it.");

        AddEncounter("/domains/hollow/mob/wandering_wolf", 20);
    }
}
