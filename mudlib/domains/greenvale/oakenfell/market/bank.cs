// @inherit /std/room

public class MkBank : Room
{
    public override void Create()
    {
        ShortDesc = "The Oakenfell Bank";
        LongDesc = "The bank's hall is cool, quiet and echoing, floored in black and white marble and lit by tall windows of greenish glass. A long counter of dark wood with brass grilles divides the public floor from the clerks' area, and behind it, a great round door of iron stands ajar on a corridor of strongrooms. Two guards in blue stand stiffly by the door. Everything here, from the ledgers to the clerks' sleeve guards, is neat, silent and watchful, and no one raises their voice.";
        Exits["south"] = "/domains/greenvale/oakenfell/market/coin_street";
        ResetSeconds = 120;

        AddDetail("counter,grilles,brass grilles", "The counter is dark oak polished by a hundred years of elbows. The brass grilles above it are cut in a pattern of acorns and scales.");
        AddDetail("vault,vault door,round door,strongrooms", "The great vault door is a disc of iron two feet thick, hinged like a gate, and today it is ajar for the clerks. Beyond it, a corridor lined with iron-banded doors leads out of sight.");
        AddDetail("guards,guard,livery", "Two guards in blue livery, with halberds and good boots, stand with the practiced stillness of those paid to do nothing and do it well.");
        AddDetail("floor,marble,tiles", "Black-and-white marble, so polished that you can see the roof beams in it. Your boots sound very loud.");
        AddDetail("clerks,ledgers,books", "Clerks in sleeve guards scribble in huge ledgers with a quiet, persistent scratching. A few glance up at you, and glance down again.");
        AddDetail("windows,glass", "Tall windows of greenish glass in lead frames, which makes the light seem underwater.");
        AddReadable("notice,sign,brass plaque,plaque", "THE OAKENFELL BANK\nFounded in the Consortium's second century.\nAccounts and vaults for the citizens of Oakenfell.\nNew customers are received when the books are opened.\nTrade Builds Tomorrow.", "A brass plaque on the wall carries a notice. You could {cmd}read{/} it.");

        AddSpawn("/domains/greenvale/oakenfell/market/npc/banker_aldous");
    }
}
