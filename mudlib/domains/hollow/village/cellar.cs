// @inherit /std/room

public class TavernCellar : Room
{
    public override void Create()
    {
        ShortDesc = "Tavern Cellar";
        LongDesc = "Dusty barrels and crates fill the cellar beneath the tavern. Several have been torn apart and strange footprints cover the dirt floor. A rough opening has been smashed through the far wall. One flat stone by the barrels sits a little crooked in the dirt.";
        Exits["up"] = "/domains/hollow/village/tavern";
        Exits["east"] = "/domains/hollow/cellars/collapsed_passage";
        Safe = false;

        AddDetail("barrels,barrel,casks,cask", "Oak barrels stacked three high against the wall. Most are empty, and the few that are not have been gnawed at the bunghole.");
        AddDetail("crates,crate,broken crates,broken crate", "Wooden crates lie splintered across the floor, their lids pried off and their straw scattered. Something with sharp teeth did the prying.");
        AddDetail("sacks,sack,grain sacks,grain sack,chewed grain sacks", "Burlap grain sacks slump against the crates, their seams chewed open and the grain inside trodden into the dirt.");
        AddDetail("footprints,footprint,floor,dirt floor,prints", "Small, clawed footprints cover the dirt floor, crossing and recrossing one another. Some of them are larger than a rat's, and far too many for a single rat.");
        AddDetail("opening,hole,wall,far wall,rough opening", "A ragged hole has been smashed through the far wall, its edges of crumbled stone still pale with fresh dust. Cold air breathes out of it from the dark passage beyond.");

        AddDetail("flagstone,flat stone,stone", "A flat stone about the size of a bread board, set into the dirt by the barrels. It sits a finger's width higher than the floor around it, and the dirt at its edges is scuffed, as if something small has been at it. You could {cmd}search{/} it.");

        AddSpawn("/domains/hollow/mob/large_rat", 3);
        AddSpawn("/domains/hollow/obj/cellar_purse");
    }
}
