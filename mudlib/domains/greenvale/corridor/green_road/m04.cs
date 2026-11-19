// @inherit /std/room

public class GrTheSunlessMile : Room
{
    public override void Create()
    {
        ShortDesc = "The Green Road, the Sunless Mile";
        LongDesc = "The road runs dead straight for a mile through trees so tall and dense that the day is little brighter than dusk. The ground on either side is bare, trodden earth with nothing growing, and the silence has a weight to it. The wheel ruts here are old and deep, set with flat stones where carters have laid them down against the mud. Travelers walk this stretch close together and without talking.";
        Exits["south"] = "/domains/greenvale/corridor/green_road/m03";
        Exits["north"] = "/domains/greenvale/corridor/green_road/m05";
        Safe = false;
        DangerBand(6, 7);

        AddDetail("mile,road,straight,line", "A long, straight, empty road. It was driven through the forest by someone who wanted it to be quick, and the forest has never forgiven it.");
        AddDetail("trees,trunks,pines,firs,boughs,canopy,dark", "The trunks rise bare for forty feet before the first branch, as if the lower boughs had given up reaching for light and died. Above that, the crowns knit into a roof.");
        AddDetail("ground,earth,bare,soil", "The soil is dark, hard and bare, strewn with a deep litter of old needles and leaves. Only fungi grow here, grey and pale.");
        AddDetail("stones,flat stones,ruts,rut", "Flat stones are laid in the worst of the ruts, set there by hand and kept in place by carters who grumble about it and do it anyway.");
        AddDetail("fungus,fungi,toadstools,mushrooms", "Pale toadstools in rings, some of them as big as plates. They look wholesome and are not worth the risk.");
        AddDetail("silence,quiet,birds,birdsong", "There is no birdsong. Whether the birds avoid the Sunless Mile or only the dark does not seem to matter.");

        AddEncounter("/domains/greenvale/corridor/mob/boar_brute", 10);
    }
}
