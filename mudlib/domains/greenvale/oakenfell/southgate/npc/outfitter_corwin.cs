// @inherit /std/shopkeeper

public class SgOutfitterCorwin : Shopkeeper
{
    public override void Create()
    {
        Name = "Corwin Dray";
        Ids.Add("corwin");
        Ids.Add("outfitter");
        Ids.Add("corwin dray");
        Ids.Add("shopkeeper");
        Ids.Add("proprietor");
        ShortDesc = "Corwin Dray, the outfitter";
        LongDesc = "A neat, narrow man of middle years in a long grey apron with many pockets. His spectacles sit low on his nose, and he handles stock like a man who has counted everything twice.";
        Level = 2;
        MaxHp = 25;

        AddChat("Everything an adventurer forgets until it matters. Try {cmd}browse{/} and see what you lack.");
        AddChat("Rope, tinder, potions and rations. The four things between you and a bad week.");
        AddChat("I sell what keeps you alive. I buy what you can't carry. Fair and square.");
        AddTopic("potion,potions,healing", "Minor healing potions, twenty gold a vial. Dearer than the village, I know, but I keep my stock honest. {cmd}buy potion{/}.");
        AddTopic("ration,rations,food", "Trail rations: biscuit, dried apple, salted meat. Six gold. They'll keep you on your feet for a day.");
        AddTopic("rope,tinderbox,tinder,gear", "Rope for climbing, tinder for fires. Cheap, useful and rarely thought of until you're in the dark.");
        AddTopic("tool,tools,knife,kit,sickle,skinning,bleeding,harvest,gather,gathering", "A skinning knife, a bleeding kit and a harvest sickle, for taking hides, blood and plants. Carry them, don't wield them. Eight, ten and seven gold; the Hollow's prices are a little lower, as usual. {cmd}browse{/}.");
        AddTopic("village,hollow,vincent", "I've met a few of Vincent's Hollow folk. Good people. Their prices are lower and their shelves emptier.");
        AddEmote("Corwin Dray adjusts his spectacles and checks a ledger.", 8);
        AddEmote("Corwin Dray straightens a row of lanterns that was already straight.", 6);

        AddStock("/domains/hollow/obj/healing_potion", 20);
        AddStock("/domains/hollow/obj/trail_ration", 6);
        AddStock("/domains/greenvale/oakenfell/southgate/obj/hemp_rope", 8);
        AddStock("/domains/greenvale/oakenfell/southgate/obj/tinderbox", 5);
        AddStock("/domains/hollow/obj/craft/skinning_knife", 8);
        AddStock("/domains/hollow/obj/craft/bleeding_kit", 10);
        AddStock("/domains/hollow/obj/craft/harvest_sickle", 7);
    }
}
