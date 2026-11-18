// @inherit /std/room

public class RvOldRiverStreet : Room
{
    public override void Create()
    {
        ShortDesc = "Old River Street";
        LongDesc = "Old River Street runs east from the square toward the water, a broad cobbled way lined with tall warehouses of tarred timber and brick. Carts and handbarrows rumble over the stones, and the creak of hoist-chains and the shouts of men loading sacks come from every doorway. The smell changes with each step, from tar and hemp to wet rope and, faintly, the river itself. A green door with a pale shield painted on it stands on the north side of the street, and the gulls wheel above the roofs, looking for dropped fish.";
        Exits["west"] = "/domains/greenvale/oakenfell/founders/square";
        Exits["east"] = "/domains/greenvale/oakenfell/river/quay";
        Exits["north"] = "/domains/greenvale/oakenfell/river/riverwatch_office";

        AddDetail("warehouses,warehouse,doorways,hoists,hoist,chains,timber,brick", "Tall, deep warehouses of tarred timber on brick footings, each with a hoist beam jutting from the top floor and a chain swinging in the breeze. Painted on the doors are the marks of the merchant houses that lease them: a wheel, a crown, a three-masted ship, a sheaf of wheat.");
        AddDetail("street,cobbles,cobblestones,stones,paving,ground", "Grey cobbles scarred by iron wheels and slick with spilled brine. The center of the street is worn into two deep troughs, and the gutters on either side run with river water and fish scales.");
        AddDetail("carts,cart,wagons,wagon,handbarrows,barrows,sacks,barrels,crates", "Carts of every size are drawn up along the walls, heaped with sacks, barrels, bales of wool and crates of river fish packed in straw. A cart's hub bears the carter's name burned into the wood, in case of argument.");
        AddDetail("green door,door,pale shield,shield,shield door,riverwatch door", "A heavy green door, studded with iron, with a pale shield painted on its planks. This is the city office of the Riverwatch Rangers, and it stands to the {cmd}north{/}.");
        AddDetail("gulls,gull,birds,roofs,sky", "Grey and white gulls wheel above the roofs, screaming. One sits on a hoist beam with a stolen herring and the watchful face of a thief.");
        AddReadable("tally board,tally,board,chalk,chalkboard,slate", "On a slate by the nearest warehouse, in untidy chalk:\n\"IN: 40 sacks barley (Corvane), 12 bales wool (Dunne & Sons), 6 casks pitch.\nOUT: 22 barrels salt fish, 30 hides, 1 piano (do not drop).\nCartage to the Market at the posted rate. No credit.\"", "A slate hangs by a warehouse door, covered in chalk. You could {cmd}read{/} it.");

        AddSpawn("/domains/greenvale/oakenfell/river/npc/hob_marlow");
        AddSpawn("/domains/greenvale/oakenfell/river/npc/piet_dunne");
    }
}
