// @inherit /std/room

public class SgStables : Room
{
    public override void Create()
    {
        ShortDesc = "Southgate Stables";
        LongDesc = "A long, whitewashed stable yard opens off the street, with a row of stalls under a slate roof and a hayloft above. The air is warm and rich with horse, straw, leather and liniment. Carriage horses, cart-ponies and a few proud saddle horses lean over their half-doors to watch you. A farrier's anvil rings somewhere out of sight, and a stablemaster's board hangs by the tack room. The finest beasts are kept behind a gate at the back of the yard, away from strangers.";
        Exits["west"] = "/domains/greenvale/oakenfell/southgate/wayfarers_row";
        ResetSeconds = 120;

        AddDetail("stalls,stall,half-doors,doors", "Each stall is clean, with fresh straw and a nameplate: Duke, Marigold, Old Nell, Thunder. Thunder is a placid grey cob, and Marigold bites.");
        AddDetail("horses,horse,ponies,pony", "Mostly cart-horses and ponies, broad and patient, with a few lean riding horses among them. The best look at you as if they were considering the purchase.");
        AddDetail("hayloft,hay,straw", "The loft is stacked with sweet-smelling hay bales. A cat sleeps in the open hatch.");
        AddDetail("tack,tack room,saddles,bridles", "Saddles and bridles hang in neat rows in the tack room, glossy with oil. Every piece is marked with an owner's name.");
        AddDetail("anvil,farrier", "A farrier's anvil and fire stand in a lean-to at the yard's end. Horseshoes hang on a nail by the dozen.");
        AddDetail("gate,back gate,fine beasts,best horses", "A stout gate bars the back of the yard. Beyond it you glimpse sleek, well-bred horses with braided manes, which are clearly not meant for casual hire.");
        AddReadable("board,stablemaster's board,sign", "STABLES, SOUTHGATE\nStabling by the night, feed and water included.\nCarriage and cart horses for hire to known customers only.\nSaddle horses and mounts: inquire within when the city knows you better.\nNo naked flames in the hayloft.", "A stablemaster's board hangs by the tack room door. You could {cmd}read{/} it.");

        AddSpawn("/domains/greenvale/oakenfell/southgate/npc/stablemaster_garrow");
    }
}
