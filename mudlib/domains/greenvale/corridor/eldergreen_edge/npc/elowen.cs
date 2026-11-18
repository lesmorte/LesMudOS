// @inherit /std/npc

public class EeElowen : Npc
{
    public override void Create()
    {
        Name = "Elowen";
        Ids.Add("elowen");
        Ids.Add("hunter");
        Ids.Add("archer");
        Ids.Add("huntress");
        ShortDesc = "Elowen, a hunter";
        LongDesc = "A wiry woman in a doeskin tunic with a short bow, quick grey eyes and dark hair bound in a leather thong. A faded scar runs along her jaw, and she sits very still, as if listening for something far away.";
        Level = 5;
        MaxHp = 60;

        AddChat("Hush. I almost had that one.");
        AddChat("Everything in the wood is hunting something. The trick is to hunt the hunter.");
        AddChat("Morwenna would have laughed at that. She laughed at everything.");
        AddTopic("hunting,hunter,game,deer", "I go for deer mostly. The roe on the lawn are quick, and hard to stalk. You have to wait until they stop watching.");
        AddTopic("lawn,deer lawn,roe", "Roe deer, in the clearing south of the thicket. Skittish. They'll flee unless you strike first. We don't hunt there more than we must; they're the wood's, not ours.");
        AddTopic("spiders,spider,thicket,bite", "The thicket is a trap. I walked in once for a hare. I came out with a leg that wouldn't hold me for a week. Take a torch, and a friend.");
        AddTopic("goblin,goblins,scouts,cords", "Small. Quick. Grey. They don't fight if they can leave. They move at night, and they stop at the big oak. I've watched them. They walk round it, left hand to the tree.");
        AddTopic("morwenna,friend,missing", "My friend. We came up here together. She wanted to track the goblins to where they went, and she found something, and she went on alone. I should have gone with her.");
        AddTopic("hide,great tree,beech,high hide", "Morwenna had a hide, somewhere in the deep wood, up a tree. She never told me where. Said it was safer. If you find it, you'll find her notes.");
        AddTopic("oak,marked oak,markings", "Somebody swept the ground around that oak, and I don't think it was a goblin. Somebody tends it. I'd as soon not meet them.");
        AddTopic("camp,hunters camp", "Stay as long as you like. Share the fire. Don't touch Morwenna's blanket.");
        AddEmote("Elowen runs a thumb along her bowstring and listens.", 6);
        AddEmote("Elowen stares eastward into the trees, unblinking.", 6);
        AddEmote("Elowen scrapes a deerskin with long strokes.", 6);
    }
}
