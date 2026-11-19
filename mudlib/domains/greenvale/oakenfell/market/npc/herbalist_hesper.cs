// @inherit /std/shopkeeper

public class MkHerbalistHesper : Shopkeeper
{
    public override void Create()
    {
        Name = "Hesper Yarrow";
        Ids.Add("hesper");
        Ids.Add("hesper yarrow");
        Ids.Add("herbalist");
        Ids.Add("herbwife");
        Ids.Add("healer");
        ShortDesc = "Hesper Yarrow, an herbalist";
        LongDesc = "A quiet, weathered woman with grey-green eyes and a long plait, in a green shawl pinned with an oak-leaf brooch. Her fingers are stained with sap, and her stall smells of rosemary, thyme and bitter things.";
        Level = 2;
        MaxHp = 20;

        AddChat("Yarrow for wounds, comfrey for bones, willow-bark for fever. Honest herbs, honestly gathered.");
        AddChat("Mind where you step, dear. Some of those baskets are nettles, and some are worse.");
        AddChat("The wild gives, if you ask politely.");
        AddTopic("herbs,herb,yarrow,comfrey,willow,remedy,remedies", "Yarrow, comfrey, willow-bark, feverfew. Simple things, but they work if the hand that gathers them is gentle. I sell to the apothecaries mostly, and a few grateful regulars. For a traveler, healing herb, wildflower and thistle; {cmd}browse{/}, if you like. I will not pretend my prices are the village's.");
        AddTopic("potion,potions,healing", "I don't make potions. I know the herbs that go in them. The Outfitter sells vials for the road.");
        AddTopic("forest,elderwood,woods,trees", "The Elderwood is old. Older than the city, older than the Hall. It remembers more than we do, and I think it is wise to remember with it.");
        AddTopic("logging,loggers,timber,logging camps", "They cut and cut, and call it progress. I'd say ask the loggers what the forest took in return. I would not say more.");
        AddTopic("thorn pact,pact", "I know nothing of any pact. I know the forest remembers. That is all I will say here, in the middle of the market.");
        AddStock("/domains/hollow/obj/craft/healing_herb", 4);
        AddStock("/domains/hollow/obj/craft/wildflower", 3);
        AddStock("/domains/hollow/obj/craft/thistle", 2);
        AddEmote("Hesper Yarrow ties a bundle of herbs with a green thread and hangs it from the awning.", 8);
        AddEmote("Hesper Yarrow glances at the Market Cross, then away.", 6);
    }
}
