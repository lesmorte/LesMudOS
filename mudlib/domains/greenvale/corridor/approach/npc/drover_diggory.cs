// @inherit /std/npc

public class OaDroverDiggory : Npc
{
    public override void Create()
    {
        Name = "Diggory Marsh";
        Ids.Add("diggory");
        Ids.Add("marsh");
        Ids.Add("diggory marsh");
        Ids.Add("drover");
        Ids.Add("oxherd");
        ShortDesc = "Diggory Marsh, a drover";
        LongDesc = "A lanky young man with a goad over his shoulder, straw in his hair and a sleepy ox's patience in his expression. His boots are cracked past mending, and his hands are enormous.";
        Level = 2;
        MaxVitality = 25;

        AddChat("Gee up, Duke. Not you, miss, I'm talking to the ox.");
        AddChat("Eight oxen, two hands, one road. That's a drover's life.");
        AddChat("Take my advice: never stand behind a ox, never stand in front of an ox, and never argue with the one in the middle.");
        AddTopic("oxen,ox,duke,team,beasts", "Eight to a wagon. Duke is the lead, and he knows the way better than I do. He'd go to the city and back on his own, if I let him.");
        AddTopic("road,road south,south,heartwood", "The Heartwood is the worst. Dark, and the oxen hate it. They don't like the smell. They know when something is watching.");
        AddTopic("bandits,bandit,aqueduct,camp", "I don't go west of the paving, miss. Nor does Duke. The ox has more sense than most drovers.");
        AddTopic("wagon master,baldric,master,wages,pay", "Baldric pays on time and shouts on time. A decent master.");
        AddTopic("city,oakenfell,gate", "I've seen the city three times and it frightens me every time. So many people, and they all know where they are going.");
        AddEmote("Diggory Marsh scratches an ox behind the ear and murmurs to it.", 8);
        AddEmote("The young drover yawns until his jaw cracks.", 6);
    }
}
