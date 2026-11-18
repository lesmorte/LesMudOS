// @inherit /std/room

public class BgCrossingRoad : Room
{
    public override void Create()
    {
        ShortDesc = "The Crossing Road";
        LongDesc = "The Crossing Road runs east from Eastgate, wide and well kept, a straight causeway of packed gravel between flat green fields. Ditches run on both sides, and every half mile or so a post stands at the verge with a green rag tied to it, a Ranger's mark. The road is the best in Greenvale, rutted and smooth by turns, and far ahead it shimmers in the heat. It leads to Watchfort Crossing, where the Riverwatch Rangers keep their headquarters. A cart or two trundles toward the gate, and the carters nod.";
        Exits["west"] = "/domains/greenvale/oakenfell/river/eastgate";
        BlockExit("east", "The Crossing Road runs on east toward Watchfort Crossing, but it is not yet open to travelers.");

        AddDetail("road,crossing road,gravel,causeway,ruts,ditches", "A broad, well-kept road of packed gravel on a low causeway, with ditches to drain it on either side. It is plainly the work of people who care that it should last.");
        AddDetail("posts,post,rag,green rag,ranger's mark,mark,marker", "A weathered post with a green rag tied to it, standing at the verge. The Riverwatch Rangers set them along the roads to show a patrol has passed.");
        AddDetail("fields,green fields,flat fields,farms", "Flat green fields stretch away on either hand, bordered by ditches and the occasional hawthorn hedge. Not a person is in sight.");
        AddDetail("carts,cart,carters,wagons,wagon", "A farm cart creaks toward the city, laden with sacks, its carter dozing on the box while the horse knows the way.");
        AddDetail("horizon,east,heat,distance,watchfort", "Far to the east the road narrows to a point and the air shivers above it. Somewhere beyond lies Watchfort Crossing.");
        AddReadable("milestone,stone,marker,mile stone", "A milestone at the edge of the road reads:\n\"OAKENFELL, 1.\nWATCHFORT CROSSING.\"", "A milestone stands by the road. You could {cmd}read{/} it.");
    }
}
