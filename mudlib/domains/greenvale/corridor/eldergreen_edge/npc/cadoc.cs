// @inherit /std/npc

public class EeCadoc : Npc
{
    public override void Create()
    {
        Name = "Cadoc";
        Ids.Add("cadoc");
        Ids.Add("burner");
        Ids.Add("charcoal burner");
        Ids.Add("brother");
        ShortDesc = "Cadoc, a charcoal burner";
        LongDesc = "A thin, stooped young man sitting on a log with a blanket over his knees, rubbing his eyes. His face is black with soot, and his eyelids are red-rimmed and drooping. A bucket of water stands beside him.";
        Level = 3;
        MaxHp = 40;

        AddChat("Mm. Was I asleep? I was resting my eyes.");
        AddChat("Berenice says I sleep through everything. I don't. I hear the clamps. Sort of.");
        AddChat("Quiet night. Wolves a long way off. Nothing in the clearing.");
        AddTopic("sleep,tired,night,watch", "Someone's got to sit up with the clamps all night, and that's me. You get used to it. I nap in the day. Don't tell Berenice.");
        AddTopic("berenice,sister", "My sister. She's been burning charcoal since she was a girl, and she'll tell you how to do it, whether you ask or not.");
        AddTopic("wolves,wolf,noises,howl", "Wolves at night, yes. They come closer when the moon's full. Last month one sat at the edge of the clearing and watched me for an hour. I didn't sleep after.");
        AddTopic("lights,fires,glow,goblins,cords", "I saw small lights in the trees to the east, three nights back, bobbing low down. Too small for men. I told the Rangers, and they said thanks. Hm. They say thanks a lot.");
        AddTopic("rangers,outpost,patrol", "The Rangers come by twice a day, and bring bread when they can. Good lot. A woman called Tegan keeps their stores, and she's the one to ask about supplies.");
        AddTopic("oak,marked oak,markings", "I've heard of it. Far east, in the deep wood. Berenice won't let me go near. I wouldn't want to.");
        AddEmote("Cadoc yawns hugely and squints at the sun.", 6);
        AddEmote("Cadoc nods off for a moment, jerks awake, and pretends he did not.", 6);
        AddEmote("Cadoc stirs the water in the bucket with a stick.", 6);
    }
}
