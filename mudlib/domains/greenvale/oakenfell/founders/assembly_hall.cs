// @inherit /std/room

public class FdAssemblyHall : Room
{
    public override void Create()
    {
        ShortDesc = "Assembly Hall, the Public Gallery";
        LongDesc = "The public gallery is a narrow, paneled room of dark oak with three tiers of worn benches facing a carved screen. Through the lattice of the screen can be glimpsed a horseshoe of benches in the chamber beyond, and a pair of tall inner doors stand shut beside it, though they do not quite muffle the voices behind them. Deep green banners hang from the beams, each stitched with an oak and a few words in gold thread. A steward sits at a small desk by the street door with a lamp, a ledger and the look of a man who has heard every possible argument and found most of them short.";
        Exits["out"] = "/domains/greenvale/oakenfell/founders/square";

        AddDetail("gallery,benches,tiers,bench,seats", "Three tiers of benches, the wood polished dark by generations of observers who came to watch the government at work and found it, as a rule, a long afternoon. A few seats have cushions; the cushions bear initials.");
        AddDetail("screen,lattice,carved screen,chamber", "A tall screen of carved oak, pierced with a lattice of leaves and acorns. Through it you can make out a horseshoe of benches in the chamber beyond, a raised chair at the open end, and a great many delegates in good coats, all talking at once.");
        AddDetail("doors,inner doors,tall doors,chamber doors", "Tall inner doors of black oak, shut, with brass handles worn bright. No sign says that you may not open them; the steward's eye says it well enough. Behind them the arguing goes on.");
        AddDetail("voices,delegates,arguing,argument,noise", "From the chamber beyond come the voices of delegates in the middle of a very long disagreement about roads, tolls, grain and a boundary stone. It goes on, with brief pauses for breath, in the manner of weather.");
        AddDetail("banners,banner,green banners,cloth", "Deep green banners hang from the beams, each stitched with an oak of seven boughs and, beneath it, a line of gold thread. You could {cmd}read{/} it.");
        AddReadable("banners,banner,green banners,motto,words,thread,gold thread", "Stitched in gold beneath the oak on each banner:\n\"PROSPERITY THROUGH UNITY\"", "Deep green banners with a gold-stitched line beneath an oak. You could {cmd}read{/} it.");
        AddDetail("desk,steward's desk,ledger,lamp", "A small desk of dark wood, with a green-shaded lamp, a ledger kept in tidy columns, and a stack of slips that bear the stamp of the Assembly. The steward's hand never rests far from the stack.");
        AddReadable("notice,notice of the gallery,rules,rules of the gallery", "A card is pinned to the desk:\n\"The public gallery is open to observers. Observers will not speak, applaud or pass notes to delegates. Business before the Assembly is by petition, and petitions are received at City Hall.\"", "A neat card is pinned to the desk. You could {cmd}read{/} it.");

        AddSpawn("/domains/greenvale/oakenfell/founders/npc/steward_orlan");
    }
}
