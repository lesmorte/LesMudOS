// @inherit /std/room

public class TavernCellar : Room
{
    public override void Create()
    {
        ShortDesc = "Tavern Cellar";
        LongDesc = "Dusty barrels and crates fill the cellar beneath the tavern. Several have been torn apart and strange footprints cover the dirt floor. A rough opening has been smashed through the far wall.";
        Exits["up"] = "/domains/hollow/village/tavern";
        Exits["east"] = "/domains/hollow/cellars/collapsed_passage";
        Safe = false;
        // Light (docs/specs/light.md, section 11): Harland keeps his own cellar lit.
        Light = LightDim;
        AddSpawn("/domains/hollow/obj/wall_lantern");
        LongDesc += " A storm lantern hangs on a nail by the stair.";

        AddDetail("barrels,barrel,casks,cask", "Oak barrels stacked three high against the wall. Most are empty, and the few that are not have been gnawed at the bunghole.");
        AddDetail("crates,crate,broken crates,broken crate", "Wooden crates lie splintered across the floor, their lids pried off and their straw scattered. Something with sharp teeth did the prying.");
        AddDetail("sacks,sack,grain sacks,grain sack,chewed grain sacks", "Burlap grain sacks slump against the crates, their seams chewed open and the grain inside trodden into the dirt.");
        AddDetail("footprints,footprint,floor,dirt floor,prints", "Small, clawed footprints cover the dirt floor, crossing and recrossing one another. Some of them are larger than a rat's, and far too many for a single rat.");
        AddDetail("opening,hole,wall,far wall,rough opening", "A ragged hole has been smashed through the far wall, its edges of crumbled stone still pale with fresh dust. Cold air breathes out of it from the dark passage beyond.");

        AddReadable("card,post,notice,sign,harland's card,stair post",
            "HARLAND'S CARD, tacked to the stair post.\n  The lamps end at the Smuggler Hall. Past it is dark, and I will not hear a word about it after.\n  One torch from the bin, free, the once. More at the bar: five gold, the pitch torch, which burns longer.\n  A cloth strip and some tallow, wound tight, make a rag torch if you have the wit. Ask me and I'll show you.",
            "A card is tacked to the stair post. You could {cmd}read{/} it.");
        AddSpawn("/domains/hollow/obj/torch_bin");

        AddSpawn("/domains/hollow/mob/large_rat", 3);
    }
}
