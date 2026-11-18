// @inherit /std/room

/// <summary>The Hall of Legends, the Hall of Callings: five hallmasters and the wall of fifteen banners.</summary>
public class GqHallOfCallings : Room
{
    public override void Create()
    {
        ShortDesc = "The Hall of Legends, the Hall of Callings";
        LongDesc = "This is the great chamber the founders of the five callings raised, and every generation since has added to it: a floor of worn flagstones inlaid with a ring of five colours, a roof of black beams as thick as ships' keels, and a wall of tall banners along the east side. Five stations stand around the ring, one for each calling, each marked by its own device and kept by a hallmaster who has plainly seen a great many nervous first-timers. The air is warm and quiet, and the talk is low, as in a library that expects to be argued with. Passages lead east to the Chronicle Gallery and west to the Envoys' Gallery, and the Great Doors lie to the south.";
        Exits["south"] = "/domains/greenvale/oakenfell/guild/hall_doors";
        Exits["east"] = "/domains/greenvale/oakenfell/guild/chronicle_gallery";
        Exits["west"] = "/domains/greenvale/oakenfell/guild/envoys_gallery";

        AddDetail("floor,flagstones,ring,inlay", "The flagstones are worn smooth in five long paths that run from the south doors to the five stations. In the middle, a ring of inlaid stone in five colours circles a plain central stone: red for the Warriors, green for the Scouts, white for the Priests, blue for the Mages, and a stripe of every colour at once for the Adventurers.");
        AddDetail("roof,beams,ceiling,timbers", "Black beams span the chamber, each one cut from a single tree. Several carry the carved initials of the wrights who raised them, and there is a gap between two beams where the builders of a later century joined their work to the builders of an earlier one, and had the sense to leave the joint in view.");
        AddDetail("stations,station,devices,device,alcoves", "Five stations stand around the ring: a shield and sword for the Warriors, a bow and a cloak-pin for the Scouts, a lamp and open hand for the Priests, a star over a book for the Mages, and a lantern with a compass for the Adventurers. A hallmaster stands at each.");
        AddDetail("hallmasters,hallmaster,masters", "Five people stand at five stations, each easy to find. Try {cmd}talk{/} to any of them, or {cmd}ask{/} one {cmd}about{/} a class.");
        AddDetail("lamps,lanterns,light", "Lamps of amber glass hang from the beams on long chains. They are lit from morning until the last petitioner leaves.");
        AddReadable("banners,banner,wall,wall of banners,fifteen banners,advanced classes",
            "THE FIFTEEN BANNERS\n" +
            "At the fifth level, a member of each calling may choose among the three paths below. The choice is permanent. All that the calling has given is kept.\n\n" +
            "WARRIOR\n" +
            "  Knight: Heavy armor defender.\n" +
            "  Berserker: Aggressive melee damage dealer.\n" +
            "  Monk: Disciplined martial artist.\n\n" +
            "SCOUT\n" +
            "  Rogue: Stealth and assassination.\n" +
            "  Ranger: Ranged combat and survival.\n" +
            "  Bard: Support, inspiration, and utility.\n\n" +
            "PRIEST\n" +
            "  Cleric: Healing and protection.\n" +
            "  Templar: Holy warrior.\n" +
            "  Oracle: Divine support and foresight.\n\n" +
            "MAGE\n" +
            "  Mage: Pure arcane spellcaster.\n" +
            "  Druid: Nature magic and shapeshifting.\n" +
            "  Necromancer: Undeath, curses, and summons.\n\n" +
            "ADVENTURER\n" +
            "  Spellblade: Hybrid martial and magical combatant.\n" +
            "  Beastmaster: Explorer and companion handler.\n" +
            "  Artificer: Inventor, gadgeteer, and crafter.\n\n" +
            "Ask the hallmaster of your calling about any path.",
            "Fifteen tall banners hang in five groups of three along the east wall, each stitched with the device of a class. You could {cmd}read{/} the banners.");

        AddSpawn("/domains/greenvale/oakenfell/guild/npc/hallmaster_warrior");
        AddSpawn("/domains/greenvale/oakenfell/guild/npc/hallmaster_scout");
        AddSpawn("/domains/greenvale/oakenfell/guild/npc/hallmaster_priest");
        AddSpawn("/domains/greenvale/oakenfell/guild/npc/hallmaster_mage");
        AddSpawn("/domains/greenvale/oakenfell/guild/npc/hallmaster_adventurer");
    }
}
