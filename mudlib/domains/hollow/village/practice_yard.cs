// @inherit /std/room

/// <summary>The Practice Yard: a fenced yard outside the north wall, with archery butts and a training dummy that cannot be killed. Brann Odell keeps it. The Warrior's and Adventurer's rank trainer belongs here later.</summary>
public class VhPracticeYard : Room
{
    public override void Create()
    {
        ShortDesc = "The Practice Yard";
        LongDesc = "A long, fenced yard of trodden earth lies in the angle between the village palisade and the north road. At the far end three round butts of straw stand against a bank, stuck full of arrows and ringed in faded red. Nearer, a training dummy stands on a stout post in the middle of a worn circle of ground, and a rack of blunted weapons leans against the fence beside a water barrel and a rough bench. A gate in the fence opens onto the north road, and a postern door in the palisade leads back into Fletcher's Lane. A small board is nailed to the gatepost.";
        Exits["lane"] = "/domains/hollow/village/fletchers_lane";
        Exits["road"] = "/domains/hollow/village/north_road";
        Safe = false;

        AddDetail("yard,fence,wall,palisade,postern,gate", "A tall plank fence on two sides and the palisade on the third, with a gate to the road and a postern door through the palisade. The earth is trodden to a pale, hard floor, and the one patch of grass at the far end is only still there because nobody can reach it without being shot.");
        AddDetail("butts,butt,straw,straw butts,targets,rings,bank,arrows", "Three round butts of tight-bound straw stand against an earthen bank, painted with rings that were red once. A good many arrows are stuck in them, and a few are stuck in the bank behind, and one is stuck in a gatepost, high up, for reasons nobody will give.");
        AddDetail("circle,ground,worn circle,post", "A circle of ground trodden bare around the dummy's post. The grass never grew back, and the mud when it rains is a shape that you could almost call a footprint.");
        AddDetail("rack,weapons,blunted weapons,blunts", "A rack of weapons whose edges have been ground off, and whose points have been rounded for safety. They are for borrowing, but not for taking out of the yard, and they are all very slightly bent.");
        AddDetail("barrel,water barrel,bench", "A water barrel with a tin dipper chained to it, and a rough bench polished by the backsides of tired archers. Somebody has carved a rude word in it, and somebody else has tried to sand it off.");
        AddReadable("board,small board,gatepost,notice,rules", "The board reads:\n  \"PRACTICE YARD. All are welcome.\n  1. The dummy does not hit back. Do not let that make you proud.\n  2. Turn {cmd}verbose{/} on and every blow shows what it did.\n  3. Nothing here gives experience, loot or a name in the Chronicle. It is only practice.\n  4. Do not shoot the gatepost. -- B. Odell, Yard Master\"", "A small board is nailed to the gatepost. You could {cmd}read{/} it.");

        AddSpawn("/domains/hollow/mob/practice_dummy");
        AddSpawn("/domains/hollow/npc/brann_odell");
    }
}
