// @inherit /std/room

public class RvRivergate : Room
{
    public override void Create()
    {
        ShortDesc = "Rivergate";
        LongDesc = "Rivergate is a water gate: a steep stone arch in the city wall through which the River Mere flows out on its way downriver, with a raised iron portcullis above and a narrow landing alongside. A path runs on beside the water, under the arch, toward open country and a hazy blue distance. The stone is slick with spray and green with moss, and the sound of the current under the arch is a low constant rumble. A watchwoman stands on the landing with a boathook, looking downstream.";
        Exits["south"] = "/domains/greenvale/oakenfell/river/quay_north";
        Exits["north"] = "/domains/greenvale/oakenfell/beyond/river_road";

        AddDetail("arch,water gate,water-gate,gate,stone,wall,portcullis", "A tall pointed arch of old stone, green at the waterline, with an iron portcullis hauled up into the dark above. The Mere passes beneath it in a smooth brown tongue, and the wall on either side is dotted with weep-holes and moss.");
        AddDetail("river,mere,river mere,water,current,downriver", "The River Mere narrows to pass the arch and speeds up, running quick and quiet past the stone. Beyond, it widens again and winds away between low green banks.");
        AddDetail("landing,path,towpath,beside the water", "A narrow stone landing with an iron rail on the river side, and beyond it a beaten towpath running on along the bank, rutted by the hooves of barge-horses.");
        AddDetail("boathook,hook,pole", "A long ash pole with an iron hook, kept by the watch for fishing out drifting timber, drowned dogs and the occasional drunk.");
        AddReadable("notice,gate notice,sign,board", "A weathered board by the landing reads:\n\"RIVERGATE. Barges clear the arch by day only. The towpath beyond is the River Road; the watch cannot answer for it. Keep out of the water.\"", "A weathered board is bolted to the wall by the landing. You could {cmd}read{/} it.");

        AddSpawn("/domains/greenvale/oakenfell/river/npc/watch_rivergate");
    }
}
