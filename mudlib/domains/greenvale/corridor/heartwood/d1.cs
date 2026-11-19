// @inherit /std/room

public class HwD1 : Room
{
    public override void Create()
    {
        ShortDesc = "The Ancient Wolf's Dell";
        LongDesc = "The ground falls away into a round dell, steep-sided and ringed with ferns, with a floor of flattened bracken and old bone. The bed in the middle is the size of a cart, pressed into the earth by a body that has lain there year after year. Grey hair is snagged on every thorn along the rim, coarse as horsehair and as long as your forearm. The air is heavy and warm and smells of wet fur, and the only sound is a slow, deep breathing from somewhere close.";
        Exits["west"] = "/domains/greenvale/corridor/heartwood/w3";
        Exits["north"] = "/domains/greenvale/corridor/heartwood/d2";
        DangerBand(9, 10);

        AddDetail("dell,hollow,bowl,rim,slope,sides", "The dell is nearly round, twenty paces across and the depth of two men, with sides too steep for a deer to climb comfortably. It was not dug, but it has been kept clear, and the thorn has been allowed to grow in a ring on the rim.");
        AddDetail("bed,lair,den,bracken,flattened", "The bed is a hollow worn into the floor and lined with bracken crushed flat and dry. It is much too large for a wolf as you know wolves. In the middle, a long shape has pressed itself into the earth, and the earth has not recovered.");
        AddDetail("bones,bone,skulls,skull,remains", "The bones are mostly deer, with a good many boar, and the skull of a bear with the jaw taken. A few of them are very old, brown as walnut and cracked from lying in the sun. Among them is a rusted spearhead.");
        AddDetail("hair,fur,thorn,thorns,grey hair", "The hair on the thorns is grey, coarse and in tufts, and some are as long as a man's arm. It has the smell of an animal that has lived for a very long time.");
        AddDetail("prints,tracks,pawprints,paw", "A paw print in the damp earth at the edge of the bed spreads wider than your open hand, and the claws leave dents the length of your finger. The prints go round the dell, and then out to the north, and back in the morning.");
        AddDetail("breath,breathing,sound,air", "Beneath the rim the breathing is slow and rough, with long gaps in it. It does not stop when you look for it, and it does not hurry.");
        AddSpawn("/domains/greenvale/corridor/mob/ancient_wolf", 1);
    }
}
