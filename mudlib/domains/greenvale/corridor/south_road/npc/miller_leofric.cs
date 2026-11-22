// @inherit /std/npc

public class SrMillerLeofric : Npc
{
    public override void Create()
    {
        Name = "Old Leofric";
        Ids.Add("leofric");
        Ids.Add("miller");
        Ids.Add("old miller");
        Ids.Add("old leofric");
        Ids.Add("old man");
        ShortDesc = "Old Leofric, the miller";
        LongDesc = "A bent old man dusted white from cap to boots, with forearms like knotted rope and eyes the pale blue of skimmed milk. He has ground this district's grain for sixty years, and he still cocks his head at the sails every time the wind changes.";
        Level = 2;
        MaxVitality = 30;

        AddChat("Mind the dog. She's not vicious, but she's thorough. Welcome to the mill farm, traveler.");
        AddChat("Sixty years I've ground corn for this vale, and I never yet saw a wind I could not read.");
        AddChat("If you're going north, buy bread at the Rest first. There's no bakery in the Eldergreen.");
        AddTopic("mill,windmill,flour", "My grandfather raised the first post of that mill. The sails have been replaced six times, the tail pole nine, and the hill not once. Hamo keeps her turning now; I just tell him what he's done wrong.");
        AddTopic("farm,orchard,bees", "The orchard was my wife's, and the bees too. They know me; they will know you if you do not swat.");
        AddTopic("wolf,wolves,fox,foxes", "There's a wolf come down from the wood this summer, a lean grey stray. Bram lost a lamb. Keep away from the thorn on the western slope, it's a den, I'd swear it.");
        AddTopic("north,road,wood,forest,eldergreen", "Past the bridge it's another country. I went to Oakenfell once, in my youth, for a wedding. I came home in three days and have not left the vale since.");
        AddTopic("hale,hale's acre,well", "Hale's Acre? Gone before my time. Their well failed, the story goes, and they went to the Hollow and never came back. The thorn took the place.");
        AddTopic("thorn,hawthorn,ring", "The hawthorn ring in the brook pasture? It was there before my father. We do not cut it. We do not ask. The cattle keep their own counsel, and the milk is no worse for it.");
        AddEmote("Old Leofric tilts his head to the sails and grunts with approval.", 8);
        AddEmote("Leofric brushes flour from his sleeves and raises a white cloud.", 6);
    }
}
