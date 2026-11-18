// @inherit /std/room

/// <summary>The Hall of Legends, the Great Doors: the timber and stone front.</summary>
public class GqHallDoors : Room
{
    public override void Create()
    {
        ShortDesc = "The Hall of Legends, the Great Doors";
        LongDesc = "The front of the Hall of Legends rises over the end of Guild Row like a cliff that has been taught carpentry. It is plainly many buildings joined into one: a wall of pale river stone gives way to dark timber framing, then to a stretch of red brick, then to carved oak, yet a single long roofline runs over all of it as if the whole had always been meant. Broad steps climb to a pair of great doors standing open on a hall of lamps and quiet voices. Folk of every sort come and go beneath the carved lintel, and the doorwarden nods at each of them.";
        Exits["south"] = "/domains/greenvale/oakenfell/guild/guild_row";
        Exits["north"] = "/domains/greenvale/oakenfell/guild/hall_of_callings";

        AddDetail("front,facade,hall,building,buildings,wall,walls", "Look long enough and the seams show: pale river stone at the western end where the oldest part of the Hall must have stood, black timber and plaster beside it from a later age, brick from a century of prosperity, and the newest wing in honey-coloured oak. No two sections share a window, and every one of them has been fitted to the next so closely that it reads as a single, ever-growing thing.");
        AddDetail("stone,river stone,timber,timbers,brick,carpentry", "The stone is grey and rounded, carried up from the Mere by hand. The timbers are older still in places, black as iron, with joints cut in styles no carpenter now living would use. Each generation of builders matched the work of the last and signed their own.");
        AddDetail("roof,roofline,gables,eaves", "A single long roofline of dark slate runs over everything, stepping up and down with the buildings beneath it. From the street it looks like one roof. Only when the light is low can you see that it is a dozen.");
        AddDetail("steps,stairs", "Wide stone steps, each one hollowed in the middle. Some say the founders' own boots made the first dips.");
        AddDetail("doors,great doors,door", "Two doors of black oak, each three times the height of a man, bound with iron and standing permanently open. The wood bears a thousand small scratches where hands and sheathes and pack-corners have brushed it.");
        AddDetail("lamps,lanterns", "Iron lanterns hang in a row inside the entrance, lit all day, so that the passage beyond glows like the inside of a lantern itself.");
        AddReadable("motto,lintel,carving,inscription,letters",
            "Carved deep into the lintel above the doors, in letters a hand wide and gilded long ago:\n\n    GREATNESS IS CHOSEN\n\nBeneath, in smaller letters worn almost smooth: \"Built by the hands of five callings, for all who would answer them.\"",
            "A line of deep-cut, gilded letters runs along the lintel above the doors. You could {cmd}read{/} it.");

        AddSpawn("/domains/greenvale/oakenfell/guild/npc/doorwarden");
    }
}
