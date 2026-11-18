// @inherit /std/npc

public class FdClerkAnsel : Npc
{
    public override void Create()
    {
        Name = "Ansel Verrick";
        Ids.Add("ansel");
        Ids.Add("verrick");
        Ids.Add("clerk");
        Ids.Add("records clerk");
        ShortDesc = "Ansel Verrick, a clerk";
        LongDesc = "A narrow, tidy man in black sleeves and ink-stained cuffs, his spectacles pushed up onto a high forehead. He writes without looking at the page, and he has a pen behind each ear in case of emergency.";
        Level = 1;
        MaxHp = 15;

        AddChat("Records of citizens, births, deaths, boundaries. If it can be written down, it is kept here.");
        AddChat("Please do not lean on the grille. It was a gift.");
        AddTopic("records,rolls,citizens,citizenship,register", "The city keeps rolls of its citizens and of those with leave to trade. Entering a stranger on the roll is a matter for the Assembly's Clerk of Registers, and the Clerk is not receiving new names from travelers at present.");
        AddTopic("petition,petitions,assembly", "Petitions to the Assembly are entered here, by registered citizens, on the proper form, with three witnesses. You are welcome to look at the form, but not to fill it in.");
        AddTopic("fees,fee,price,cost", "All fees are posted by the grille. If the posted fee does not seem reasonable, the correct remedy is a petition. I recommend you not file one.");
        AddEmote("Ansel Verrick dips his pen, writes a line without looking down, and blots it.", 6);
        AddEmote("Ansel Verrick slides a roll into its pigeonhole with a small click of satisfaction.", 5);
    }
}
