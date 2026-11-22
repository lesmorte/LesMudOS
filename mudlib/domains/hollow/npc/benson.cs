// @inherit /std/npc

public class Benson : Npc
{
    public override void Create()
    {
        Name = "Benson";
        Ids.Add("farmer");
        Ids.Add("old farmer");
        Ids.Add("farmer benson");
        Ids.Add("old farmer benson");
        ShortDesc = "Old Farmer Benson";
        LongDesc = "A stooped, weathered man with hands like tree roots and a straw hat older than some of the villagers. He knows every story the Hollow has ever told, and tells most of them twice.";
        Level = 1;
        MaxVitality = 12;

        AddChat("Vincent founded this place. Brave man... but he went lookin' for somethin' beneath the earth and never returned.");
        AddChat("My grandfather helped dig that well. Swore the water tasted of iron the first winter, and never said why.");
        AddChat("Mind the cellar under the Hollowman's Rest. Goblins, they say. In my day we only had rats.");
        AddTopic("vincent,founder,holloway", "Vincent Holloway. Came up from the south with a handful of friends and a wagon of tools. Built the first stockade right where the square is now.");
        AddTopic("well", "Dug in the first winter, when the ground was like iron. They say Vincent himself kept digging after the others gave up.");
        AddTopic("goblins,goblin,cellar", "They came up from below a few years back. Folk say something is organizing them now. I say it's none of my business, and I'm too old to make it so.");
        AddTopic("resolve,stubborn,stubbornness,remembered,remember,digging", "They say Vincent was remembered for his Resolve, and my grandfather said the same, though he put it plainer: the man would not stop. Dug that well when the ground was iron, and kept at it after the others laid down their spades.");
        AddEmote("Benson leans on the well and squints at the northern sky.", 6);
        AddEmote("Benson mutters something about the price of turnips.", 4);
    }
}
