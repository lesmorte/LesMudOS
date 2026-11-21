// @inherit /std/npc

public class RvHughPardew : Npc
{
    public override void Create()
    {
        Name = "Giles Pardew";
        Ids.Add("giles");
        Ids.Add("pardew");
        Ids.Add("clerk");
        Ids.Add("senior clerk");
        ShortDesc = "Giles Pardew, a senior clerk";
        LongDesc = "A heavy, balding man with a drooping moustache, sleeves protectors on his forearms and a large brass key on a chain at his belt. He sighs often and always at the paperwork.";
        Level = 1;
        MaxVitality = 18;

        AddChat("Come back in a season, if it is passage you want. For freight, wait in the line.");
        AddChat("Thirty years at this desk. I have seen the river rise, I have seen it fall, and I have seen the forms change four times, and not once for the better.");
        AddTopic("passage,downriver,ticket,travelers,travel", "No, no. We do not sell passage. Not yet. There are barges, certainly, but they carry grain and fish, not people. The Assembly will want a form for it first.");
        AddTopic("assembly,forms,form,paperwork,stamp", "The Assembly requires a form for everything. I have a form for forms. It is a rather good form.");
        AddTopic("consortium,merchants,caravan,trade", "The Consortium books most of our freight, and they pay promptly. We keep them happy. Everything else follows from that.");
        AddEmote("Giles Pardew sighs gustily and turns a page.", 6);
        AddEmote("Giles Pardew polishes his brass key on his sleeve, looks at it, and polishes it again.", 4);
    }
}
