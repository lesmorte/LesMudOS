// @inherit /std/npc

public class RvNanHobb : Npc
{
    public override void Create()
    {
        Name = "Agnes Pell";
        Ids.Add("agnes");
        Ids.Add("pell");
        Ids.Add("angler");
        Ids.Add("woman");
        ShortDesc = "Agnes Pell, an angler";
        LongDesc = "A broad, weather-reddened woman sitting astride the parapet with a short rod, a flask and a basket full of silver perch. She looks inordinately pleased with herself.";
        Level = 1;
        MaxHp = 15;

        AddChat("Eleven so far today. Do not tell Wat.");
        AddChat("It is all in the wrist, and the bait, and the flask. Mostly the flask.");
        AddTopic("fish,fishing,perch,bait,secret", "Worms from behind the Fish Market, and patience from my late husband. Neither costs a thing.");
        AddTopic("wat,ellery,old wat", "Wat is a fine man and a wretched angler. Thirty years, and I have never seen him land anything bigger than his hat.");
        AddTopic("river,mere,barges,quay", "Go down the stair at the north end for the quay. You will see the barges, the crane and the harbormaster, who is polite until a barge is left in the wrong place.");
        AddEmote("Agnes Pell hauls in a silver perch, unhooks it with a flick, and drops it in her basket with a satisfied grunt.", 6);
        AddEmote("Agnes Pell takes a long swig from her flask and smacks her lips.", 5);
    }
}
