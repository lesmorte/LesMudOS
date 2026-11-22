// @inherit /std/room

public class SgSilverStag : Room
{
    public override void Create()
    {
        ShortDesc = "The Silver Stag Inn";
        LongDesc = "The Silver Stag's common room is long and low, its black beams hung with antlers, tankards and travelers' tokens left by those who came back. A great stone fireplace fills the north wall, and a staircase climbs beside it to the guest rooms. The air is thick with woodsmoke, roasting meat and talk. Adventurers, drovers, clerks and sellswords share the long tables, and a few old regulars hold the corner seats by the fire. A cork board by the stairs is crowded with notices, and many an adventure in Greenvale began at one of these tables.";
        Exits["west"] = "/domains/greenvale/oakenfell/southgate/stag_lane";
        Exits["up"] = "/domains/greenvale/oakenfell/southgate/silver_stag_rooms";
        ResetSeconds = 120;

        AddDetail("beams,antlers,tankards,tokens,ceiling", "Hundreds of small things hang from the beams: antlers, dented tankards, a child's shoe, ribbons, a goblin's tooth on a string. Each, the regulars say, was left by someone who walked out of here to a great deed or a long road, and some who walked back.");
        AddDetail("stag,stag's head,mantel", "The stag's head hangs above the hearth. It is old and moth-bitten, with antlers painted silver. The regulars say the inn's first owner took it in a single arrow, and they argue about the arrow.");
        AddDetail("tables,benches,corner,seats", "Long tables of black oak scarred with knife-cuts, initials and the rings of a thousand tankards. The corner seats near the fire are always taken by the same three people.");
        AddDetail("stairs,staircase,steps", "A broad staircase of dark wood climbs beside the chimney to the guest rooms. A small painted sign on the post reads \"Guests Only\".");
        AddDetail("travelers,adventurers,sellswords,drovers", "A dozen travelers eat and drink in small groups, and the talk is half boasts and half warnings. Anyone willing to listen might leave with a rumor.");
        AddReadable("toasts,toast wall,wall,wall of names,regulars,names",
            "Names are chalked along the wall by the fire, with a word beside each, and the regulars add to it on feast nights:\n" +
            "  \"DAGNA RUSK, for her Resolve at the gate.\"\n" +
            "  \"THE BLACK-HEADED DROVER, for his Vigor. Fell in the river and was in the common room before the water dried.\"\n" +
            "  \"BRANDT KETCH, for his Might, and for finishing the pie.\"",
            "Names are chalked along the wall by the fire. You could {cmd}read{/} them.");
        AddReadable("board,cork board,notices,notice", "The cork board is crowded with notices:\n \"WANTED: guards for a wagon to Watchfort Crossing, good pay, no questions about the Elderwood.\"\n \"LOST: a brindled hound, one white ear, answers to Biscuit. Two gold. Ask for Marta at the Fish Market.\"\n \"FOUND: a signet ring, engraved with a stag. Ask at the bar.\"\n \"The Free Lodge is hiring steady blades. Coin Before Crown.\"", "A cork board by the stairs is crowded with notices. You could {cmd}read{/} it.");

        AddSpawn("/domains/greenvale/oakenfell/southgate/obj/stag_cooking_fire");
        AddSpawn("/domains/greenvale/oakenfell/southgate/npc/innkeeper_orla");
        AddSpawn("/domains/greenvale/oakenfell/southgate/npc/regular_tam");
        AddSpawn("/domains/greenvale/oakenfell/southgate/npc/regular_dessa");
        AddSpawn("/domains/greenvale/oakenfell/southgate/npc/regular_fenn");
    }
}
