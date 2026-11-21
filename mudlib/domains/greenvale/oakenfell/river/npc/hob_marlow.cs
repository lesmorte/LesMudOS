// @inherit /std/npc

public class RvHobMarlow : Npc
{
    public override void Create()
    {
        Name = "Hob Marlow";
        Ids.Add("hob");
        Ids.Add("marlow");
        Ids.Add("carter");
        ShortDesc = "Hob Marlow, a carter";
        LongDesc = "A thick-armed carter in a leather apron, with a whip coiled at his belt and a straw between his teeth. His cart is piled with barrels and his horse, a patient bay mare, looks at the world with great weariness.";
        Level = 1;
        MaxVitality = 20;

        AddChat("Mind the wheel, friend. She bites.");
        AddChat("Twenty years I have hauled from this quay to the market, and the market has never once been satisfied.");
        AddTopic("cart,load,cartage,work", "Barrels, mostly. Salt fish up to the market, grain down from the farms. It is honest work, and my back has opinions about it.");
        AddTopic("road,roads,west,westwood,caravan,trouble", "I will not take a load west past the second milestone. Not for double rate. The Riverwatch has stopped riding it, and where the Riverwatch will not go, a sensible carter does not either.");
        AddTopic("river,mere,quay,barges", "Down at the quay, east of here. Barges come in from upriver, and I haul what they bring. If you want to see the river, go and look. I only ever see its cargo.");
        AddEmote("Hob Marlow slaps the flank of his mare and shifts a barrel an inch to the left.", 6);
        AddEmote("Hob Marlow spits out his straw and picks a fresh one from his pocket.", 4);
    }
}
