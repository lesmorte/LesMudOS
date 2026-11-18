// @inherit /std/room

public class SrW3 : Room
{
    public override void Create()
    {
        ShortDesc = "Windmill Hill";
        LongDesc = "The hill's flat crown is kept short by sheep and wind, and on it stands the windmill, a squat tower of tarred timber on a stone base, its sails turning above your head with a rumble you feel through your boots. From here the whole of the south road lies spread out like a ribbon: the Hollow's smoke far south, the farms patched like a quilt, the inn's yard small and bright, and to the north the dark band of the Eldergreen Wood. A wind-flattened path drops down the western slope toward a dim tangle of thorn. Flour dust lies on every surface, white as frost.";
        Exits["south"] = "/domains/greenvale/corridor/south_road/w2";
        Exits["north"] = "/domains/greenvale/corridor/south_road/w4";
        Exits["west"] = "/domains/greenvale/corridor/south_road/d1";
        ResetSeconds = 120;

        AddDetail("windmill,mill,tower,timber,sails", "A post-mill of tarred oak, turned into the wind by a great tail pole. Every board and beam trembles with the work.");
        AddDetail("view,road,ribbon,farms,hollow,smoke", "Standing here the whole country makes sense: road, farms, brook, bridge. Far south, a fleck of smoke is Vincent's Hollow.");
        AddDetail("forest,wood,eldergreen,dark band,north", "A long wall of trees, solid and dark, across the whole northern sky. Nothing in the farmland has prepared you for the size of it.");
        AddDetail("flour,dust,flour dust", "Fine white flour on everything, floating in the sunbeams. It tickles the nose and writes on the sleeves.");
        AddDetail("slope,tangle,thorn,west,western slope", "The western slope falls away into rough ground, thorn and bramble and the dark mouths of burrows. Sheep do not go there, and the shepherds do not like to talk about why.");
        AddReadable("tally,tally board,board,mill board", "A board by the mill door carries chalked tallies and a few lines of a miller's hand:\n\"The 12th: 14 sacks Pollard's barley. The 13th: wind from the north-west, good milling. The 14th: wolf took a lamb from Bram's flock at dusk. The 15th: mended the tail pole.\"", "A chalked tally board hangs on the mill wall. You could {cmd}read{/} it.");

        AddSpawn("/domains/greenvale/corridor/south_road/npc/millhand_hamo");
    }
}
