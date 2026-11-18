// @inherit /std/room

public class RvRiverwatchOffice : Room
{
    public override void Create()
    {
        ShortDesc = "The Riverwatch Office";
        LongDesc = "The Riverwatch Rangers' city office is a plain, tidy room with whitewashed walls, a scarred oak desk and a rack of long green cloaks by the door. An enormous painted map of Greenvale's roads covers the whole of the back wall, studded with colored pins. A banner bearing a pale shield hangs over the desk, a duty roster is nailed up beside it, and the room smells of boot-grease, lamp oil and cold tea. The office is quiet in the way of a place where people come in out of the weather and tell the truth.";
        Exits["south"] = "/domains/greenvale/oakenfell/river/old_river_street";

        AddDetail("map,wall map,roads map,greenvale map", "A great painted map of Greenvale's roads covers the back wall, stretched on a frame of ash. Oakenfell is a walled circle at its center. You could {cmd}read{/} the map.");
        AddReadable("map,wall map,roads map,greenvale map", "The map shows Oakenfell as a walled circle at the center, the River Mere a blue ribbon through it. Roads run out from the gates like the spokes of a wheel:\n\nSOUTH: a road runs down across meadows to a small cluster of roofs marked VINCENT'S HOLLOW.\nEAST: the Crossing Road runs to a fort drawn in red, marked WATCHFORT CROSSING, with a banner flying over it.\nWEST: the Westwood Road runs into a great green wash, marked THE ELDERWOOD.\nNORTH: the Northern Road climbs between fields and forest toward a range of grey peaks, marked THE MOUNTAINS.\n\nRed pins mark the Rangers' posts along the roads. The River Mere runs on downriver, off the edge of the map.", "A great painted map of Greenvale's roads covers the back wall. You could {cmd}read{/} the map.");
        AddDetail("banner,shield,pale shield,flag", "A pale shield on a field of green hangs over the desk. Beneath it, stitched in white, are the words of the Rangers' motto. You could {cmd}read{/} it.");
        AddReadable("banner,motto,shield,pale shield,flag,words", "Stitched in white beneath the pale shield:\n\"EVER VIGILANT\"", "A green banner with a pale shield. You could {cmd}read{/} it.");
        AddDetail("desk,oak desk,papers,ledger,tea", "A scarred oak desk, its top stained with a hundred rings of tea. A stack of patrol reports is held down by a worn river stone.");
        AddDetail("cloaks,cloak,rack,green cloaks,boots", "A rack of long green cloaks, mud-stained at the hems and mended in a dozen places, hangs by the door. Beneath it, a row of boots stand dripping.");
        AddReadable("roster,duty roster,patrols,list,notice", "The duty roster nailed up beside the banner lists patrols by road: Crossing Road, Vale Road, Westwood (as far as the second milestone), the Northern Road, the river. Every column has a name in it. A line at the foot has been underlined twice:\n\"All hands to report anything moving on the roads that should not be.\"", "A duty roster is nailed up beside the banner. You could {cmd}read{/} it.");

        AddSpawn("/domains/greenvale/oakenfell/river/npc/sergeant_maren");
    }
}
