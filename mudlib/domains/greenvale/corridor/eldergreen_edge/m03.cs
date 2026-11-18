// @inherit /std/room

public class EeForestersCabin : Room
{
    public override void Create()
    {
        ShortDesc = "The Forester's Cabin";
        LongDesc = "A squat cabin of squared oak logs stands back from the west side of the road under a roof of green moss, with a woodpile stacked higher than a man along one wall and a thread of smoke rising from the chimney. White paint has been daubed in blazes on every trunk near the door, and a rack of long-handled tools hangs under the eaves. To the east a wide ride has been cut into the trees, its floor deep in chips and sawdust. The air smells of sap, woodsmoke and fresh-split oak.";
        Exits["south"] = "/domains/greenvale/corridor/eldergreen_edge/m02";
        Exits["north"] = "/domains/greenvale/corridor/eldergreen_edge/m04";
        Exits["east"] = "/domains/greenvale/corridor/eldergreen_edge/w1";
        ResetSeconds = 120;

        AddDetail("cabin,lodge,logs,roof,moss", "The cabin is built of squared oak logs fitted so closely that no chinking shows. Moss has taken the roof, and a small window with horn panes looks out on the road.");
        AddDetail("woodpile,pile,firewood,wood", "Split billets are stacked in ranks as neat as a mason's work, bark-side up against the rain. The oldest ranks are grey, and the newest still weep sap.");
        AddDetail("blazes,blaze,paint,white paint,marks", "A daub of white paint on a trunk marks a tree the forester has judged and left to stand. Trees without a blaze, you gather, are fair game for the woodcutters.");
        AddDetail("tools,rack,axes,axe,billhook,saw,eaves", "Felling axes, a billhook, a two-man saw and a long-handled hook for dragging brush hang in a row under the eaves, each with its edge oiled and its handle worn pale.");
        AddDetail("chimney,smoke", "The smoke rises thin and straight and smells of oak. Someone is home, or has not been gone long.");
        AddDetail("ride,track,east,sawdust,chips", "The ride east is wide enough for a hauling sled, floored with sawdust and bark. Fresh drag marks run along it, and somewhere along it an axe is working.");
        AddReadable("sign,notice,board", "Nailed to the door post on a plank:\n\"FORESTER'S LODGE. Blazed trees are not to be cut, barked, or tapped. No fires past this point in dry weather. Charcoal burners east of the Narrows by leave only. Report any tree marked other than with white paint to the forester.\"", "A painted plank is nailed to the door post. You could {cmd}read{/} it.");

        AddSpawn("/domains/greenvale/corridor/eldergreen_edge/npc/alwin");
    }
}
