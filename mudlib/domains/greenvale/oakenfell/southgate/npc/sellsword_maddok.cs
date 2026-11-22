// @inherit /std/npc

public class SgSellswordMaddok : Npc
{
    public override void Create()
    {
        Name = "Maddok";
        Ids.Add("maddok");
        Ids.Add("sellsword");
        Ids.Add("mercenary");
        ShortDesc = "Maddok, an off-duty sellsword";
        LongDesc = "A broad, grey-bearded man with a patch over one eye. He sits by the hearth with a whetstone and a dagger, working the edge in slow, patient strokes.";
        Level = 4;
        MaxVitality = 40;

        AddChat("Mm.");
        AddChat("Don't take every job. Take the ones that pay.");
        AddChat("Sit by the fire. Keep your hands where I can see them. It's a joke. Mostly.");
        AddTopic("eye,patch", "A wolf. Don't ask which wolf.");
        AddTopic("dagger,whetstone,blade", "A dull blade kills its owner. Keep it sharp.");
        AddTopic("companies,free companies", "Good company, mostly. Better than a king's army, if you like to be paid.");
        AddEmote("Maddok runs the whetstone along his dagger with a long, soft rasp.", 10);
        AddEmote("Maddok looks up, nods once, and goes back to his blade.", 6);
    }
}
