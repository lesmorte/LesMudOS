// @inherit /std/room

public class EeRiverwatchOutpost : Room
{
    public override void Create()
    {
        ShortDesc = "The Riverwatch Outpost";
        LongDesc = "A stout palisade of oak stakes encloses a trampled yard on the east side of the road, its gate standing open and a green pennant with a pale shield stirring on a tall pole above. Inside are a log-built bunkhouse, a lean-to stable, a rack of bows and a watch platform from which a ranger scans the road in both directions. The yard smells of horses, boot-grease, woodsmoke and stew, and the noise of the forest seems to stop at the stakes. A board of notices stands beside the gate where every traveler must pass it.";
        Exits["south"] = "/domains/greenvale/corridor/eldergreen_edge/m08";
        Exits["north"] = "/domains/greenvale/corridor/eldergreen_edge/m10";
        Safe = true;
        ResetSeconds = 120;

        AddDetail("palisade,stakes,wall,gate,yard", "The palisade is a hedge of sharpened oak stakes taller than a mounted man, lashed with iron bands and weathered grey. The gate is open by day, and a ranger stands near it, nodding to every traveler.");
        AddDetail("pennant,pole,flag,banner,shield,pale shield", "A green pennant bearing a pale shield flies from a tall pole. It is the same device that hangs in the Rangers' office in Oakenfell, and beneath it, painted on the pole in white, are the words \"Ever Vigilant\".");
        AddDetail("bunkhouse,bunks,barracks,building", "The bunkhouse is a long low building of logs with a stone chimney and a row of shuttered windows. Through the open door come the smells of stew and wet wool.");
        AddDetail("stable,lean-to,horses,horse", "Five rangers' horses stand in the lean-to, mud to the hocks and stamping at flies. They are small, hairy, sensible animals bred for the forest.");
        AddDetail("bows,bow,rack,arrows,longbows", "A rack of longbows stands by the bunkhouse door with a barrel of arrows beside it, fletched in green and white. None of them is for sale.");
        AddDetail("platform,watch platform,tower,lookout", "A watch platform of timber and rope stands above the palisade, and a ranger on it scans the road both ways, turning on her heels at intervals like a clock.");
        AddDetail("hearth,fire,cookfire,stew,pot", "A cookfire burns in a ring of stones in the yard, and a black pot hangs over it with something savoury in it. Travelers are not turned away from it.");
        AddReadable("board,notice board,notices,bounties,bounty board,bounty", "The board is crowded with notices pinned in layers. The freshest, in a clear hand, read:\n\nRIVERWATCH RANGERS, EDGE POST. BOUNTIES.\n\nFOREST WOLVES. Packs hunting the Fern Banks and the Narrows, and bolder than they were. Proof of a kill to be shown to the quartermaster.\nWILD BOAR. Sows with young in the deep wood, and big tuskers wallowing east of the road. Dangerous when cornered. Cull them before they come out to the farms.\nGOBLIN SCOUTS. Seen on the old trail beyond the Birch Stand, in twos and threes. Do not seek them in numbers less than three. Report any sighting to the commander.\nFOREST SPIDERS. The Spider Thicket grows. Burn webs, bring word.\n\nThe Rangers will acknowledge all work done in the road's service. See the quartermaster.\n\nA smaller notice, pinned at the corner, says only: \"Any word of Morwenna, hunter, last at the Hunter's Camp in the autumn, to the commander.\"", "A board of notices stands by the gate. You could {cmd}read{/} it.");
        AddReadable("roster,duty roster,patrol roster,rota", "The patrol roster, nailed to the bunkhouse door, reads:\n\n\"Edge Post patrol: out from the Outpost along the Patrol Mile to the Green Gate and back; then south by the Narrows, the Charcoal Clearing and the Fern Banks as far as the Broken Waystone, and back. Two rangers at a time. Report anything moving on the road that should not be.\"", "A duty roster is nailed to the bunkhouse door. You could {cmd}read{/} it.");

        AddSpawn("/domains/greenvale/corridor/eldergreen_edge/npc/gwenllian");
        AddSpawn("/domains/greenvale/corridor/eldergreen_edge/npc/hywel");
        AddSpawn("/domains/greenvale/corridor/eldergreen_edge/npc/idris");
        AddSpawn("/domains/greenvale/corridor/eldergreen_edge/npc/tegan");
    }
}
