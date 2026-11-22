// @inherit /std/npc

public class GqGuildClerk : Npc
{
    public override void Create()
    {
        Name = "Osric";
        Ids.Add("osric");
        Ids.Add("clerk");
        Ids.Add("guild clerk");
        Ids.Add("man");
        ShortDesc = "Osric, clerk of the Joined Guilds";
        LongDesc = "A harried young man with a ledger under each arm, an inkpot at his belt and an air of mild apology. He walks quickly and stops suddenly, as though he has just remembered something he was supposed to do.";
        Level = 6;
        MaxVitality = 35;

        AddChat("Hello. Welcome to Crafters' Court. I am Osric, clerk to the Joined Guilds. If you are here to enroll, I am afraid the guilds are not enrolling at present. I am so sorry.");
        AddChat("The notice board will say when they open. I check it every morning myself. I would very much like to tell you more.");
        AddChat("Watch the forges, by all means. Visitors are welcome. Just keep your hands out of the quench trough.");
        AddTopic("enroll,enrolling,enrollment,join,joining,apply,application,guild,guilds", "The guilds are not enrolling at present. It is not that we do not want new members. We do. It is only that the arrangements are not yet ready. Check the notice board, please. I promise it will say as soon as it can.");
        AddTopic("crafting,craft,crafts,smith,weaver,trades", "The Joined Guilds of Craft share this yard: the smiths, the weavers, and the allied trades. Crafting for travelers is not yet open, but you are welcome to look.");
        AddTopic("hall,hall of legends,hallmasters", "The Hall of Legends certifies the guilds, you see. We are very grateful to them. And a little afraid.");
        AddTopic("ledger,ledgers,books,records", "Members, masters, apprentices, dues. It is all here. It is very orderly. Please do not touch it.");
        AddEmote("Osric flips through a ledger, frowns, and flips back.", 9);
        AddEmote("Osric glances at the notice board and sighs.", 6);
    }
}
