// @inherit /std/room

/// <summary>The Hall of Legends, the Envoys' Gallery: faction contacts.</summary>
public class GqEnvoysGallery : Room
{
    public override void Create()
    {
        ShortDesc = "The Hall of Legends, the Envoys' Gallery";
        LongDesc = "A broad, low-ceilinged gallery of dark panelling and green leather, lined with desks and standing banners where the great powers of Greenvale keep their representatives. Each desk has its own small flag, its own stack of letters, and its own habit of looking pleasant while listening closely. A long window on the north wall looks across the rooftops toward the river, and a rack of pigeonholes by the door overflows with sealed correspondence. The place hums with quiet, civil business.";
        Exits["east"] = "/domains/greenvale/oakenfell/guild/hall_of_callings";

        AddDetail("desks,desk", "Four desks stand in a row, each with a brass nameplate and a stack of correspondence: an envoy of the Greenvale Assembly, an envoy of the Merchant Consortium, an envoy of the Riverwatch Rangers, and a clerk of the Hall of Legends itself.");
        AddDetail("banners,banner,flags,flag", "Small banners stand behind each desk. The Assembly's shows a gathering of hands. The Consortium's shows a pair of scales. The Riverwatch's shows a watchful eye. The Hall's shows five bars joined into one. None is larger than the others, which seems to have been negotiated.");
        AddDetail("window,rooftops,river", "Through the window you can see the slate roofs of the city, tumbling toward the silver thread of the River Mere.");
        AddDetail("pigeonholes,letters,correspondence,rack", "Dozens of sealed letters, filed by crest. A clerk collects them hourly. No one has ever been seen to read one.");
        AddDetail("panelling,panels,leather,chairs", "Dark oak panelling and deep green leather chairs. The chairs are for visitors and are surprisingly comfortable, which the envoys say is deliberate.");
        AddReadable("nameplates,plates,brass plates,nameplate",
            "The brass nameplates read:\n  ENVOY OF THE GREENVALE ASSEMBLY\n  ENVOY OF THE MERCHANT CONSORTIUM\n  ENVOY OF THE RIVERWATCH RANGERS\n  CLERK OF THE HALL OF LEGENDS\nBelow, on a small card: \"Ask freely. Ask about anyone.\"",
            "Each desk bears a brass nameplate. You could {cmd}read{/} the nameplates.");

        AddSpawn("/domains/greenvale/oakenfell/guild/npc/envoy_assembly");
        AddSpawn("/domains/greenvale/oakenfell/guild/npc/envoy_consortium");
        AddSpawn("/domains/greenvale/oakenfell/guild/npc/envoy_riverwatch");
        AddSpawn("/domains/greenvale/oakenfell/guild/npc/hall_clerk");
    }
}
