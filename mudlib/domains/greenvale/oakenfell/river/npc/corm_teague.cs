// @inherit /std/npc

public class RvCormTeague : Npc
{
    public override void Create()
    {
        Name = "Corm Teague";
        Ids.Add("corm");
        Ids.Add("teague");
        Ids.Add("dockhand");
        Ids.Add("docker");
        ShortDesc = "Corm Teague, a dockhand";
        LongDesc = "A huge, slow-moving man in a leather vest, with a barrel on each shoulder and a rope belt hung with hooks. He has hands the size of shovels and a voice, when he uses it, as soft as a child's.";
        Level = 2;
        MaxVitality = 30;

        AddChat("Mind the planks. They are slippery.");
        AddChat("Barrels, bales, crates. It is all the same weight by evening.");
        AddTopic("work,docks,dock,dockhand,job", "I carry what the barges bring and I carry what the barges take. Twelve hours, and then the tavern. It's a good life, if you like your back.");
        AddTopic("barges,barge,cargo,river", "Salt fish, grain, wool, hides, stone. And sometimes things in crates with no marks. I do not ask, and the harbormaster does not tell me.");
        AddTopic("passage,downriver,boats,sail", "Passage? Not yet, friend. The Trade Office will tell you the same. They are not selling places on the barges yet.");
        AddEmote("Corm Teague heaves a barrel up the planks and sets it down with a thump that shakes the quay.", 6);
        AddEmote("Corm Teague wipes his brow with a forearm like a ham and takes a long drink from a leather bottle.", 5);
    }
}
