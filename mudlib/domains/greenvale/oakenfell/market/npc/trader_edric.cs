// @inherit /std/npc

public class MkTraderOsric : Npc
{
    public override void Create()
    {
        Name = "Edric Bale";
        Ids.Add("edric");
        Ids.Add("edric bale");
        Ids.Add("trader");
        Ids.Add("ironmonger");
        Ids.Add("stallholder");
        ShortDesc = "Edric Bale, an ironmonger";
        LongDesc = "A lean, sharp-faced man with a tape over one shoulder and a nail between his teeth. His stall is a clutter of pots, nails, hinges and hooks, each hung in perfect order.";
        Level = 1;
        MaxHp = 15;

        AddChat("Pots, pans, nails and hooks! The best iron in Greenvale, forged in the Guild Quarter!");
        AddChat("A good pot lasts a lifetime. A bad pot lasts till you cook with it.");
        AddChat("Mind the hooks, friend. They're sharper than they look.");
        AddTopic("pots,pans,nails,hooks,iron", "I supply the inns and kitchens by the crate, mostly. A traveler wants a pan for one night, I'd say sleep at the Stag and let them cook for you.");
        AddTopic("smith,smiths,guild,guild quarter", "The smiths of the Crafters' Court make the best iron in Greenvale, and they know it. Their prices know it too.");
        AddTopic("weapons,armor,armorer", "Brann at the corner stall will have armor when his shipment is unloaded. For blades, ask Kessa up in the North Rows.");
        AddEmote("Edric Bale counts a handful of nails and hangs them in a perfect row.", 8);
    }
}
