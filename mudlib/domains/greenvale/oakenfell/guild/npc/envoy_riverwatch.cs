// @inherit /std/npc

public class GqEnvoyRiverwatch : Npc
{
    public override void Create()
    {
        Name = "Sorrel";
        Ids.Add("sorrel");
        Ids.Add("envoy");
        Ids.Add("riverwatch envoy");
        Ids.Add("ranger");
        Ids.Add("envoy of the rangers");
        Ids.Add("woman");
        ShortDesc = "Sorrel, Envoy of the Riverwatch Rangers";
        LongDesc = "A sun-browned, stiff-backed woman in a worn green-and-grey coat, with a ranger's badge on her breast and a map case across her knees. She sits at a desk as though it were a saddle: capable, but not at home. Her eyes keep drifting to the window and the roads beyond.";
        Level = 25;
        MaxHp = 110;

        AddChat("Sorrel, Riverwatch Rangers. We are an office of the Rangers in this city. Ever Vigilant is our motto. Ask what you like. I will answer short.");
        AddChat("The roads are safer than they were. They are not safe. Do not confuse the two.");
        AddChat("Most of the Rangers are out on the frontier, at Watchfort Crossing and beyond. I am the one who writes the letters.");
        AddTopic("rangers,riverwatch,riverwatch rangers,ranger,watchfort", "The Riverwatch Rangers: a military frontier order. Our headquarters is Watchfort Crossing. Our motto is Ever Vigilant. We want to defend Greenvale, secure its roads, map the unexplored lands and eliminate the major threats. Our friends are the Assembly and the Hall of Legends. Our rivals are monsters, raiders and the extremists of the Thorn Pact. Common folk are kind enough to call us heroes. We prefer to call it work.");
        AddTopic("motto,vigilant,ever vigilant", "Ever Vigilant. Not clever. Not glorious. Just awake.");
        AddTopic("goals,want,wants,aims,seek,duty", "Defend Greenvale, secure the roads, map the unmapped, and remove the large threats before they reach the settled land. That is the whole job.");
        AddTopic("allies,friends,rivals,enemies,opponents", "Friends: the Assembly and the Hall of Legends. Rivals: monsters, raiders and the Thorn Pact's extremists. The merchants say we are their friends too, and I do not argue with them.");
        AddTopic("circle,circle of ash", "No. Never heard of it. Ask me about the roads.");
        AddTopic("assembly,greenvale assembly,government", "The Assembly sets the law. The Rangers help keep it where the law cannot walk. We work well together, so far.");
        AddTopic("consortium,merchant,merchants,merchant consortium", "The Consortium pays for a good many wagons on roads we keep clear. They call us friends and we do not complain. Merchants who stay on the road are no trouble to me.");
        AddTopic("hall,hall of legends,hallmasters,guild confederation", "The Hall trains good people, and good people are what the frontier needs. A friend to the Rangers.");
        AddTopic("thorn,pact,thorn pact,thorns", "The Thorn Pact is mostly folk who love the forest, and I have no quarrel with that. The extremists who burn logging camps and sabotage roads are another matter. Those are my rivals, and I mean to see them stopped.");
        AddTopic("free companies,free company,companies,mercenaries,sellswords,coin before crown", "Sellswords. Some are honest soldiers. Some are bandits with a flag. A nuisance on the road either way, and a danger when the coin runs short.");
        AddTopic("standing,reputation,favor,favour,rank", "The Hall's ledgers are not yet open. When they are, they will tell you how the Rangers think of you. Until then, I judge by what I see.");
        AddEmote("Sorrel studies a worn map, then folds it with practiced hands.", 8);
        AddEmote("Sorrel glances at the window as though listening for something on the road.", 6);
    }
}
