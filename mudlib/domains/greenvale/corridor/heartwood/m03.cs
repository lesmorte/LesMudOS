// @inherit /std/room

public class HwM03 : Room
{
    public override void Create()
    {
        ShortDesc = "The Heartwood Road, the Root Stair";
        LongDesc = "The road climbs a slope where the roots of the great trees have spread across it and made rough steps, each one worn smooth in the middle by the passing of feet. The trunks above are close together here, and the ground between them is hummocked and dark. To the east a narrow trail leaves the road and heads toward a long, low shape of grey stone showing between the boles. The air smells of wet earth and old leaf.";
        Exits["south"] = "/domains/greenvale/corridor/heartwood/m02";
        Exits["north"] = "/domains/greenvale/corridor/heartwood/m04";
        Exits["east"] = "/domains/greenvale/corridor/heartwood/w1";

        AddDetail("roots,root,steps,stair,stairs,root stair", "Roots as thick as a barrel lie across the slope in shallow curves, and the road has been cut and trodden across them. The tops are polished by boots and hooves. Between them leaf mould has packed into steps of its own.");
        AddDetail("slope,hill,rise,climb", "The slope is gentle but long. At the top the trees stand a little farther apart, and you can see that the road bends round the shoulder of the hill and does not go over it.");
        AddDetail("trail,east,path,track,side trail", "The trail east is narrow and plain: a groove in the moss that someone has kept open with a blade. It leads up toward the grey stone, and it is much older than the road on which you stand.");
        AddDetail("stone,grey stone,shape,wall,ruin,ruins", "Through the trunks to the east a long wall of dark stone shows, higher than a house, furred with moss and held together by roots. From here you cannot tell how far it goes.");
        AddDetail("trunks,trees,boles,tree", "The trees on this slope grow so close that their roots have joined in places. In one spot two trunks have grown through each other and stand like a single tree with four limbs.");
        AddDetail("smell,air,earth,leaf", "The smell is dark and cold and sweet, like the inside of a cellar that has been shut for years.");
    }
}
