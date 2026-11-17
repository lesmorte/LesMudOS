// @inherit /std/room

/// <summary>The end of the Forgotten Cellars: the Goblin Overseer's room.</summary>
public class OverseerChamber : Room
{
    public override void Create()
    {
        ShortDesc = "Overseer Chamber";
        LongDesc = "Unlike the rest of the dungeon, this room is surprisingly organized. Maps, notes, and excavation sketches cover a large wooden table. A strange symbol resembling a crow is repeatedly scrawled throughout the chamber.";
        Exits["west"] = "/domains/hollow/cellars/overseer_hall";

        AddDetail("table,wooden table,large table", "A large wooden table, buried under maps, notes and excavation sketches. Most are in crude goblin script, but the sketches show tunnels, shafts and one great round chamber, drawn again and again.");
        AddDetail("maps,map,notes,sketches,excavation sketches", "Hand-drawn maps of the tunnels beneath the village. Many arrows lead down, toward a place marked with a circle and a crow.");
        AddDetail("symbol,crow,crow symbol,scrawl", "A crude black crow with spread wings, scratched into the walls, the table and the floor. Whoever drew it did so over and over, as if for luck, or as if for fear.");

        AddSpawn("/domains/hollow/mob/goblin_overseer");
    }
}
