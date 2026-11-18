// @inherit /std/room

public class EeLastFences : Room
{
    public override void Create()
    {
        ShortDesc = "The Edge Road, the Last Fences";
        LongDesc = "The last wooden fences of the farms behind you run out here, their rails grey and leaning, and the fields beyond them give way to rough pasture and bramble. Ahead the road narrows from a cart's breadth to something barely wider than two walkers abreast, and a dark line of trees stands along the northern skyline like a wall. Swallows still stitch the air over the stubble, but the smell has changed from hay and dung to leaf-mould and cold resin.";
        Exits["south"] = "/domains/greenvale/corridor/south_road/m11";
        Exits["north"] = "/domains/greenvale/corridor/eldergreen_edge/m02";
        ResetSeconds = 120;

        AddDetail("fences,fence,rails,rail,posts", "The fence rails are split oak, silvered by weather and bowed where cattle have leaned on them. Past this point nobody has bothered to put up another.");
        AddDetail("pasture,bramble,brambles,fields,stubble", "Rough pasture runs up to a tangle of bramble, the ground too stony and too far from any yard to be worth the plough. A few sheep have been here, and left tufts of wool on the thorns.");
        AddDetail("road,ruts,verge", "The road is still rutted by farm carts, but the ruts are fewer and older, and grass has begun to grow along the crown of it.");
        AddDetail("trees,wood,treeline,skyline,line", "The treeline is a long dark band, ragged at its southern edge where saplings have crept out into the pasture. Behind the saplings the trunks stand much taller than any tree on the farms.");
        AddDetail("swallows,birds,swallow", "Swallows swoop low over the stubble after midges, and one flicks past your head and away toward the wood.");
        AddDetail("thistle,thistles,verge thistles", "Thistles stand head-high along the verge, purple-crowned and humming with bees. Their prickly stalks would reward a careful gatherer.");
        AddReadable("post,boundary post,brand", "Burned deep into the leaning gatepost, under a farmer's brand:\n\"THE STONE BRIDGE HOLDINGS END HERE.\"\nSomeone has scratched below it, in a different hand: \"And so does the kindness.\"", "A weathered gatepost with writing burned into it stands at the roadside. You could {cmd}read{/} it.");
        AddNode("/domains/greenvale/corridor/obj/thistle_patch");

        AddEncounter("/domains/greenvale/corridor/mob/forest_spider", 5);
        AddEncounter("/domains/greenvale/corridor/mob/forest_wolf", 8);
    }
}
