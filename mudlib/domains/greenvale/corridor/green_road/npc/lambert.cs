// @inherit /std/shopkeeper

public class GrLambert : Shopkeeper
{
    public override void Create()
    {
        Name = "Lambert";
        Ids.Add("lambert");
        Ids.Add("merchant");
        Ids.Add("caravan merchant");
        Ids.Add("trader");
        Ids.Add("shopkeeper");
        ShortDesc = "Lambert, the caravan merchant";
        LongDesc = "A heavy, sandy-haired man in a good wool coat gone shiny at the cuffs, sitting on an upturned crate with a strongbox between his boots. He has the cheerful eyes of a man who sells things and the tired mouth of one who has done it for a long way in the dark.";
        Level = 3;
        MaxVitality = 40;

        AddChat("Potions and provisions, friend, as good as you will find between here and the city. Try {cmd}browse{/}.");
        AddChat("Prices are a touch above Oakenfell's, I will not pretend otherwise. You are paying for the miles, and for me not being eaten.");
        AddChat("Camp pasties, hot from the pot. Mutton and onion. A coin well spent on a cold night.");
        AddTopic("potion,potions,healing,vial", "Minor healing potions, twenty-four gold a vial. Cherry and iron, the same as in the Hollow, but I carried this one three days through the wet. {cmd}buy potion{/}.");
        AddTopic("ration,rations,food,trail ration,biscuit", "Trail rations at seven gold. Biscuit, dried apple, salted meat. They keep, and that is the best that can be said of them.");
        AddTopic("pasty,pasties,pie,stew,supper", "My own cook's pasties, mutton and onion, nine gold. Hot from the pot at the fire, which is more than you will find on the road north.");
        AddTopic("caravan,wagons,consortium,trade", "Four wagons, sixteen oxen and a Consortium contract to deliver to the Market in Oakenfell. We halt here every night, because nobody halts anywhere else.");
        AddTopic("road,north,south,danger,goblins,raiders", "Goblins on the road, and worse in the wood. We have not lost a wagon yet, but I have lost a good many nights' sleep. Keep to the road and do not wander after dark.");
        AddTopic("camp,fee,camp-fee", "The camp-fee is a copper a head and well worth it. Hereward's lads keep the watch and I keep the stew.");
        AddEmote("Lambert counts coins from the strongbox into neat stacks and counts them again.", 6);
        AddEmote("Lambert looks at the dark between the trunks and then, firmly, at his ledger.", 6);
        AddEmote("Lambert stretches his back and mutters about the price of oxen.", 6);

        AddStock("/domains/hollow/obj/healing_potion", 24);
        AddStock("/domains/hollow/obj/trail_ration", 7);
        AddStock("/domains/greenvale/corridor/green_road/obj/gr_camp_pasty", 9);
    }
}
