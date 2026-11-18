// @inherit /std/room

public class GrTheOldShrine : Room
{
    public override void Create()
    {
        ShortDesc = "The Old Shrine";
        LongDesc = "Where the causeway ends and firm ground begins, a small wayside shrine stands under a great beech, a squat hut of grey stone with a slate roof and an open front. A lamp burns in the niche inside, its flame steady in the stillness, and the ground about it has been swept and the weeds pulled. Offerings crowd the step: ribbons, nails, small coins, a heel of bread. The road passes close, and almost everyone slows as they pass.";
        Exits["south"] = "/domains/greenvale/corridor/green_road/m05";
        Exits["north"] = "/domains/greenvale/corridor/green_road/m07";
        Safe = false;

        AddDetail("shrine,hut,niche,roof,slate,stone", "A hut of fitted grey stones, no taller than a man with an arm raised, and without a door. The stone is old and the slate is new: someone keeps it roofed.");
        AddDetail("lamp,flame,oil,wick", "A small oil lamp with a horn shield, trimmed and filled and burning. Whoever tends it has never let it go out in living memory.");
        AddDetail("offerings,offering,ribbons,ribbon,nails,coins,bread,step", "Ribbons tied to a hanging rail, bent nails, a few copper coins, a heel of bread gone green. Travelers leave what they have, and the offerings are cleared away each dawn.");
        AddDetail("beech,tree,great beech,bark,roots", "A huge old beech with a girth of three men. Its smooth bark is carved with initials, dates and little pictures of boats, wagons and wolves, some of them very old.");
        AddDetail("acorns,acorn,twig,twigs,knot,knotted twig,stag,antler", "At the back of the shrine, tucked in the gap between wall and tree, lie a handful of acorns and a twig bound in a knot. They are not like the other offerings, and nobody here seems to know who leaves them.");
        AddDetail("swept,ground,weeds,broom", "The ground is swept bare in a neat half-circle. A birch broom leans against the wall.");
        AddReadable("plaque,carving,inscription,lintel,words,stone words", "Cut into the lintel, in letters worn almost smooth by thumbs:\n\nFOR THOSE WHO WENT BEFORE\nAND THOSE WHO DID NOT ARRIVE.\nKEEP THE LIGHT. KEEP THE ROAD.", "Words are cut into the lintel. You could {cmd}read{/} them.");

        AddSpawn("/domains/greenvale/corridor/green_road/npc/melisande");
        AddSpawn("/domains/greenvale/corridor/green_road/npc/isabeau");
    }
}
