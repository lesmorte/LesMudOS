// @inherit /std/room

/// <summary>Crafters' Court: the crafting guilds' shared yard.</summary>
public class GqCraftersCourt : Room
{
    public override void Create()
    {
        ShortDesc = "Crafters' Court";
        LongDesc = "Crafters' Court is a wide, cobbled yard ringed by the workshops of the crafting guilds, where the din of hammers on iron mixes with the rattle of looms and the hiss of quenching water. Forges glow in open-fronted smithies on the north side, while racks of dyed cloth in every colour hang drying along the west wall. A tall guild house stands at the far end with its doors open and a notice board beside them. Under the guild awning stand a long workbench and, apart from the cloth, a tanner's rack, both free to any crafter who asks. The air smells of coal-smoke, hot metal and wet wool, and nobody here is idle.";
        Exits["east"] = "/domains/greenvale/oakenfell/guild/guild_row";
        Exits["south"] = "/domains/greenvale/oakenfell/guild/proving_yard";

        AddDetail("forges,forge,smithies,smithy,anvils,anvil", "Three open-fronted smithies share the north side, each with its forge roaring and its anvil ringing. Smiths in leather aprons work the metal in a rhythm that carries across the court like slow music.");
        AddDetail("looms,loom,weavers,weaving", "Beside the guild house, weavers work at tall looms set up under a long awning, the shuttles flicking back and forth faster than the eye can follow.");
        AddDetail("cloth,dye,dyed cloth,racks,drying", "Racks of cloth hang along the west wall: crimson, indigo, saffron and a green so deep it is almost black. The colours bleed faintly into the cobbles beneath.");
        AddDetail("guild house,guildhouse,house,doors", "A tall, narrow house of grey stone with doors thrown open. A brass plaque on the lintel reads \"The Joined Guilds of Craft\". Inside, clerks move between desks piled with ledgers.");
        AddDetail("yard,court,cobbles,water,trough", "A wide stone trough of water sits near the smithies for quenching, ringed in soot. The cobbles are blackened with a century of sparks.");
        AddReadable("notice board,board,notice,notices",
            "NOTICE OF THE JOINED GUILDS OF CRAFT\n" +
            "The guilds of Smiths, Weavers and the allied crafts hold their yard in common. Enrollment of new members is not open at this time. Apprentices already bound to a master continue their work as before. Visitors are welcome to watch, and are asked to keep clear of the forges. Further notice will be posted on this board.",
            "A board covered in neat notices. You could {cmd}read{/} it.");

        AddSpawn("/domains/greenvale/oakenfell/guild/obj/court_workbench");
        AddSpawn("/domains/greenvale/oakenfell/guild/obj/court_tanning_rack");
        AddSpawn("/domains/greenvale/oakenfell/guild/npc/smith");
        AddSpawn("/domains/greenvale/oakenfell/guild/npc/weaver");
        AddSpawn("/domains/greenvale/oakenfell/guild/npc/guild_clerk");
    }
}
