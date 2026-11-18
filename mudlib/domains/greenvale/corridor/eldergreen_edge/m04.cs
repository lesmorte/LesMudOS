// @inherit /std/room

public class EeUnderEaves : Room
{
    public override void Create()
    {
        ShortDesc = "The Edge Road, under the Eaves";
        LongDesc = "The road passes beneath the eaves of the Eldergreen Wood proper, where the roadside trees have reached across to touch one another and the light turns the colour of tea. Shafts of sun still lance down to the road in places, and in them dust and pollen drift like slow snow. Wood sorrel and moss carpet the banks, and a sweet damp smell of earth and rotting leaves rises from the ditch. Behind you the farm country is already a memory; ahead the trunks stand closer.";
        Exits["south"] = "/domains/greenvale/corridor/eldergreen_edge/m03";
        Exits["north"] = "/domains/greenvale/corridor/eldergreen_edge/m05";
        ResetSeconds = 120;
        DangerBand(5, 7);

        AddDetail("eaves,branches,trees,boughs,roof", "The branches overhead interlace in an uneven arch. Where one has fallen, the gap lets in a shaft of sun as solid-looking as a plank.");
        AddDetail("light,shafts,sun,sunlight,pollen,dust", "Pollen turns every sunbeam into a bright pillar. Where the road passes between two shafts the shade between them is cool as a cellar.");
        AddDetail("sorrel,wood sorrel,moss,banks,herbs,herb patch,yarrow,patch", "A bank of clover-leaved wood sorrel runs beside the road, and among it grow yarrow, self-heal and a few sprigs of pale comfrey. A herb-gatherer would find the patch worth the stoop.");
        AddDetail("ditch,leaves,rot,mould", "The ditch holds black water and drifts of last autumn's leaves. Something small has been rooting in the mould, and left neat holes.");
        AddDetail("tracks,paw prints,prints,scratches,bark", "On the bark of the nearest trunk are four parallel scratches, higher than your head. Whatever made them has not gone far, or has not forgotten the place.");
        AddDetail("road", "The road is still firm and wide enough for a wagon, but roots have begun to lift its edges, and moss grows in the wheel ruts.");
        AddNode("/domains/greenvale/corridor/obj/herb_patch");

        AddEncounter("/domains/greenvale/corridor/mob/forest_spider", 8);
        AddEncounter("/domains/greenvale/corridor/mob/forest_wolf", 8);
    }
}
