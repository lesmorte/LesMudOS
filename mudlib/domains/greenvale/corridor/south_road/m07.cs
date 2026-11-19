// @inherit /std/room

public class SrM07 : Room
{
    public override void Create()
    {
        ShortDesc = "Traveler's Rest";
        LongDesc = "A wayside inn stands back from the road around a square yard of beaten earth, a long low house of stone below and timber above, with a stable at one end and a well at the other. Wagons stand shafts-down against the wall, their drivers dozing in the shade of a great ash tree, and the smell of stew comes warm from the kitchen door. A lantern hangs from an iron arm over the porch, ready for dusk, and a painted sign swings above it. Here the road is not only passed but rested on, and a traveler is welcomed without being asked who he is. A track leads west into the hay meadow.";
        Exits["south"] = "/domains/greenvale/corridor/south_road/m06";
        Exits["north"] = "/domains/greenvale/corridor/south_road/m08";
        Exits["west"] = "/domains/greenvale/corridor/south_road/w5";
        Safe = true;
        LogoutRoom = true;
        ResetSeconds = 120;
        DangerBand(5, 6);

        AddDetail("inn,house,stone,timber,porch", "The inn is older than its sign, built of grey fieldstone with a jettied upper floor of black timber. Every sill is worn smooth where a hundred years of travelers have set down their packs.");
        AddDetail("yard,earth,courtyard", "The yard is beaten flat and swept every morning. Hoof prints and wheel marks fill it in layers, like a ledger of arrivals.");
        AddDetail("well,bucket,rope", "A stone well with a wooden roof and a cold, sweet bucket of water on a rope. A tin cup hangs beside it on a chain, free to all.");
        AddDetail("stable,stables,horses,trough", "A low stable with a half-door, two horses looking out of it, and a trough of fresh water. The straw smells wonderful.");
        AddDetail("ash,ash tree,tree,shade,bench", "A huge ash spreads over half the yard, and a bench is built around its trunk. The bark has been smoothed by ten thousand backs.");
        AddDetail("lantern,arm,wagons,wagon", "The iron lantern arm is hung with a lantern whose glass is clean and bright. The wagons wait, their traces slack, while their drivers eat.");
        AddReadable("sign,painted sign,inn sign", "The sign shows a boot and a bed and a full tankard, and beneath them the words:\n\"THE TRAVELER'S REST. BED, BOARD AND WATER FOR BEAST. NO QUESTIONS ASKED, SOME ANSWERS GIVEN.\"", "A painted sign swings over the porch. You could {cmd}read{/} it.");
        AddReadable("notice,riverwatch notice,board,notice board", "A notice board by the door carries a printed sheet with a silver eye at its head:\n\"RIVERWATCH RANGERS. Travelers bound north of the Stone Bridge are advised to go by daylight and in company. Report wolves, bandits and torn fences to any ranger. Ever Vigilant.\"\nUnderneath, in a different hand: \"Eat first.\"", "A notice board hangs by the inn door. You could {cmd}read{/} it.");
        AddSpawn("/domains/greenvale/corridor/npc/patrol_south_road");

        AddSpawn("/domains/greenvale/corridor/south_road/npc/innkeeper_aldith");
        AddSpawn("/domains/greenvale/corridor/south_road/npc/drover_ives");
        AddSpawn("/domains/greenvale/corridor/south_road/npc/pedlar_jocosa");
        AddSpawn("/domains/greenvale/corridor/south_road/npc/veteran_kenrick");
    }
}
