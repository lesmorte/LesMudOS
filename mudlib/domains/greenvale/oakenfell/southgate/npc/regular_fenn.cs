// @inherit /std/npc

public class SgRegularFenn : Npc
{
    public override void Create()
    {
        Name = "Fenn Alder";
        Ids.Add("fenn");
        Ids.Add("fenn alder");
        Ids.Add("regular");
        Ids.Add("clerk");
        Ids.Add("young man");
        ShortDesc = "Fenn Alder, a clerk";
        LongDesc = "A thin young man with ink on his cuffs and a hopeful, tired look. He wears a clerk's grey coat, a quill behind his ear, and a stack of papers in a satchel he clutches in his sleep.";
        Level = 2;
        MaxHp = 20;

        AddChat("The Assembly meets, the Consortium nods, and somehow a bylaw appears. I write them all down.");
        AddChat("Keep your eyes open in the Market. Everything is for sale, including the gossip.");
        AddChat("I'm only a clerk, but I hear things. Clerks always do.");
        AddTopic("districts,district,city,layout", "South of the market are the inns and the stables, the Market is the heart of it, and the Founder's District is north, with City Hall and the Assembly. The Guild Quarter lies west of the Square, with the Hall of Legends, and the River District is east. Don't confuse them.");
        AddTopic("assembly,government,council", "The Greenvale Assembly sits in Founder's Square, and its meetings are loud. Half of them say the Consortium owns half of the Assembly, and the other half say it's fine.");
        AddTopic("consortium,merchants,trade", "The Merchant Consortium controls the Market, the caravans and half the warehouses. They say 'Trade Builds Tomorrow', and nobody argues, because nobody dares.");
        AddTopic("hall,hall of legends,legends", "The Hall of Legends is neutral, they say. Neutral with a bell tower and a gold-bound chronicle. Don't believe in neutral halls.");
        AddTopic("oak,great oak,tree", "The oak's a legend and a trade sign. Ask old Tam about it; he'll tell you where it stood, and how much of it he still believes.");
        AddEmote("Fenn Alder scribbles on a scrap of paper, then crosses it out.", 8);
        AddEmote("Fenn Alder glances over his shoulder before whispering something to Dessa.", 6);
    }
}
