// @inherit /std/npc

public class GvTobiahRue : Npc
{
    public override void Create()
    {
        Name = "Tobiah Rue";
        Ids.Add("tobiah");
        Ids.Add("rue");
        Ids.Add("caretaker");
        Ids.Add("old man");
        Ids.Add("gardener");
        ShortDesc = "Tobiah Rue, the caretaker";
        LongDesc = "A stooped old man in a coat the color of moss, a rake in one hand and a sack at his hip. He moves slowly and quietly, as if afraid of waking something, and his eyes, when they meet yours, are mild and entirely unsurprised.";
        Level = 2;
        MaxVitality = 30;

        AddChat("Mm.");
        AddChat("You found it. They mostly do, in the end.");
        AddChat("Take your time. It has all the time there is.");
        AddTopic("legends,legend,stories,rumors,rumours,tales,say,people say", "Four things folk say about it. That the roots still live. That spirits gather here. That druids can hear the tree whisper. That there are ways beneath the stump. I have heard all four from people who should know better. I could not tell you more than that.");
        AddTopic("roots,root,alive,live,living", "They say the roots still live. That the oak did not die, only went underground. The wood is stone, I will say that. As for the rest, they say it. I rake the leaves.");
        AddTopic("spirits,spirit,ghosts,ghost,gather,gathering", "They say spirits gather here, at dusk, to be near the old tree. Folk bring them bread and ribbons for it. I sweep up in the morning, the same as every morning. That is a thing people say.");
        AddTopic("druids,druid,whisper,whispers,whispering,hear,voice", "They say a druid can stand at the stump and hear the tree whisper. I have never been a druid. I have stood here a long time, and I have heard the wind.");
        AddTopic("passages,passage,beneath,under,underground,tunnels,hidden,secret,secrets", "They say there are hidden passages beneath the stump. Under the roots, down into the dark. I have swept this ground for thirty years and I have never seen a door. That is not the same as saying there is none.");
        AddTopic("offerings,offering,bread,ribbons,stone", "People leave what they have. Bread, ribbons, a coin, a toy. I clear the old ones away and I do not ask. It seems to matter to them.");
        AddTopic("door,garden door,iron door,guild quarter,rusted door", "That door has not opened in my time, nor in my master's before me. They say it gives onto the Guild Quarter. I say it is a door, and it is shut.");
        AddTopic("stump,great stump,oak,great oak,tree", "The Great Oak, or what the fire left. Go and look. It will tell you more than I can.");
        AddTopic("caretaker,you,yourself,who are you,grove,hidden grove", "I keep the ground. A few of us have, always, and some will after. We do not talk about it much. There is not much to say.");
        AddEmote("Tobiah Rue draws his rake slowly across the path, gathering a few fallen leaves into a neat heap.", 6);
        AddEmote("Tobiah Rue straightens a ribbon on the flat stone, then stands still for a moment as if listening.", 5);
    }
}
