// @inherit /std/shopkeeper

public class TravelingMerchant : Shopkeeper
{
    public override void Create()
    {
        EnableCommands();
        Name = "Merchant";
        Ids.Add("traveling merchant");
        Ids.Add("trader");
        ShortDesc = "a traveling merchant";
        LongDesc = "A cheerful, dusty man with a pack twice his size, hung with pots, coils of rope and bundles of dried herbs. He will sell you what you need for the road.";
        Level = 2;
        MaxHp = 18;

        AddChat("Potions for the brave and rations for the hungry! Buy, sell, or just browse, friend.");
        AddChat("I've traveled the northern road for twenty years. The wilds grow stranger every season.");
        AddTopic("potion,potions,healing", "Minor healing potions, fifteen gold a vial. They'll put you back together after a scrap.");
        AddTopic("road,north,wolf,wolves", "Keep a sharp eye on the North Road. A wolf has been seen prowling there.");
        AddEmote("The merchant straightens his stall and hums a tuneless song.", 5);

        AddStock("/domains/hollow/obj/healing_potion", 15);
        AddStock("/domains/hollow/obj/trail_ration", 5);
    }
}
