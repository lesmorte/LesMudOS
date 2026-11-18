// @inherit /std/npc

public class SgRegularDessa : Npc
{
    public override void Create()
    {
        Name = "Dessa Rook";
        Ids.Add("dessa");
        Ids.Add("dessa rook");
        Ids.Add("regular");
        Ids.Add("caravan guard");
        Ids.Add("guard");
        ShortDesc = "Dessa Rook, a former caravan guard";
        LongDesc = "A wiry woman with a sunburnt face, a missing front tooth and a cloak patched in six colours. She sits with her back to the wall and her boots on the bench, watching the door.";
        Level = 3;
        MaxHp = 30;

        AddChat("Don't take the western road, not without a good reason and a better escort. Wagons have been turned back near the Elderwood.");
        AddChat("I guarded caravans for eleven years. The road teaches you who's lying.");
        AddChat("The Free Companies take jobs the Consortium won't. The Consortium takes the Free Companies' jobs when it's cheaper.");
        AddTopic("western road,westwood road,elderwood,west", "Logging camps burned, trade wagons turned over, signs nailed to the trees that I could not read. The Consortium blames the Thorn Pact. The Pact says it's nothing to do with them. Somebody's lying, and the roads are the price.");
        AddTopic("factions,faction,politics", "The Assembly keeps the peace, the Consortium keeps the books and the Hall of Legends keeps the heroes. The Riverwatch guard the roads, the Free Companies sell their blades, and the Thorn Pact pretends it isn't here. Everyone claims to be keeping the country safe.");
        AddTopic("thorn pact,pact,druids", "Officially they're not in the city. Unofficially, ask the herbalist in the north rows. They don't love the Consortium, and the feeling is mutual.");
        AddTopic("caravans,caravan,consortium", "The Consortium runs the caravans from the Caravan Office on Coin Street. Good pay if you can swing a sword and keep your mouth shut.");
        AddEmote("Dessa Rook checks the door, then the window, then the door again.", 8);
        AddEmote("Dessa Rook tips her tankard to a passing sellsword.", 6);
    }
}
