// @inherit /std/room

public class BgNorthernRoad : Room
{
    public override void Create()
    {
        ShortDesc = "The Northern Road";
        LongDesc = "The Northern Road climbs gently away from Northgate between hedgerows and plowed fields, a dusty track marked by the ruts of many wagons. Ahead the land rises toward forested hills, and beyond them, blue and grey and capped with snow, stand the mountains, filling the northern sky. A milestone stands at the roadside, and a thin line of smoke rises from a farmstead in the fields. The wind that comes down the road is cold, and it smells of pine and distant rock.";
        Exits["south"] = "/domains/greenvale/oakenfell/founders/northgate";
        BlockExit("north", "The Northern Road climbs on toward the mountains, but it is not yet open to travelers.");

        AddDetail("road,northern road,ruts,wagons,track,dust", "A broad dusty road, rutted by wagons, that runs straight north through the fields and then begins to climb. Hoof prints cross it in every direction.");
        AddDetail("hedgerows,hedges,fields,plowed fields,farmstead,smoke,farm", "Hawthorn hedgerows divide the plowed fields, and a farmstead sits a little way off, its chimney trailing a thread of smoke. A dog barks once, distantly, and stops.");
        AddDetail("hills,forest,forested hills,trees,pine,pines", "Dark forested hills rise ahead, their pines black against the sky. The road vanishes into them.");
        AddDetail("mountains,mountain,peaks,snow,sky,skyline", "The mountains fill the northern horizon, a long jagged wall of grey and blue stone. Their tops are white even now, and they look much farther away than they are close.");
        AddReadable("milestone,stone,marker,mile stone", "The milestone's face reads:\n\"OAKENFELL, 1.\nTHE MOUNTAINS.\"", "A milestone leans at the roadside. You could {cmd}read{/} it.");
    }
}
