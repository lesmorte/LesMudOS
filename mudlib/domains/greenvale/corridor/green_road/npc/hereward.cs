// @inherit /std/npc

public class GrHereward : Npc
{
    public override void Create()
    {
        Name = "Hereward";
        Ids.Add("hereward");
        Ids.Add("captain");
        Ids.Add("guard captain");
        Ids.Add("caravan guard");
        Ids.Add("guard");
        ShortDesc = "Hereward, the caravan guard captain";
        LongDesc = "A broad, grey-bearded man in a boiled-leather coat, with a short spear planted butt-down beside him and a notched shield on his back. He does not sit, and his eyes go to the tree line every few breaths.";
        Level = 5;
        MaxHp = 70;

        AddChat("Three on watch, three asleep. That is how we do it, and that is how the wagons get to the city.");
        AddChat("Stay inside the ring after dark. The wood is theirs once the sun goes.");
        AddChat("Ever vigilant, the Rangers say. I say, ever sober, and a spear in reach.");
        AddTopic("watch,guards,guard,night", "Four posts around the ring, changed on the hour. Nobody sleeps through a watch of mine, not twice.");
        AddTopic("goblins,raiders,goblin,raider", "Goblin raiders work the Raiders' Ford and the deep wood to the west. They like a straggler, a lame ox, a wagon that has fallen behind. We keep close, and they leave us be.");
        AddTopic("rangers,riverwatch,patrol,tower,watchtower", "The Rangers walk the road when they can. They kept a post on the knoll past the Leaning Stones once, but it is empty now, and I have heard every kind of reason why. I give none of them credit.");
        AddTopic("bandits,rogues,lookout", "Bandits have a lookout somewhere in the deep wood, so the drovers say. They watch the road for a wagon with a thin guard. Mine is not thin.");
        AddTopic("work,job,hire,escort", "I take no new hands in the middle of a run. If you are going north, walk with us at dawn and keep your blade sheathed until I say.");
        AddTopic("crossroads,heartwood,north", "Three miles on, the Crossroads Stone. We go straight on for the Heartwood and the city. I have never taken the Old Way, and do not mean to.");
        AddEmote("Hereward walks the ring of wagons, thumping each wheel with the butt of his spear.", 6);
        AddEmote("Hereward stares into the trees for a long moment, then nods to a drover and moves on.", 6);
    }
}
