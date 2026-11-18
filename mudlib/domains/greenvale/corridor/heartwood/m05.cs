// @inherit /std/room

public class HwM05 : Room
{
    public override void Create()
    {
        ShortDesc = "The Mossgate Ruins";
        LongDesc = "On the east side of the road a long wall of dark stone runs back among the trunks, three times the height of a man at its highest and broken into steps at its lower end. Where it meets the road it is pierced by a gateway whose arch has gone, and moss hangs from the empty lintel-stones like a curtain. The stone is the same green-grey as the Fallen Arch, and it has been cut with the same unhurried exactness. Beyond the gateway you can see only paving and the grey stems of saplings. The road itself runs on past, as if it had never been invited in.";
        Exits["south"] = "/domains/greenvale/corridor/heartwood/m04";
        Exits["north"] = "/domains/greenvale/corridor/heartwood/m06";

        AddDetail("wall,walls,masonry,stone,stones,course,courses", "The wall is built of long blocks laid in courses of unequal height, without mortar, and the joints are barely wider than a hair. Each course leans a very little inward as it rises. No mason you know of builds so, and the wall has not moved in all the time the roots have been working on it.");
        AddDetail("gateway,gate,mossgate,opening,doorway,lintel,lintels", "The gateway is narrow, and tall for its width, with a flat lintel of three stones laid side by side. The moss on the lintel hangs in long beards. Through the opening you see the open court beyond, though the way in from here is blocked by deadfall and thorn; the usual path runs round by the east trail, farther south.");
        AddDetail("moss,beards,curtain,hanging moss", "The moss that hangs from the lintel is pale green, wet and long as hair. When the air moves, it sways all together, like something breathing.");
        AddDetail("paving,court,saplings,sapling,stems", "Past the gateway the ground is flagged with the same stone, and the flags are lifted and tilted by roots. Saplings of silver-grey bark and no more than a hand across have rooted in the joints and stand in rows, as if planted.");
        AddDetail("steps,end,lower end,broken wall", "At its southern end the wall has come down in a stair of broken courses. A fox or a badger has made a way through the rubble, and the stone there is polished by small bodies.");
        AddDetail("road,ground,dirt", "The road does not turn toward the gateway or away from it. The dirt runs by, a few paces from the foot of the wall, and the moss grows right to its edge on both sides.");
        AddSpawn("/domains/greenvale/corridor/heartwood/npc/marcella");
    }
}
