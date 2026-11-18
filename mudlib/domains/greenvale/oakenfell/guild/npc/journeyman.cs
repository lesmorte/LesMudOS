// @inherit /std/npc

public class GqJourneyman : Npc
{
    public override void Create()
    {
        Name = "Ansel";
        Ids.Add("journeyman");
        Ids.Add("mason");
        Ids.Add("ansel");
        Ids.Add("journeyman mason");
        ShortDesc = "Ansel the journeyman mason";
        LongDesc = "A broad young man in a dusty leather apron, a mason's hammer swinging at his belt and a rolled drawing under one arm. He walks like someone who has just been promoted and has not got used to it.";
        Level = 5;
        MaxHp = 40;

        AddChat("Mind your feet, friend. The Row is all apprentices before noon and all journeymen after, and neither of us looks down.");
        AddChat("Four years I carried stone for Master Dunmere. Last spring he handed me a hammer of my own, and now I carry stone for money.");
        AddChat("Going to the Hall? Take the north road and keep walking. Everyone stops at the doors to stare, and the doorwarden will not mind.");
        AddTopic("hall,hall of legends,doors", "Some of the oldest beams in the Hall were here before the Row was paved. My master says the founders built it in pieces, and each generation added a wing without ever pulling down the last. I have spent three winters looking for a seam and never found one.");
        AddTopic("guild,guilds,journeyman,apprentice", "Apprentice, then journeyman, then master, and a masterpiece to prove it. The guilds hold the road clear for us, and we hold up the guilds' name. That is the whole bargain.");
        AddTopic("masons,mason,stone", "Honest work. Oakenfell is built on the stuff, and half of it still needs mending.");
        AddTopic("oak,great oak", "Every child here is told the story. I could not tell you one stone of this city that does not remember it.");
        AddEmote("Ansel unrolls his drawing, frowns at it, and rolls it up again.", 8);
        AddEmote("Ansel taps the wall beside him, listening to the sound the stone makes.", 6);
    }
}
