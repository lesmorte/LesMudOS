// @inherit /std/npc

public class SgSellswordJory : Npc
{
    public override void Create()
    {
        Name = "Jory Ketch";
        Ids.Add("jory");
        Ids.Add("sellsword");
        Ids.Add("jory ketch");
        Ids.Add("mercenary");
        ShortDesc = "Jory Ketch, an off-duty sellsword";
        LongDesc = "A stocky, sandy-haired man with a crooked grin and a shirt open at the neck. His sword lies across the table beside a tankard, and his knuckles are scarred.";
        Level = 4;
        MaxHp = 40;

        AddChat("Sit down, stranger. The first story's free. The second costs a drink.");
        AddChat("I fought at the Watchfort bridge once. Or someone did. I was there for the pay.");
        AddChat("Never turn down a free meal, a good boot, or a clean sword.");
        AddTopic("fight,fights,battle,sword", "A sword's only as good as the hand and the day. I've seen rich fools with fine steel die like peasants, and farm lads with rusty iron live.");
        AddTopic("work,pay,contract", "Pay's decent, work's dirty. That's the Free Companies. Talk to Sera about the ledger.");
        AddTopic("west,western road,elderwood", "Heard the western road is bad. Wagons burned, loggers vanished. Nobody pays enough for that, but somebody always does.");
        AddTopic("lodge,companies", "The Free Lodge is home, church and bank. They keep your coin, your name and your secrets, in that order.");
        AddEmote("Jory Ketch flips a coin across his knuckles and catches it behind his back.", 8);
        AddEmote("Jory Ketch stretches and cracks his neck.", 6);
    }
}
