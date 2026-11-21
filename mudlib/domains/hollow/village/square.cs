// @inherit /std/room

public class VillageSquare : Room
{
    public override void Create()
    {
        ShortDesc = "Village Square";
        LongDesc = "The center of Vincent's Hollow bustles with life. A large stone well marks the center of the square while merchants display goods from temporary stalls. Locals gather around benches exchanging news while children dart between travelers. Off the square stand the healer's cottage and the craftsman's workshop, and a lane leads downwind to the tannery. A notice post stands beside the well, hung with a fresh placard. A narrow lane runs off the corner of the square toward the sound of hammers and the thud of arrows: Fletcher's Lane, with the bowyer, the shrine and the scholar's loft.";
        Exits["north"] = "/domains/hollow/village/gate";
        Exits["west"] = "/domains/hollow/village/guild";
        Exits["east"] = "/domains/hollow/village/ironoak";
        Exits["south"] = "/domains/hollow/village/tavern";
        Exits["healer"] = "/domains/hollow/village/healer";
        Exits["workshop"] = "/domains/hollow/village/workshop";
        Exits["tannery"] = "/domains/hollow/village/tannery";
        Exits["lane"] = "/domains/hollow/village/fletchers_lane";
        Safe = true;

        AddDetail("well,stone well", "A broad well of fitted grey stone, its rim worn smooth by seventy years of hands and buckets. The water far below smells cold and clean. A small plaque is set into the stonework.");
        AddDetail("bench,benches", "Rough plank benches, polished by generations of travelers and gossips. A few locals are leaning together over some news or other.");
        AddDetail("stalls,stall,merchants", "Temporary stalls of canvas and trestle tables, piled with tools, cloth and trinkets. Most of it is not for sale to the likes of you, but a traveling merchant seems willing.");
        AddDetail("children", "Children dart between the stalls, shrieking with laughter. Somehow none of them ever collides with a cart.");
        AddReadable("plaque,small plaque,bronze plaque", "The plaque reads:\n\"This well was dug by Vincent Holloway and his companions during the settlement's first winter.\"", "A small bronze plaque is set into the rim of the well. You could {cmd}read{/} it.");

        AddReadable("notice,notices,notice post,post,placard,sign,craft notice,crafting notice",
            "THE CRAFTSFOLK OF THE HOLLOW GIVE NOTICE\n" +
            "Whatever grows or dies near the Hollow may be gathered and made into something useful. Carry a sickle, a skinning knife or a bleeding kit and try {cmd}harvest{/}, {cmd}skin{/} and {cmd}bleed{/}.\n" +
            "  Elspeth the healer, off the square: bandages, salves, tonics and poultices.\n" +
            "  Dunstan the tanner, down the lane: treated leather. Tobin the craftsman: bandages and leather wraps.\n" +
            "  Harland at the Hollowman's Rest: berry juice, made at his hearth.\n" +
            "Each teaches for a few coins ({cmd}lessons{/}, then {cmd}learn{/} NAME) and sells what the work needs ({cmd}browse{/}).\n" +
            "Then {cmd}craft{/} NAME at the right table, rack, bench or fire, and {cmd}recipes{/} lists what you know. Curious? Try {cmd}combine{/} THING {cmd}and{/} THING. {cmd}help crafting{/} says more.\n" +
            "NEW: Fletcher's Lane ({cmd}lane{/}), bows, the Hedge-Scholar's staves and robes, a blessing at the shrine; armour at the shed by the Iron Oak; the Practice Yard past the lane. Better steel at Torren's.",
            "A weathered post by the well carries a freshly painted placard. You could {cmd}read{/} it.");

        AddSpawn("/domains/hollow/npc/benson");
        AddSpawn("/domains/hollow/npc/traveling_merchant");
    }
}
