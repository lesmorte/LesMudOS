// @inherit /std/room

public class BgRiverRoad : Room
{
    public override void Create()
    {
        ShortDesc = "The River Road";
        LongDesc = "The River Road is a towpath of packed earth that follows the bank of the Mere out of Rivergate, with the slow brown water on one side and a screen of willows and reeds on the other. Hoof prints and the marks of dragged ropes show where barge horses have walked it for generations. Ahead, the river winds off between low green banks into a haze of distance, and the sound of the city fades behind. The water flows downriver, and so does the road.";
        Exits["south"] = "/domains/greenvale/oakenfell/river/rivergate";
        BlockExit("north", "The River Road runs on downriver along the Mere, but it is not yet open to travelers.");

        AddDetail("road,river road,towpath,path,hoof prints,ropes,marks", "A narrow towpath beaten hard by hooves, grooved at its edge where tow-ropes have sawn the bank. It follows every bend of the river.");
        AddDetail("river,mere,river mere,water,current,downriver", "The River Mere is wide and brown and slow here, curving away between green banks. A barge, tiny with distance, slides on it.");
        AddDetail("willows,willow,reeds,bank,banks,screen", "Willows trail their pale leaves in the water, and thick reeds crowd the margins, rustling. Something plops into the water and is gone.");
        AddDetail("haze,distance,horizon,downriver,north", "Far downriver the water and the sky blur together in a pale haze. You cannot see where the river goes.");
        AddReadable("milestone,stone,marker,mile stone", "A mossy stone beside the towpath reads:\n\"OAKENFELL, 1.\nDOWNRIVER.\"", "A mossy stone stands by the towpath. You could {cmd}read{/} it.");
    }
}
