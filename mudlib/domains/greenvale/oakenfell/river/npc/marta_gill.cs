// @inherit /std/npc

public class RvMartaGill : Npc
{
    public override void Create()
    {
        Name = "Marta Gill";
        Ids.Add("marta");
        Ids.Add("gill");
        Ids.Add("fishwife");
        Ids.Add("woman");
        ShortDesc = "Marta Gill, a fishwife";
        LongDesc = "A stout woman with a shawl over her head and her sleeves rolled, gutting a heap of perch with a knife that flashes in the lamplight. Her eyes are red-rimmed, and she keeps glancing at the street.";
        Level = 1;
        MaxVitality = 15;

        AddChat("Have you seen a brindled hound? Answers to Biscuit. One white ear.");
        AddChat("He is only a dog, I know. But he is mine.");
        AddTopic("dog,hound,biscuit,lost,reward", "Biscuit went off a week ago, on the bridge, after a gull. I have put up a notice in the square, and offered two gold. If you find him, bring him here. Please.");
        AddTopic("fish,gutting,perch,knife", "A flick here, a flick there, and the perch is ready for the pan. Thirty years, and I could do it blindfolded. I often do, on bad days.");
        AddTopic("market,fishmonger,bessa", "Bessa Crane has the big stall. She is hard, but fair, and she has been very kind about the dog.");
        AddEmote("Marta Gill wipes her eyes with the back of her wrist and picks up the next fish.", 6);
        AddEmote("Marta Gill looks up at the sound of a distant bark, then down again, disappointed.", 5);
    }
}
