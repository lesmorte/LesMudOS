/// <summary>
/// The words that send a new character to the Adventurers Guild (docs/specs/play-feel.md, section 15): the line after the first look at the Village
/// Square, the hint under "You have no quests.", the line at the top of the tavern's cellar stair and the "help start" page. They are written here,
/// once. The way to the guild is read from the Village Square's real exits, so a builder who moves the guild door changes the words too.
/// Pure text: it reads and changes nothing, so a wizard's sandbox room may use it. Blueprint only.
/// </summary>
public class Guidepost : MudObject
{
    private const string Square = "/domains/hollow/village/square";
    private const string Guild = "/domains/hollow/village/guild";

    /// <summary>The exit of the Village Square that leads to the guild ("west"), or "" if there is none.</summary>
    private string Way()
    {
        var square = LoadObject(Square);
        foreach (var direction in square.Call<List<string>>("QueryExits"))
            if (square.Call<string?>("QueryExitPath", direction) == Guild) return direction;
        return "";
    }

    /// <summary>"the Adventurers Guild, {cmd}west{/} of the Village Square", or just the name of the place if no exit leads there.</summary>
    public string GuildPlace()
    {
        var way = Way();
        return way == "" ? "the Adventurers Guild" : $"the Adventurers Guild ({{cmd}}{way}{{/}} from the Village Square)";
    }

    /// <summary>A new character's first lesson, told under the first room description.</summary>
    public string Welcome()
        => $"Your first stop is {GuildPlace()}. Guildmaster Rowan gives out the work that pays in experience and gold: read the board there, then {{cmd}}accept{{/}} a contract. {{cmd}}help start{{/}} tells you more.\n";

    /// <summary>Under "You have no quests." for a character who has never taken one.</summary>
    public string Hint()
        => $"Contracts are given out at {GuildPlace()}: read the board and {{cmd}}accept{{/}} one from Guildmaster Rowan.\n";

    /// <summary>Once, as a character with no quest goes down the tavern's cellar stair.</summary>
    public string Stair()
        => $"The stair goes down toward goblins, and you have no contract. The guild pays well for this very work: {GuildPlace()}. You can still go on.\n";

    /// <summary>"help start".</summary>
    public string Help()
        => $"Where to start: go to {GuildPlace()} and read the quest board ({{cmd}}read board{{/}}). Guildmaster Rowan is there: {{cmd}}accept cellar{{/}} takes the first contract, which sends you down the tavern's cellar to the goblins.\n" +
           $"Before you go, buy kit in the village ({{cmd}}browse{{/}} in a shop), try your weapon on the dummy in the practice yard by the north gate, and place your attribute points ({{cmd}}improve{{/}}).\n" +
           $"Fight one creature at a time and {{cmd}}rest{{/}} between fights. When the work is done, come back to Rowan and {{cmd}}report{{/}}. {{cmd}}quests{{/}} shows where you stand.\n";
}
