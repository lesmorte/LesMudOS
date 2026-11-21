// @inherit /std/npc

public class SrFishermanCuthbert : Npc
{
    public override void Create()
    {
        Name = "Cuthbert";
        Ids.Add("cuthbert");
        Ids.Add("fisherman");
        Ids.Add("angler");
        Ids.Add("man");
        Ids.Add("old man");
        ShortDesc = "Cuthbert, a fisherman";
        LongDesc = "A thin, patient man in a battered felt hat, sitting cross-legged on the parapet with a long rod and a basket of silver-sided fish at his feet. He watches the float with an expression of utter peace.";
        Level = 2;
        MaxVitality = 30;

        AddChat("Shh. Not so loud. He's down there, the big one. I can feel him.");
        AddChat("Forty years I've fished this bridge. Forty years, and the big pike still hasn't taken the bait. He is a gentleman.");
        AddChat("Sit, if you like. Just don't talk to the fish.");
        AddTopic("fish,fishing,pike,trout,river", "Trout in the shallows, roach in the weed, and the pike under the middle arch, ancient and clever. I have never caught him. I have never wanted to.");
        AddTopic("bridge,arches,stones,mason", "Older than the Assembly, they say. The mason carved his mark in the keystone of the middle arch, underwater, where only the fish can read it.");
        AddTopic("north,wood,forest,eldergreen,trees", "Past the bridge it's a different world. The river bends into the forest and the water goes brown. I've followed it half a mile, no more. You hear things.");
        AddTopic("ranger,mildred,riverwatch,patrol", "Mildred stands the bridge most days. She never talks, she never smiles, but she brought me tea in the rain once. I'd die for her. Don't tell her.");
        AddTopic("wolves,wolf,danger", "They come to drink at dusk, up the river. I leave before then. A man who fishes after dark is a man who plans to be a fish.");
        AddEmote("Cuthbert twitches the line and sighs, disappointed.", 8);
        AddEmote("Cuthbert adjusts his hat and settles back against the parapet.", 6);
    }
}
