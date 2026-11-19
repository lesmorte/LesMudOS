// @inherit /std/room

public class SrM04 : Room
{
    public override void Create()
    {
        ShortDesc = "The South Road, Barley Fields";
        LongDesc = "Barley stands on both sides of the road, tall and bearded, and the wind runs across it in long silver waves. Wooden fences keep the fields from the road, each rail grey with weather and furred with lichen. Skylarks hang overhead in the heat, singing as though nothing could ever be wrong, and crows quarrel in the stubble beyond. A scarecrow stands in the near field in a coat that has seen better scarecrows.";
        Exits["south"] = "/domains/greenvale/corridor/south_road/m03";
        Exits["north"] = "/domains/greenvale/corridor/south_road/m05";
        ResetSeconds = 120;
        DangerBand(5, 6);

        AddDetail("barley,fields,field,crop,crops", "The barley is almost ripe, gold at the tips, and it rustles like a crowd whispering. The ears are heavy and bow toward the road.");
        AddDetail("fence,fences,rails,rail,lichen", "Split-rail fences, lashed with withies and shored up with stones. The lichen on them is the colour of old brass.");
        AddDetail("larks,skylarks,skylark,birds", "Two larks climb the air in a spiral, singing without pause, and you lose them in the glare.");
        AddDetail("crows,crow,stubble", "A dozen crows work the stubble of the harvested corner, and every few moments one looks up and checks that you are not coming closer.");
        AddDetail("windmill,sails,hill", "Back to the south and west you can see the windmill, still turning, the sails touched with light each time they come round.");
        AddReadable("scarecrow,coat,figure,plate", "The scarecrow wears a brown coat patched in four colours, a straw hat, and a tin plate tied on its chest. The plate is scratched with letters:\n\"WATCHMAN. HIRED 3 SUMMERS. HAS NOT ONCE ASKED FOR A RAISE.\"\nOne crow sits on its hat, entirely unimpressed.", "A scarecrow stands in the field, a plate on its chest. You could {cmd}read{/} it.");

        AddEncounter("/domains/greenvale/corridor/mob/field_rabbit", 8);
        AddEncounter("/domains/greenvale/corridor/mob/grey_fox", 5);
    }
}
