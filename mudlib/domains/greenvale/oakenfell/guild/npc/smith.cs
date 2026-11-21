// @inherit /std/npc

public class GqSmith : Npc
{
    public override void Create()
    {
        Name = "Brenna";
        Ids.Add("brenna");
        Ids.Add("smith");
        Ids.Add("blacksmith");
        Ids.Add("woman");
        ShortDesc = "Brenna, a master smith";
        LongDesc = "A broad-shouldered woman with forearms like hams, a scorched leather apron and a face red from the forge. She holds a hammer as lightly as a pen, and has the cheerful bluntness of someone whose work speaks louder than she does.";
        Level = 15;
        MaxVitality = 100;

        AddChat("Stand back from the sparks, friend. They do not care who you are.");
        AddChat("Iron is honest. You do right by it and it does right by you. Do wrong and it tells you at once.");
        AddChat("The guilds are not enrolling at present. But you are welcome to watch. Many a good smith started by watching.");
        AddTopic("smith,smithing,forge,iron,steel,metal", "A good smith knows three things: when to heat, when to strike, and when to stop. Most of the craft is stopping at the right moment.");
        AddTopic("guild,guilds,join,enroll,enrolling,apprentice", "The guilds are not taking new members just now. The clerk by the guild house will tell you the same. When they do open, there will be a notice.");
        AddTopic("weapons,armor,sword,swords,blades", "The great smiths in the Market sell what they sell. Here we make and mend for the guild's own. Come back later and perhaps there will be more to say.");
        AddTopic("hall,hall of legends", "They like our work. I will say that much. The beams in that hall hold a great many of our nails.");
        AddEmote("Brenna lifts a glowing bar from the forge and strikes it three times in quick rhythm.", 10);
        AddEmote("Brenna plunges a blade into the trough in a cloud of hissing steam.", 8);
    }
}
