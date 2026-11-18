// @inherit /std/room

/// <summary>
/// The Tannery: home of the Tanning Rack (a described detail for now; the crafting station comes
/// later) and of Dunstan Skell. Set apart from the square for the smell.
/// </summary>
public class VhTannery : Room
{
    public override void Create()
    {
        ShortDesc = "The Tannery";
        LongDesc = "This open-sided shed stands well off the square and downwind of everything, and for good reason: the smell of lime, bark and old hide hangs about it like a fog. Wooden vats sunk in the earth floor steep in dark liquor, and stretched hides hang from the rafters like drying laundry. A tall tanning rack of lashed poles stands in the best of the light, with a half-scraped skin pegged across it. A price slate leans against the nearest post.";
        Exits["out"] = "/domains/hollow/village/square";
        Safe = true;

        AddDetail("rack,tanning rack,tanning frame,frame,poles", "A tall rack of lashed ash poles, taller than a man, built to hold a hide taut while it is scraped and treated. The poles are dark and slick from years of handling. Dunstan says leather and hide treatment are done here, and nowhere else in the Hollow.");
        AddDetail("vats,vat,liquor,pits", "Round wooden vats sunk to the rim in the floor, each filled with a different murky brew. Dunstan has never explained which is which, and the smell of the nearest suggests you should not ask for a taste.");
        AddDetail("scrapers,scraper,knives,fleshing knife,tools", "Curved two-handled scrapers and fleshing knives hang from pegs by size, their edges bright against the dark wood. They are the only clean things in the shed.");
        AddDetail("hides,hide,skins,skin,stretched hides,rafters", "Rabbit, wolf and deer skins hang from the rafters in every state from raw to supple, the stiff ones rattling gently in the draught. A few are tagged with scraps of bark.");
        AddDetail("smell,stink,air", "It is very strong. Within a minute you stop noticing it, which is somehow worse.");
        AddReadable("slate,price slate,prices,price", "The slate reads:\n  \"HIDES TAKEN IN. Rabbit, wolf, deer.\n  Bring them fresh.\n  Do not bring them in the tavern first.\n  Wash before you go back to the square.\"", "A price slate leans against a post. You could {cmd}read{/} it.");

        AddSpawn("/domains/hollow/npc/dunstan");
    }
}
