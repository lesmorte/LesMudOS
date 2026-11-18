// @inherit /std/room

public class HwM01 : Room
{
    public override void Create()
    {
        ShortDesc = "The Heartwood Road, the Old Growth";
        LongDesc = "The road passes under the first of the great trees and the light changes at once, from green to a dimmer brown. The trunks stand far apart and each is wider than a cart is long, their bark in plates as thick as a hand. Nothing grows beneath them but fern and pale moss, and the road is soft with centuries of fallen needle and leaf. It is quieter than the Green Road was: the birds are few and high, and your boots make no echo.";
        Exits["south"] = "/domains/greenvale/corridor/green_road/m11";
        Exits["north"] = "/domains/greenvale/corridor/heartwood/m02";

        AddDetail("trunks,trunk,trees,tree,oaks,oak,giants", "The nearest trunk takes eleven of your paces to walk around. Its first branch is higher than any roof in the Hollow, and its roots rise out of the ground in ridges that you must step over. Nothing in the Green Road behind you is half its age.");
        AddDetail("bark,plates", "The bark has split into grey plates, each deep enough to hide your hand to the wrist. Lichen grows on them in rings, and some of the rings are wider than a shield.");
        AddDetail("fern,ferns,moss,undergrowth", "Fern and a thin pale moss cover the ground between the roots. There is no bramble and no young growth: the canopy keeps the light from everything except the old.");
        AddDetail("road,ground,needles,leaf,leaves,dirt,path", "The road is a strip of packed earth, softer here than any road you have walked, with no stone showing. The leaf mould is deep, and the ruts of old carts are filled with it.");
        AddDetail("canopy,branches,sky,light", "Far above, the branches meet and hold the sky off. The light that comes down is brown-green and has no direction.");
        AddDetail("birds,silence,quiet,sound,sounds", "What birds there are keep to the topmost boughs. Below them there is only the creak of wood moving very slowly, and your own breath.");
        AddDetail("south,green road,behind", "Behind you the Green Road's lower trees show bright and ordinary, as though you had stepped out of a smaller wood.");
        AddEncounter("/domains/greenvale/corridor/mob/black_bear", 8);
    }
}
