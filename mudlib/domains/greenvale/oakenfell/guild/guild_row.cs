// @inherit /std/room

/// <summary>Guild Row: the avenue of the guilds, west of Founder's Square.</summary>
public class GqGuildRow : Room
{
    public override void Create()
    {
        ShortDesc = "Guild Row";
        LongDesc = "Guild Row leads west from Founder's Square, a broad avenue of worn cobbles with the banners of the guilds hanging from every upper window. Apprentices in aprons and ink-stained cuffs hurry along it with parcels, scrolls and sacks of charcoal, weaving past the slower feet of the curious. To the north the road climbs gently toward the vast, many-gabled front of the Hall of Legends, while a library door stands open to the south and the ring of hammers comes from the west. Everything here is made to be looked up at, and nothing in it is made to keep you out.";
        Exits["east"] = "/domains/greenvale/oakenfell/founders/square";
        Exits["north"] = "/domains/greenvale/oakenfell/guild/hall_doors";
        Exits["south"] = "/domains/greenvale/oakenfell/guild/athenaeum";
        Exits["west"] = "/domains/greenvale/oakenfell/guild/crafters_court";

        AddDetail("banners,banner,guild banners,windows", "Dozens of banners hang across the Row: a hammer on a field of red for the smiths, a shuttle on blue for the weavers, an open book on grey for the scholars, a quill crossed with a trowel for the clerks and builders. Some are bright with new dye and some have faded to the colour of old bread. The newest are always hung nearest the Hall, and the oldest nearest the Square, where they began.");
        AddDetail("apprentices,apprentice", "A stream of them, none older than twenty, each carrying something too large and each late for something. They step around the newcomers without looking, the way river water goes around a stone.");
        AddDetail("cobbles,cobblestones,road,avenue,street", "The cobbles are polished into shallow dips down the middle of the Row, worn by several centuries of ambitious feet.");
        AddDetail("hall,hall of legends,front,gables", "The front of the Hall of Legends fills the north end of the Row, a great mass of timber and stone with more gables than you can count at once. Take the road north to reach its doors.");
        AddDetail("library,athenaeum,library door", "A tall oak door set in a grey stone front, propped open with a stack of books. A carved owl keeps watch above the lintel.");
        AddDetail("hammers,yard,smithies", "The noise of metalwork and loom-work comes from the west, where the crafting guilds share a yard.");
        AddReadable("signpost,sign,post",
            "A tall signpost with painted arms:\n" +
            "  NORTH: the Hall of Legends ({cmd}north{/})\n" +
            "  SOUTH: the Athenaeum, library and archives ({cmd}south{/})\n" +
            "  WEST: Crafters' Court, and beyond it the Proving Yard ({cmd}west{/})\n" +
            "  EAST: Founder's Square ({cmd}east{/})",
            "A tall signpost stands at the corner. You could {cmd}read{/} it.");

        AddSpawn("/domains/greenvale/oakenfell/guild/npc/journeyman");
        AddSpawn("/domains/greenvale/oakenfell/guild/npc/newcomer");
    }
}
