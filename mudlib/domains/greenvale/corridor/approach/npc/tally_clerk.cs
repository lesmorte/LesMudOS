// @inherit /std/npc

public class OaTallyClerk : Npc
{
    public override void Create()
    {
        Name = "Philippa Reeve";
        Ids.Add("philippa");
        Ids.Add("reeve");
        Ids.Add("philippa reeve");
        Ids.Add("clerk");
        Ids.Add("tally clerk");
        Ids.Add("consortium clerk");
        Ids.Add("tally");
        ShortDesc = "Philippa Reeve, a Consortium tally clerk";
        LongDesc = "A neat young woman in a blue coat with a silver scale badge, carrying a slate, a satchel of tally-sticks and a cash box chained to her wrist. She counts wagons as she walks, lips moving, and her chalk never stops.";
        Level = 3;
        MaxHp = 30;

        AddChat("Axles, bales, weight. Axles, bales, weight. Do not talk to me while I am counting.");
        AddChat("Everything that goes south passes my slate. If it is not on my slate, it was not carried.");
        AddChat("Trade Builds Tomorrow. Tomorrow, preferably with a receipt.");
        AddTopic("tally,slate,counting,count,tallies", "Each wagon is counted out and counted in. A wagon that goes south with forty bales and returns with thirty-nine is a conversation I do not enjoy having.");
        AddTopic("consortium,merchant consortium,scales,badge", "The Merchant Consortium keeps the books for most of Greenvale's trade. I am only a clerk, but a clerk is where the books begin.");
        AddTopic("cash,cash box,chain,money,payment", "The master's pay, the toll money, the hire for the oxen, all in the box. I would rather not talk about the box in public, if you do not mind.");
        AddTopic("bandits,bandit,robbers,road,danger", "Yes, I have heard the stories. I carry the cash south on the Rise road every month and I have never once been stopped. That seems a very good reason to be frightened.");
        AddTopic("board,tally board,rows,departures", "Row one, row two, row three. It is chalked and re-chalked every hour. Read the board and you will know what is going where.");
        AddEmote("Philippa Reeve ticks a mark on her slate and scowls at a barrel.", 8);
        AddEmote("The tally clerk taps her chalk against her teeth, counting under her breath.", 6);
    }
}
