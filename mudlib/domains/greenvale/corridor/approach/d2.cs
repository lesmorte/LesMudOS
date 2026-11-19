// @inherit /std/room

public class OaD2 : Room
{
    public override void Create()
    {
        ShortDesc = "The Panther's Ledge";
        LongDesc = "A wide ledge of sun-warmed rock juts from the hillside above a ravine, screened by yews and overlooking the whole broken line of the aqueduct. The stone is scored with deep claw-marks, strewn with the bones of deer, and polished by the passing of something heavy and soft-footed, and the air has a musky, big-cat smell. Through a gap in the trees to the north the towers of Oakenfell stand small and clear on the horizon, which offers little comfort, because they are a long way off and you are not alone here.";
        Exits["south"] = "/domains/greenvale/corridor/approach/d1";
        Exits["north"] = "/domains/greenvale/corridor/approach/d3";
        ResetSeconds = 1800;
        DangerBand(9, 10);

        AddDetail("ledge,rock,stone,sun,warm rock", "A flat shelf of limestone, twenty paces long, baked in the afternoon sun. The rock beneath your feet is warm, and you suspect that someone has been lying on it.");
        AddDetail("claw marks,claws,scratches,marks,scars", "Four parallel grooves, each the width of a finger, are raked into the trunk of the nearest yew at the height of a man's chest. The sap beside them is still bright.");
        AddDetail("bones,deer bones,remains,carcass,skulls", "A pile of picked-clean bones lies in the shade: roe deer, a young boar, and one human thigh bone whose owner nobody will want to guess at.");
        AddDetail("ravine,drop,gorge,edge,cliff", "The ledge ends in air; below, the ravine plunges forty feet to a stream and a tangle of boulders. A cat would not fall.");
        AddDetail("yews,yew,trees,screen,branches", "Old yews screen the ledge, black-green and hung with red berries. It is a good place to watch the world from, and to wait.");
        AddDetail("towers,oakenfell,city,horizon,view,gap", "Through the gap you can see, small and clear, the towers of Oakenfell. They look quite close, and a long way off, both at once.");

        AddSpawn("/domains/greenvale/corridor/mob/forest_panther");
    }
}
