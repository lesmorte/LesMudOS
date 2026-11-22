// @inherit /std/npc

public class FdClerkMaude : Npc
{
    public override void Create()
    {
        Name = "Maude Tarrow";
        Ids.Add("maude");
        Ids.Add("tarrow");
        Ids.Add("clerk");
        Ids.Add("clerk of districts");
        ShortDesc = "Maude Tarrow, a clerk";
        LongDesc = "A broad, cheerful woman with grey-streaked hair in a net and a pointer of polished ash in her hand. She has answered the question 'Where do I go?' so many times that she has begun to enjoy it.";
        Level = 1;
        MaxVitality = 15;

        AddChat("New to Oakenfell? Ask me about the districts, and I will tell you where to go and where not to bother.");
        AddChat("There are five districts. Everyone forgets one. It is usually the Founder's.");
        AddTopic("districts,five,five districts,city,oakenfell", "Five districts, and each one has its own character. The Founder's District, where you stand, is the oldest. The Market lies south of it, the River to the east, the Guild Quarter to the west, and Southgate at the very bottom. Ask me about any of them by name.");
        AddTopic("founder,founders,founder's,city hall,assembly", "The Founder's District is the original settlement: this hall, the Assembly Hall, Founder's Square, and the old stone streets going north to Northgate. It is quiet and respectable, and very proud of both.");
        AddTopic("market,coin street,bank,consortium,auction,caravan", "South of the square. Stalls and shops all along the Greenvale Market, and Coin Street running west with the bank, the Consortium House, the auction house and the caravan office. Westgate is at the end of it. Busy, noisy, and mostly honest.");
        AddTopic("river,mere,quay,docks,fish,riverwatch", "East of the square, along Old River Street. Warehouses, the quay on the River Mere, the fish market, the shipwright, the trade office, and the Riverwatch Rangers' city office. Practical people, and they do not care for dawdling.");
        AddTopic("guild,guild quarter,hall of legends,athenaeum,crafters", "West of the square, along Guild Row. The Hall of Legends is there, and the Athenaeum, Crafters' Court and the Proving Yard. If you hope to be somebody, that is the place to start.");
        AddTopic("southgate,inns,inn,stables,lodge,silver stag", "South, past the market and all the way down, near the gate to the Vale Road. The Silver Stag Inn, the stables, the Free Lodge and the Outfitter. Rough around the edges and very welcoming.");
        AddEmote("Maude Tarrow taps a district on the painted map with her pointer and nods to herself.", 6);
        AddEmote("Maude Tarrow points a lost-looking traveler toward the door and wishes them luck.", 5);
    }
}
