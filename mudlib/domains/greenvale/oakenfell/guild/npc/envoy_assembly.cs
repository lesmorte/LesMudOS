// @inherit /std/npc

public class GqEnvoyAssembly : Npc
{
    public override void Create()
    {
        Name = "Marisol";
        Ids.Add("marisol");
        Ids.Add("envoy");
        Ids.Add("assembly envoy");
        Ids.Add("envoy of the assembly");
        Ids.Add("woman");
        ShortDesc = "Marisol, Envoy of the Greenvale Assembly";
        LongDesc = "A composed, silver-haired woman in a plum-coloured gown with the Assembly's gathered hands worked in thread at the collar. She sits very straight at her desk, a stack of papers aligned to its edge, and smiles in a way that suggests she has listened to many arguments and settled most of them.";
        Level = 20;
        MaxHp = 80;

        AddChat("Welcome, traveler. I am Marisol, envoy of the Greenvale Assembly. Our motto is Prosperity Through Unity. Ask me anything about the Assembly, or about any of Greenvale's powers.");
        AddChat("The Assembly governs most of the settled lands of Greenvale. We were formed by village leaders and merchants who wanted to protect each other, and we have not found a better reason since.");
        AddChat("Peace, trade routes, prosperity and the rule of law: that is the whole of our ambition. It is a modest list, and very hard to do.");
        AddTopic("assembly,greenvale assembly,government,regional government", "The Greenvale Assembly: a regional government, with its seat in the Assembly Hall in Oakenfell, in Founder's Square. Our motto is Prosperity Through Unity. We want to maintain peace, protect trade routes, expand prosperity, prevent regional conflicts and preserve law and order. Our friends are the Merchant Consortium, the Hall of Legends and the Riverwatch Rangers. Our rivals are the Thorn Pact and the Free Companies. Most citizens think well of us. Some say we have grown comfortable and bureaucratic, and I shall not pretend I have never heard it.");
        AddTopic("motto,prosperity,unity", "Prosperity Through Unity. A simple thing, which is why it is hard.");
        AddTopic("goals,want,wants,aims,seek,purpose", "Peace, protected trade routes, prosperity for all, the prevention of regional conflicts, and law and order. Nothing glamorous. Everything essential.");
        AddTopic("allies,friends,rivals,enemies,opponents", "Our allies are the Merchant Consortium, the Hall of Legends and the Riverwatch Rangers. Our rivals are the Thorn Pact and the Free Companies.");
        AddTopic("circle,circle of ash", "I am afraid I do not know that name. Perhaps you are thinking of something else. Shall we speak of the Rangers instead?");
        AddTopic("consortium,merchant,merchants,merchant consortium", "The Merchant Consortium: our close friends, and our largest ratepayers. Their coin keeps the roads and the harbors in order. I will admit that their voice carries rather far in our debates. We do not always agree, but we always sit down together.");
        AddTopic("rangers,riverwatch,watchfort,ranger", "The Riverwatch Rangers: the finest frontier order in Greenvale. They keep the roads and the borders so that the rest of us need not. Their motto is Ever Vigilant, and it is not an exaggeration. They are firm friends of the Assembly.");
        AddTopic("hall,hall of legends,hallmasters,guild confederation", "The Hall of Legends is the confederation of guilds, and a friend of the Assembly. Officially it is neutral, and I have never known it to be anything but diplomatic. We respect its independence, and rely on its training.");
        AddTopic("thorn,pact,thorn pact,thorns", "The Thorn Pact: troublesome, to put it mildly. They say Greenvale is growing too fast, and I will grant that many of them mean well. But some have taken to sabotaging logging camps and trade roads, and the Assembly cannot allow that. They call themselves necessary. We call them a problem.");
        AddTopic("free companies,free company,companies,mercenaries,sellswords,coin before crown", "The Free Companies are sellswords and bounty hunters. Some are noble. Others, I regret to say, are little better than bandits. Their loyalty is bought, which makes them a nuisance to the lawkeepers of the Assembly.");
        AddTopic("standing,reputation,favor,favour,rank", "The Hall's ledgers of standing are not yet open, I am afraid. When they are, you will be able to learn where you stand. Until then, I can only say that the Assembly is always glad of friends.");
        AddEmote("Marisol squares a sheaf of papers against the edge of her desk.", 8);
        AddEmote("Marisol gives a visitor a gracious, attentive smile.", 6);
    }
}
