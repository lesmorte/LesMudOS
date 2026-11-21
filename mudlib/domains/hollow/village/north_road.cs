// @inherit /std/room

public class NorthRoad : Room
{
    public override void Create()
    {
        ShortDesc = "North Road";
        LongDesc = "A dusty road stretches north into the wilderness and south toward Vincent's Hollow. Wagon ruts and horse tracks mark the path used by traders and adventurers. Towering pines sway gently in the breeze while distant mountains dominate the northern skyline. A carved wooden sign welcomes travelers to the settlement. A gate in a plank fence beside the road stands open on a trampled yard, from which comes the steady thud of arrows going into straw.";
        Exits["south"] = "/domains/hollow/village/gate";
        Safe = false;
        Exits["yard"] = "/domains/hollow/village/practice_yard";
        Exits["north"] = "/domains/greenvale/vale_road/road";
        LockExit("north", "travel pass", "The road north leads toward Oakenfell through the wilds beyond Vincent's Hollow, and the Adventurers Guild does not let the untested go there. You need a travel pass from the guild.");

        AddDetail("road,dusty road,path", "A packed earth road, dusty in the dry weather and churned to mud where the wagons have passed. It runs straight north into the trees.");
        AddDetail("ruts,tracks,wagon ruts,horse tracks,hoof prints", "Deep wagon ruts run side by side along the road, and the hoof prints of horses cut through the dust between them.");
        AddDetail("pines,trees,pine trees,tree", "Tall pines crowd the verge of the road, their needles whispering in the breeze. Their trunks are dark with resin.");
        AddDetail("mountains,mountain,peaks,skyline", "Distant mountains rise grey and blue above the forest, their peaks capped with snow even in the late season.");
        AddDetail("gate,plank fence,fence,yard,trampled yard,practice yard", "A plank fence with an open gate stands at the roadside, and beyond it you can see straw butts and a dummy on a post. A neat sign on the gate says \"PRACTICE YARD\". {cmd}yard{/} will take you there.");
        AddReadable("sign,village sign,wooden sign", "The sign reads:\n\"Welcome to Vincent's Hollow\nFounded by Vincent Holloway\nShelter, Ale, and Honest Trade\"", "A carved wooden sign stands beside the road. You could {cmd}read{/} it.");

        AddEncounter("/domains/hollow/npc/traveling_merchant", 15);
        AddEncounter("/domains/hollow/mob/wandering_wolf", 25);
    }
}
