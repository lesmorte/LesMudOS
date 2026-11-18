// @inherit /std/npc

public class RvOsricFenn : Npc
{
    public override void Create()
    {
        Name = "Gunnar Holt";
        Ids.Add("gunnar");
        Ids.Add("holt");
        Ids.Add("shipwright");
        Ids.Add("master shipwright");
        ShortDesc = "Gunnar Holt, the shipwright";
        LongDesc = "A burly, sawdust-covered man with arms like tree limbs and an adze in his belt. A pencil lives behind each ear, and his eyes, when he studies the hull on the slip, are the eyes of a man looking at his own child.";
        Level = 3;
        MaxHp = 40;

        AddChat("She'll float, if I have my way. She usually does.");
        AddChat("Oak for the keel, elm for the planks, pitch for the seams. Any fool can build a boat. It takes a good one to build one that stays built.");
        AddTopic("hull,barge,slip,boat,boats,ship,ships", "That's the Dorothea, nine months in the making, and another three to go. A river barge, flat-bottomed, with a bluff bow. She'll carry forty tons and not complain.");
        AddTopic("river,mere,river trade,trade,passage,downriver", "The river trade is all barges, and the barges are all mine, one way or another. The passage question is for the Trade Office. I build the boat; they decide who may ride in it.");
        AddTopic("oak,timber,wood,elm,keel", "Oak is the best wood there is for a keel. Strong, slow to rot, and it never forgets its shape. The old folk say all the good oak in Greenvale is descended from the Great Oak. I say they never worked with it.");
        AddEmote("Gunnar Holt runs a thumb along a plank, grunts approvingly and reaches for his adze.", 6);
        AddEmote("Gunnar Holt sights down the keel with one eye closed and shakes his head at an invisible flaw.", 5);
    }
}
