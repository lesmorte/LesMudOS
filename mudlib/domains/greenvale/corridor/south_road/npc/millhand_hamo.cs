// @inherit /std/npc

public class SrMillhandHamo : Npc
{
    public override void Create()
    {
        Name = "Hamo";
        Ids.Add("hamo");
        Ids.Add("millhand");
        Ids.Add("mill hand");
        Ids.Add("journeyman");
        Ids.Add("miller's man");
        Ids.Add("man");
        ShortDesc = "Hamo, the mill hand";
        LongDesc = "A young man white with flour, his hair a dusty cap and his eyelashes powdered like a baker's. His hands are quick on the brake rope, and he watches the sails the way a sailor watches the sea.";
        Level = 2;
        MaxHp = 30;

        AddChat("Careful by the sails. They cross the path every four breaths, and I've no wish to haul you down.");
        AddChat("There's the whole road from up here, all the way to the trees. I like it. I like the trees from here.");
        AddChat("Old Leofric says I have the knack. He says it in the voice other men use for 'He has the plague'.");
        AddTopic("mill,sails,wind,milling", "She's a post-mill: the whole body turns on the post to face the wind. Pull the tail pole, the sails catch, and the stones turn. It sounds simple. It is not.");
        AddTopic("view,road,north,wood,forest,trees", "From here I can see the bridge, and the dark of the Eldergreen beyond. There are lights in it some nights, tiny ones, moving. Rangers, Leofric says. I say nothing.");
        AddTopic("west,thorn,slope,covert,fox,wolf", "The west slope is bad ground: bramble, burrows, a wolf now. The old sheepfold at the top of the ridge is worse. Someone's been up there, because I've seen a fire in the dusk, no bigger than a lantern.");
        AddTopic("fire,smoke,lantern,lookout,sheepfold", "Yes, a fire. A little thing, shielded. Not a shepherd's: Bram sleeps in his own hut. Whoever it is keeps watch on the road, and I'd rather it were not me who asked why.");
        AddTopic("flour,corn,grain", "Fourteen sacks on a good day, more with a gale. I eat bread for every meal and still can't stand the smell of yeast.");
        AddEmote("Hamo hauls on the brake rope and the great sails slow.", 8);
        AddEmote("Hamo knocks flour from his hair and sneezes.", 6);
    }
}
