// @inherit /std/room

/// <summary>The Hall of Legends, the Chronicle Gallery: the lectern, the three carved walls and the Renown board.</summary>
public class GqChronicleGallery : Room
{
    public override void Create()
    {
        ShortDesc = "The Hall of Legends, the Chronicle Gallery";
        LongDesc = "A long, high gallery of pale stone and polished oak, lit from above by a row of clerestory windows that throw bars of light across the floor. At its heart stands a great lectern bearing the Grand Chronicle, and along the walls three tall panels of carved stone wait for the names that will one day fill them. Beside the entrance hangs a board of Renown, and a chronicler's desk is tucked under the stair. It feels like a library, a museum and a vault at once, and the silence has the particular patience of a place that expects to be filled.";
        Exits["west"] = "/domains/greenvale/oakenfell/guild/hall_of_callings";

        AddDetail("gallery,windows,clerestory,light", "The windows are set high, so that the light falls onto the walls and not the lectern, and the reader is never dazzled.");
        AddDetail("desk,chroniclers desk,chronicler's desk,stair", "A small desk piled with pens, sand and ledgers, tucked under a narrow stair that leads to the upper stacks. The ink is always fresh.");
        AddReadable("six words,plaque,dedication,bronze plaque,brass plaque",
            "THE SIX WORDS\n" +
            "Set beside the lectern by the first keepers of the Chronicle:\n" +
            "\"Under six words the deeds of Hollowmere are written: Might, Grace, Vigor, Insight, Spirit and Resolve. Write what was done, and which word it took. A name is remembered for one of them, seldom two.\"",
            "A bronze plaque is set into the floor beside the lectern. You could {cmd}read{/} it.");
        AddReadable("chronicle,grand chronicle,lectern,book,great book",
            "THE GRAND CHRONICLE\n" +
            "A great book of heavy vellum, bound in oak and iron and set on a lectern worn by the hands of generations of keepers. Its first leaves are inscribed with a single page:\n\n" +
            "\"The Chronicle is the record of Hollowmere. It keeps what was done, who did it, and why it mattered. Levels are power. Renown is fame. The Chronicle is legacy.\"\n\n" +
            "The remaining leaves are blank. The Chronicle is not yet open for reading in this gallery; the chronicler will tell you when it is.",
            "A great book lies open on a lectern, its pages blank beneath a single inscribed leaf. You could {cmd}read{/} it.");
        AddDetail("hall of heroes,heroes,hero wall,heroes wall,first wall,left wall",
            "A tall panel of pale stone carved with a border of laurel and sword. This is the Hall of Heroes. A place is earned here by those who have achieved Legendary Renown, won major story victories, led a faction, or made exceptional contributions, and a name once carved here stays carved, even if its owner never plays again. The stone is still almost entirely bare.");
        AddDetail("hall of legends wall,legends,legend wall,legends wall,centre wall,center wall,middle wall",
            "A panel darker than its neighbours, carved with a ring of stars and left almost wholly smooth. This is the Hall of Legends, kept separate from the Hall of Heroes and reserved for the truly extraordinary: the first Mythic victory, world-changing decisions, realm-saving deeds, the founders of great guilds, the discovery of whole continents. Only a tiny number of names should ever appear here. The stone is still almost entirely bare.");
        AddDetail("hall of infamy,infamy,infamy wall,right wall,villains",
            "A panel of black basalt, carved with a border of broken chain and thorn. This is the Hall of Infamy, for not all history is heroic: most wanted criminals, infamous betrayals, legendary bandits, dark rituals, political assassinations and world-altering villainy. History remembers villains too. The stone is still almost entirely bare.");
        AddDetail("walls,carved walls,panels,carved panels,stone", "Three tall panels of carved stone stand along the walls: the Hall of Heroes, the Hall of Legends and the Hall of Infamy. Look at each in turn. They are all still mostly bare, with room for a long future.");
        AddReadable("renown,board,board of renown,renown board,tiers",
            "THE BOARD OF RENOWN\n" +
            "How widely a character is known, from the first tale to living folklore:\n" +
            "  Unknown: no one has heard of you.\n" +
            "  Local: known in your hometown and the communities around it.\n" +
            "  Recognized: known throughout Greenvale; tavern gossip may mention you.\n" +
            "  Acclaimed: widely known in the region; merchants know the name, and guilds begin to offer special opportunities.\n" +
            "  Renowned: known across multiple regions; travelers spread the stories.\n" +
            "  Famous: most citizens have heard the name.\n" +
            "  Heroic: songs, books and stories refer to the character.\n" +
            "  Legendary: the character has become part of modern history.\n" +
            "  Mythic: living folklore; very few ever reach this tier.\n" +
            "The Hall does not yet keep Renown records here.",
            "A slate board with nine tiers chalked in a careful hand. You could {cmd}read{/} it.");

        AddSpawn("/domains/greenvale/oakenfell/guild/npc/chronicler");
    }
}
