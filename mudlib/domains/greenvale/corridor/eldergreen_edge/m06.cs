// @inherit /std/room

public class EeFernBanks : Room
{
    public override void Create()
    {
        ShortDesc = "The Edge Road, Fern Banks";
        LongDesc = "Ferns crowd both banks of the road here, shoulder-high and uncurling in green waves, and a thin stream trickles in a stone-lined channel beside the way. The oaks are mixed with ash and beech now, and the road is dappled green and gold. The air is damp, smells of wild garlic and wet stone, and hums with midges in the shaded dips. Deer have crossed the road more than once; the mud at the stream's edge is stamped with their slots.";
        Exits["south"] = "/domains/greenvale/corridor/eldergreen_edge/m05";
        Exits["north"] = "/domains/greenvale/corridor/eldergreen_edge/m07";
        ResetSeconds = 120;

        AddDetail("ferns,fern,banks,bracken", "The ferns are broad-fronded and wet to the touch, and in their lower shade grow clumps of wild garlic, its white stars thick on the banks.");
        AddDetail("stream,channel,water,trickle", "The stream runs clear over brown stones through a channel somebody lined with flat rock long ago. It tastes cold and faintly of iron.");
        AddDetail("herbs,herb patch,garlic,wild garlic,patch,mint,watercress", "Where the stream spreads into a damp hollow, watercress, water-mint and wild garlic grow in a thick patch that a herb-gatherer would be glad to find.");
        AddDetail("deer,slots,mud,tracks,prints", "The deer have left their neat split prints all through the mud, and among them, newer and larger, the broad pad of a wolf, which seems to be following them.");
        AddDetail("ash,beech,trees,oaks", "Smooth grey beeches stand among the oaks now, and a pair of ash trees lean together over the road like gossips.");
        AddDetail("midges,insects,air", "The midges rise in little clouds each time you step through a shaft of sun, and settle again behind you.");

        AddEncounter("/domains/greenvale/corridor/mob/forest_spider", 10);
        AddEncounter("/domains/greenvale/corridor/mob/forest_wolf", 8);
    }
}
