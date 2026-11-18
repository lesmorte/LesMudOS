// @inherit /std/npc

public class GqWeaver : Npc
{
    public override void Create()
    {
        Name = "Tamsin";
        Ids.Add("tamsin");
        Ids.Add("weaver");
        Ids.Add("old woman");
        Ids.Add("woman");
        ShortDesc = "Tamsin, an old weaver";
        LongDesc = "A tiny, white-haired woman with a loom as tall as she is and fingers stained blue to the second knuckle. Her hands never stop moving, even when she is looking at you, and her eyes are sharp.";
        Level = 8;
        MaxHp = 40;

        AddChat("Hmm? Oh, you are not an apprentice. I thought you were the new one. They are all so tall these days.");
        AddChat("Eighty years I have been weaving, and I still find a thread I did not expect.");
        AddChat("The guilds are not enrolling just now, dear. Come back when the board says otherwise.");
        AddTopic("weaving,weave,loom,cloth,wool,thread,shuttle", "Warp and weft, dear. One holds, one wanders. Between them you have cloth.");
        AddTopic("dye,dyes,colour,color,colours,colors,blue,indigo", "Indigo is the stubborn one. It stains your hands for a month and the cloth for a century.");
        AddTopic("guild,guilds,join,enroll,enrolling,apprentice", "Not just now, dear. The guild clerk will tell you when. I should not rush it. A good thing is worth waiting for.");
        AddTopic("oak,great oak,old", "Oh, my grandmother used to weave a green cloth with a branch worked into the hem, for the founding festival. They do not make them any more. Perhaps they should.");
        AddEmote("Tamsin sends her shuttle flying from hand to hand without looking up.", 10);
        AddEmote("Tamsin hums a tune that seems to match the rhythm of the loom.", 8);
    }
}
