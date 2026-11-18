// @inherit /std/room

public class RvMereBridge : Room
{
    public override void Create()
    {
        ShortDesc = "Mere Bridge";
        LongDesc = "Mere Bridge is a broad stone bridge of five low arches that carries the road over the River Mere, its parapets worn smooth and wide enough to sit on. Below, the slow brown water slides past the piers, carrying a barge, a drifting branch and a great many gulls. Anglers have claimed the parapet, with baskets, rods and an air of settled disagreement. To the west, the market's noise rolls out of the North Rows; to the east, the towers of Eastgate rise above the road, and a stair at the north end drops to the quay below.";
        Exits["west"] = "/domains/greenvale/oakenfell/market/north_rows";
        Exits["east"] = "/domains/greenvale/oakenfell/river/eastgate";
        Exits["north"] = "/domains/greenvale/oakenfell/river/quay";

        AddDetail("bridge,arches,arch,stone,stones,piers,pier", "Five low arches of grey stone, their piers shaped like the prows of ships to part the current. The bridge is older than most of the buildings on either bank, and the stones at the middle of the roadway are dished by the wheels of centuries.");
        AddDetail("river,mere,river mere,water,current,barge", "The River Mere slides past below, slow and brown and wide. It comes from the south and bends away to the north, past the quay, with barges moving on it as quietly as clouds.");
        AddDetail("parapet,parapets,wall,edge", "Waist-high parapets of smoothed grey stone, wide enough for a man to sit on, and so many have that the tops are polished like glass. Names and initials are cut here and there, with dates.");
        AddDetail("anglers,angler,baskets,rods,lines,fish", "A row of anglers sits along the parapet with long rods and short tempers. Their baskets are mostly empty, but everyone insists the fish are about to bite.");
        AddDetail("stair,stairs,steps,quay,north", "A flight of worn stone steps drops at the bridge's northern end to the quay below, where the barges tie up. You can go {cmd}north{/}.");
        AddDetail("eastgate,towers,east,gate", "The two stubby towers of Eastgate stand above the roofs to the east, with the green banner on a staff between them.");
        AddDetail("market,west,north rows,noise", "To the west the noise of the market rolls out of the North Rows: a fishwife's shriek, a hammer, a barrel organ with one broken note.");

        AddSpawn("/domains/greenvale/oakenfell/river/npc/wat_ellery");
        AddSpawn("/domains/greenvale/oakenfell/river/npc/agnes_pell");
    }
}
