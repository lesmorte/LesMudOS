// @inherit /std/room

public class RwCorner : Room
{
    public override void Create()
    {
        ShortDesc = "The Tunnelers' Corner";
        LongDesc = "A barricade of timber, sharpened stakes and packed clay seals the mouth of this chamber, and every stake points north, the way you came. Behind it, in a space no larger than a cottage kitchen, a handful of goblins have made a last stand into a life, with bedrolls against the walls, a smoky fire in a pit and a pile of picks stacked like spears. The air is thick with smoke and the smell of cooked rat. Burned stumps of root lie heaped by the fire, the leavings of a long and bitter war. On the largest post of the barricade, a tally has been cut.";
        Exits["north"] = "/domains/greenvale/corridor/root_warrens/root_hall";
        ResetSeconds = 300;

        AddDetail("barricade,barricades,wall,stakes,stake,timber,timbers,clay", "The barricade is built of old shoring timber, split and lashed, and every stake is sharpened and fire-hardened. It was built facing into the warren, to keep out what came up from below, and it has been mended so many times that the original work is hard to find. A gap has been left at one end, narrow enough to hold with one spear.");
        AddDetail("notches,three notches,mark,head,post head", "At the head of the tallest post of the barricade, above the tally, three notches are cut one above another and rubbed with red earth. Whatever else these goblins have lost, they have kept their mark.");
        AddReadable("tally,post,posts,marks,scratches,cuts,scoring", "The tally is cut with a knife in the goblin way, in bundles of five, and a few lines of rough writing are scratched beside it:\n\n  'RATS KILLED THIS MOON: more than the fingers of all of us.'\n  'ROOT-CREEPERS BURNED: many. They come back.'\n  'TUNNELERS LEFT: ' and a column of marks that grows shorter, line on line, until the last holds only four.\n\n  'OVERSEER SAID HOLD UNTIL RELIEF. NO RELIEF. WE HOLD ANYWAY.'", "A tally is cut into the largest post of the barricade. You could {cmd}read{/} it.");
        AddDetail("bedrolls,bedroll,hides,hide,furs,fur,skins,skin", "The bedrolls are bundles of old hide and rag, and the hides are good ones: thick and well cured, some stained with soot and some with age. A careful hand could strip them clean for use.");
        AddDetail("picks,pick,tools,tool,spears,rack", "The picks are tunnelers' picks, short and heavy, with the edges worked down to a keen bite. They are leaned together in a wigwam beside the fire, so that any goblin could snatch one up in a moment.");
        AddDetail("fire,pit,smoke,ashes,stumps,stump,cooked", "The fire burns low and smokes sourly, and it is fed on dry root and old timber. A spit of green wood lies across the pit with a thing on it that was once a rat. The heap of burned root-stumps beside it is as high as a table.");
        AddDetail("bones,heap,rats,rat", "Rat bones are heaped against the far wall, a long ridge of tiny skulls and ribs. Many are old and brown, and some are not.");

        AddSpawn("/domains/greenvale/corridor/mob/goblin_tunneler", 2);
    }
}
