// @inherit /std/npc

public class GrWystan : Npc
{
    public override void Create()
    {
        Name = "Wystan";
        Ids.Add("wystan");
        Ids.Add("clerk");
        Ids.Add("nervous clerk");
        Ids.Add("tallyman");
        Ids.Add("scribe");
        ShortDesc = "Wystan, a nervous clerk";
        LongDesc = "A thin young man in an ink-spotted coat, hunched over a lap desk with a stub of pencil, who jumps whenever a branch cracks. His eyes are red with strain, and he keeps a bundle of waybills hugged to his chest.";
        Level = 2;
        MaxHp = 25;

        AddChat("Forty-one bales, thirty crates, sixteen oxen. I have counted them eleven times.");
        AddChat("Please do not stand behind me. I mean no offense. It is only that I do not like it.");
        AddChat("Is that a branch, or is it something? No. Of course. A branch.");
        AddTopic("waybills,bills,manifest,cargo,goods", "Wool, dyed cloth, iron fittings and eighteen kegs of Hollow ale. If I lose a keg, it comes out of my pay.");
        AddTopic("goblins,raiders,dark,wood,night", "They took a clerk from the camp two caravans ago. Not killed him, taken. I do not know what for. Captain Hereward says I am not to worry. I worry.");
        AddTopic("lambert,merchant,master", "Master Lambert is a kind man. He says I will get used to the road. I have been on it nine times.");
        AddTopic("shrine,old shrine,lamp", "I leave a ribbon at the Old Shrine each run. It costs nothing. It seems to help.");
        AddTopic("camp,fire,ring,wagons", "Inside the ring I am nearly calm. Beyond the ring I am not responsible for anything I say.");
        AddEmote("Wystan flinches at a creak from the trees and pretends he did not.", 6);
        AddEmote("Wystan licks his pencil, writes a figure and crosses it out.", 6);
    }
}
