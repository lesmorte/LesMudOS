// @inherit /std/npc

public class OaTollKeeper : Npc
{
    public override void Create()
    {
        Name = "Humphrey Cobb";
        Ids.Add("humphrey");
        Ids.Add("cobb");
        Ids.Add("humphrey cobb");
        Ids.Add("toll keeper");
        Ids.Add("keeper");
        Ids.Add("clerk");
        Ids.Add("toll");
        ShortDesc = "Humphrey Cobb, the toll keeper";
        LongDesc = "A stooped, ink-fingered man of sixty in a green Assembly coat a size too large, with a pen behind each ear and a third in his hand. Brass spectacles slide down his nose as he counts wheels, and he seems to be doing sums even when he is speaking to you.";
        Level = 3;
        MaxHp = 30;

        AddChat("Next! Axles, please, not names. Names come later and are less reliable.");
        AddChat("The toll is for wagons, friend. If you walk or ride, you owe the Assembly nothing but good manners.");
        AddChat("Travel pass? The Greenvale Travel Pass? Then pass, pass, you are expected. Go on through.");
        AddTopic("toll,tolls,fee,pay,price", "Wagons pay, by the axle and the load, and the board by the window shows what. Herds pay by the ten head. Walkers and riders pay nothing.");
        AddTopic("pass,travel pass,greenvale travel pass,permit", "Adventurers who carry the Greenvale Travel Pass are waved straight through, and I am not even to look at their packs. If you carry one, you may go on; if you do not, and you are walking, you may go on anyway. The pass was for the Northgate, not for me.");
        AddTopic("assembly,greenvale assembly,banner", "The Greenvale Assembly keeps the road, and the toll keeps the road in repair. Prosperity through unity, they say, and I say that unity is a great deal easier with a decent bridge.");
        AddTopic("bandits,bandit,highwaymen,robbers,danger", "Every wagon master from here to the Hollow has a story. I send a note to the Rangers each time, and the Rangers send a note back that says they are looking into it. Do not leave the paving, friend, that is all I will say.");
        AddTopic("aqueduct,west,track,ruin", "That track? It goes to the old arches. There is nothing there, and nothing there is exactly what I would say about it to a stranger.");
        AddTopic("oakenfell,city,gate", "Five miles on, and you will see it in all its glory before you are halfway. Mind your purse in the last mile. The carters will not rob you, but their prices will.");
        AddEmote("Humphrey Cobb licks his thumb and turns a page in the great ledger.", 8);
        AddEmote("The toll keeper heaves on the rope and the green-and-white pole swings up for a wagon.", 8);
    }
}
