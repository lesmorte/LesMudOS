// @inherit /std/npc

public class SgRecruiterSera : Npc
{
    public override void Create()
    {
        Name = "Sera Voss";
        Ids.Add("sera");
        Ids.Add("recruiter");
        Ids.Add("captain");
        Ids.Add("sera voss");
        Ids.Add("free companies");
        ShortDesc = "Sera Voss, Free Companies recruiter";
        LongDesc = "A tall, rangy woman of forty with a braid of dark hair and a nose broken more than once. She wears a red-and-black sash over a scarred leather jerkin, and her smile is friendly, practical and entirely professional.";
        Level = 5;
        MaxHp = 60;

        AddChat("Coin Before Crown. That's the Free Companies' word, and we live by it.");
        AddChat("Looking for work, blade, or a company to march with? You've come to the right bench.");
        AddChat("We don't ask where you're from. We ask what you can do.");
        AddTopic("free companies,companies,company", "We're sellswords, bounty hunters and adventurers who sell our skills where we choose. Some of us are noble, some are bandits with better manners. Loyalty's bought with coin, not politics.");
        AddTopic("coin before crown,motto", "It means we serve the contract, not the throne. The Assembly says that makes us dangerous, and the Consortium's regulators say it makes us a problem. We say it makes us honest.");
        AddTopic("assembly,consortium,lawkeepers", "The Assembly's lawkeepers don't love us, and the Consortium's regulators like us less. We take the jobs they won't, and we pay for the privilege.");
        AddTopic("work,jobs,contract,contracts,hire", "There are contracts on the board by the door, but I'd not give them to you yet. Make a name first, then we'll talk pay.");
        AddEmote("Sera Voss runs a thumb along the edge of her knife, assessing a newcomer.", 8);
        AddEmote("Sera Voss makes a note in the lodge ledger.", 6);
    }
}
