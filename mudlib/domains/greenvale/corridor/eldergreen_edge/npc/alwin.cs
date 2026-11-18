// @inherit /std/npc

public class EeAlwin : Npc
{
    public override void Create()
    {
        Name = "Alwin";
        Ids.Add("alwin");
        Ids.Add("forester");
        Ids.Add("woodsman");
        ShortDesc = "Alwin the forester";
        LongDesc = "A weathered man in a leather jerkin and a green wool cap, with a felling axe on his shoulder and a pot of white paint hanging at his belt. His beard is grey, his forearms are scarred with old branch-whip, and he looks at trees, not at you, when he talks.";
        Level = 4;
        MaxHp = 50;

        AddChat("Mind the blazes. A white mark means the tree is spoken for, and I'll know if you've touched it.");
        AddChat("I've walked this wood thirty years. It's never twice the same, and it's never done with surprising me.");
        AddChat("Fell a tree, plant two. That's the old rule, and I keep it, whatever the woodcutters say.");
        AddTopic("forester,job,work,lodge,cabin", "I mark the trees that may be felled and the trees that may not, and I keep the woodcutters honest. It's less dull than it sounds. The oaks along this road are the best timber in Greenvale, and the Assembly would have them all in a year if I let them.");
        AddTopic("woodcutters,woodcutter,ride,finnian", "Finnian works the ride east of here, and does it well. He cuts what I blaze and not an inch more. His father did the same, and his grandfather.");
        AddTopic("oaks,oak,trees,wood,forest,eldergreen", "This is the Eldergreen Wood, and it begins about here. The farms end, the oaks begin, and the light goes green. Further north it gets thicker, and darker, and older. I go as far as the Narrows, and not past the Outpost.");
        AddTopic("blaze,blazes,paint,white paint", "White for a tree to keep. Rangers use a shield and an eye, so you'll know theirs. Anything else, a red mark, a notch, a ring cut round the trunk, that's not mine, and I want to hear about it.");
        AddTopic("marks,markings,rings,carvings,oak marked,marked oak,thorn", "Odd marks? There's a great oak deep east of the trail, so I've heard, cut all round with markings I don't know. Neither the Rangers nor I put them there. I go round, and I don't ask. Some things in a wood are best left standing.");
        AddTopic("waystone,stone,broken", "The waystone north of here? Struck by lightning before my time. Somebody scratched a line on it later, so old I can't read it. I leave it be.");
        AddTopic("wolves,wolf,boar,spiders,goblins,danger", "Wolves along the Fern Banks, boar east of the ride, spiders in the thicket. Goblins I don't see, but I find their cords. Stay on the road and you'll mostly be fine. Mostly.");
        AddTopic("rangers,riverwatch,outpost", "Good people, the Rangers. Their Outpost's north, past the Charcoal Clearing and the Narrows. If you're looking for work or a safe place to rest, they're the ones.");
        AddEmote("Alwin looks up into the nearest oak and nods slowly to himself.", 6);
        AddEmote("Alwin tests the edge of his axe with his thumb and frowns.", 6);
        AddEmote("Alwin dabs a fresh blaze of white on the trunk by the door.", 6);
    }
}
