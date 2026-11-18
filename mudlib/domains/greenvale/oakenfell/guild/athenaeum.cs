// @inherit /std/room

/// <summary>The Athenaeum: library and archives, with four short histories on the shelves.</summary>
public class GqAthenaeum : Room
{
    public override void Create()
    {
        ShortDesc = "The Athenaeum";
        LongDesc = "The Athenaeum is a long, hushed hall of grey stone and dark shelving, where tall windows pour a pale light onto rows of reading desks and the air smells of vellum, wax and old paper. Shelves climb to a gallery that runs around three walls, and ladders on brass rails slide along them with a faint, expensive whisper. A librarian presides over a desk by the door, and farther in, a figure bends over a table of unrolled maps. Everything here seems to have been carefully put away, and carefully brought out again.";
        Exits["north"] = "/domains/greenvale/oakenfell/guild/guild_row";

        AddDetail("shelves,shelf,gallery,books", "Thousands of volumes and rolled scrolls, shelved by subject and age. A reading table near the entrance holds four small works set out for newcomers: a history of the founding, an account of the storm, a note on the city's name and motto, and a plain account of the five districts. You could {cmd}read founding{/}, {cmd}read storm{/}, {cmd}read name{/} or {cmd}read districts{/}.");
        AddDetail("desks,reading desks,tables,table,maps", "Long oak desks with brass lamps and ink-wells chained to them, so that nobody wanders off with one. One table in the back is covered in unrolled maps of the city's older quarters, pinned at the corners with lead weights.");
        AddDetail("windows,light", "Tall windows of old, slightly rippled glass. The light falls in long pale bars across the desks, and every reader sits in one.");
        AddDetail("ladders,rails,brass rails", "Library ladders slide on polished brass rails along the shelves. A small sign on each reads \"Please return the ladder to where you found it.\"");
        AddDetail("archives,archive,records", "A doorway behind the librarian's desk leads down to the archives. A small sign reads: \"By appointment. The archives are not open to casual visitors.\"");
        AddDetail("desk,librarians desk,librarian's desk,sign", "A broad desk with a bell, a ledger and a neat card: \"SILENCE IS REQUESTED. QUESTIONS ARE WELCOME.\"");
        AddReadable("founding,book of the founding,first book,history of the founding",
            "THE FOUNDING OF THE SETTLEMENT\n" +
            "Nearly four centuries ago, a company of settlers came up the river and built a small village on its bank. They chose the place for the water and for a tree. It was an oak so vast that it dwarfed every other tree in Greenvale, and it could be seen for miles across the countryside. Travelers called it simply the Great Oak. It was a meeting place, a marker for those who had lost their way, and a symbol of safety to anyone who had walked a long road. As the village grew, the oak stayed at its center, and no one built a wall or a house that did not look to it first.",
            "A slim, well-handled volume. You could {cmd}read{/} it.");
        AddReadable("storm,book of the storm,burning,the storm,account of the storm",
            "THE STORM AND THE THREE DAYS\n" +
            "Generations after the founding, a terrible storm broke over the village in the night. Lightning struck the Great Oak, and the tree took fire. It burned for three days, and the settlers could do nothing but watch and keep their own roofs wet. When at last the great tree collapsed, many believed the village would be abandoned. Instead the settlers resolved to rebuild around the place where it had stood, rather than lose the symbol that had guided them. They raised new houses on the old ground, and the village began to grow again. It is said that nobody who stood there that morning ever spoke of it lightly.",
            "A thin, singed-looking book. You could {cmd}read{/} it.");
        AddReadable("name,motto,book of the name,note on the name,the name",
            "OF THE NAME AND THE MOTTO\n" +
            "After the fall of the oak, the village had no name grander than the village by the Great Oak. Folk began to speak of it as Oakenfell, the place where the oak fell, and the name outlasted every other suggestion. In time it became the name of a city. The city's motto was chosen to remember the tree and what the settlers made after it: From Strong Roots Grow Great Things. It is cut over the Southgate arch for every traveler to read. Do not confuse it with the motto of the Hall of Legends, Greatness Is Chosen, which is cut over the Great Doors and is the Hall's own. The two have lived side by side for a very long time, and nobody has ever been able to say they disagree.",
            "A pamphlet with a green cover. You could {cmd}read{/} it.");
        AddReadable("districts,five districts,account of the districts,account,plain account",
            "A PLAIN ACCOUNT OF THE FIVE DISTRICTS\n" +
            "Oakenfell is built around five districts. The Founder's District is the original settlement and the oldest part of the city: historic homes, the City Hall and ancient stone streets. The Market District is the economic heart of Greenvale, with its stalls, storefronts, caravan offices, auction house and bank. The River District lies along the River Mere, with its docks, fisheries, warehouses and shipwrights. The Guild Quarter is where you stand now: the Hall of Legends, the crafting guilds, the trainers, the libraries and the archives. The Southgate District is the newest, with inns, stables and lodgings for travelers and adventurers. Each has its own character, and a good citizen knows all five.",
            "A folded paper map and a plain account. You could {cmd}read{/} it.");

        AddSpawn("/domains/greenvale/oakenfell/guild/npc/librarian");
        AddSpawn("/domains/greenvale/oakenfell/guild/npc/scholar");
    }
}
