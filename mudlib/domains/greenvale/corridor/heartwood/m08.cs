// @inherit /std/room

public class HwM08 : Room
{
    public override void Create()
    {
        ShortDesc = "The Heartwood Road, the Green Dusk";
        LongDesc = "The canopy closes so completely here that noon looks like evening. The light that reaches the road is green and without shadow, and the trunks fade into it a few dozen paces off. Pale ferns shoulder the road on both sides, taller than a child, and the only colour is the faint blue glow of a fungus on a fallen bough. Everything is damp, still and very large, and the road seems narrower than it was.";
        Exits["south"] = "/domains/greenvale/corridor/heartwood/m07";
        Exits["north"] = "/domains/greenvale/corridor/heartwood/m09";
        DangerBand(7, 8);

        AddDetail("light,dusk,green light,gloom,shadow,shadows", "The light has no source. It falls through the canopy in so many layers that it arrives from everywhere and casts nothing. Your own hands look faintly green.");
        AddDetail("ferns,fern,fronds,bracken", "The ferns are pale and wide, the fronds as long as your arm, and they close over the road at the edges. They move without a wind, a little, when something small passes underneath.");
        AddDetail("fungus,glow,bough,fallen bough,blue", "On a fallen bough a shelf of fungus gives off a dim bluish light. It is bright enough to see the hand that holds it, and no brighter.");
        AddDetail("trunks,trunk,trees,tree,canopy", "The trunks go up into a dark that the eye does not follow. Between them, at head height, hang threads of grey lichen as fine as hair.");
        AddDetail("road,ground,dirt", "The road is damp, dark and plain. It has been used by travelers for a long time, and has been kept by none.");
        AddDetail("silence,quiet,sound,air", "There is no birdsong here. Water gathers on the leaves and falls, a slow tick, tick, in the stillness.");
        AddEncounter("/domains/greenvale/corridor/mob/heartwood_goblin", 10);
    }
}
