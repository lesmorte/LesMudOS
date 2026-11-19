// @inherit /std/room

public class EeGreenGate : Room
{
    public override void Create()
    {
        ShortDesc = "The Edge Road, the Green Gate";
        LongDesc = "Two enormous oaks stand one on either side of the road, their lowest limbs reaching across and interlacing overhead in a living arch that some long-ago hand trained there. Beyond the arch the road dips into shade, and the canopy beyond looks nearly unbroken. Sunlight has reached the ground all the way along the Edge, but beyond this gate it will not. The air under the arch is still and cool, and carries the smell of leaf-mould and old, old wood.";
        Exits["south"] = "/domains/greenvale/corridor/eldergreen_edge/m10";
        Exits["north"] = "/domains/greenvale/corridor/green_road/m01";
        ResetSeconds = 120;
        DangerBand(5, 7);

        AddDetail("oaks,gate,arch,limbs,branches,trees", "The two oaks are so alike in size they might have been planted together. Their limbs have been trained, over generations, to meet and grow into one another, so the arch is no longer two trees but one thing.");
        AddDetail("canopy,shade,north,beyond,road ahead", "Beyond the arch the canopy closes. The road runs into a green gloom, and the nearest trunks beyond the gate are hung with moss on every side.");
        AddDetail("grafts,knots,joint,joints,scars", "Where the limbs meet, the bark has healed over in great ridged knots, and bits of old rope are still buried in the wood where it was bound together.");
        AddDetail("rope,ropes,ribbons,charms,tokens,offerings", "Faded ribbons, bits of knotted cord and a child's wooden horse are tied to the low branches. Travelers have been leaving them here for as long as there has been a gate.");
        AddDetail("road,ruts,moss", "The road here is old and well kept; Rangers have laid flat stones across a wet patch, and the stones are worn hollow.");
        AddDetail("air,smell,cool", "The temperature drops by a noticeable degree as you step under the arch, and the smell turns to deep forest.");
        AddReadable("sign,plank,notice,board", "Nailed to the south oak at head height, a painted plank:\n\"THE GREEN ROAD BEGINS. Keep to the road. Travel in company after dusk. Rangers keep the road as far as the Merchant Camp. Ever Vigilant.\"", "A painted plank is nailed to the nearest oak. You could {cmd}read{/} it.");

        AddEncounter("/domains/greenvale/corridor/mob/forest_wolf", 12);
        AddEncounter("/domains/greenvale/corridor/mob/forest_spider", 8);
    }
}
