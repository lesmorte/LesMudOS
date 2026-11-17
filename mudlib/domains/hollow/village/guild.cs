// @inherit /std/room

/// <summary>
/// The Adventurers Guild: the Quest Board is a readable here; Guildmaster
/// Rowan (in /domains/hollow/npc/rowan) gives out and checks the quests.
/// </summary>
public class AdventurersGuild : Room
{
    public override void Create()
    {
        ShortDesc = "Adventurers Guild";
        LongDesc = "Monster trophies, old banners, and battered shields decorate the walls of this busy guild hall. Adventurers gather around tables discussing rumors, contracts, and treasure maps. A large notice board dominates one wall.";
        Exits["east"] = "/domains/hollow/village/square";
        Safe = true;

        AddDetail("trophies,monster trophies", "Horns, tusks, a wolf's pelt and the skull of something with far too many teeth. Each has a little brass label with a name and a date.");
        AddDetail("banners,old banners", "Faded banners of companies long disbanded. Several are scorched, and one has a hole the size of a fist.");
        AddDetail("shields,battered shields", "Dented shields hung up in thanks by those who survived behind them.");
        AddDetail("tables,table,adventurers", "Adventurers lean over maps and mugs, trading rumors in low voices. Most of the talk is about the cellar below the Hollowman's Rest.");
        AddReadable("board,quest board,notice board,notices",
            "The Quest Board\n" +
            "  1. CLEAR THE CELLAR. Goblins have made a stronghold in the Forgotten Cellars beneath the Hollowman's Rest, and something is organizing them. Defeat the Goblin Overseer. Reward: experience, gold and a Guild Initiate's Helm. ({cmd}accept cellar{/})\n" +
            "  2. RECOVER THE OVERSEER'S ORDERS. The goblins take their orders from somewhere. Bring the Overseer's Orders to the Guildmaster. Reward: experience and gold. ({cmd}accept orders{/})\n" +
            "Speak to Guildmaster Rowan to {cmd}accept{/} a quest, and {cmd}report{/} to him when it is done.",
            "A large notice board covered in pinned parchments. The two biggest are quest notices. You could {cmd}read{/} it.");

        AddSpawn("/domains/hollow/npc/rowan");
    }

    /// <summary>The board shows how the reader is getting on.</summary>
    public override string? QueryReadable(string name)
    {
        var text = base.QueryReadable(name);
        var reader = ThisPlayer();
        if (text == null || reader == null || !name.ToLowerInvariant().Contains("board") && !name.ToLowerInvariant().Contains("notice")) return text;
        var cellar = reader.Call<int>("QueryQuest", "clear-the-cellar");
        var orders = reader.Call<int>("QueryQuest", "overseers-orders");
        return text + "\nYour contracts: " +
               (cellar == 2 ? "Clear the Cellar (done)" : cellar == 1 ? "Clear the Cellar (active)" : "Clear the Cellar (open)") + "; " +
               (orders == 2 ? "Recover the Overseer's Orders (done)" : orders == 1 ? "Recover the Overseer's Orders (active)" : "Recover the Overseer's Orders (open)") + ".";
    }
}
