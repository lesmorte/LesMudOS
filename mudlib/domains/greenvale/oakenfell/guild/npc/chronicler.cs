// @inherit /std/npc

public class GqChronicler : Npc
{
    public override void Create()
    {
        Name = "Perrin";
        Ids.Add("chronicler");
        Ids.Add("perrin");
        Ids.Add("scribe");
        Ids.Add("old man");
        ShortDesc = "Perrin, the Chronicler of the Hall";
        LongDesc = "A stooped, bright-eyed old man in a grey scribe's gown, ink permanently worked into the creases of his fingers and a pair of spectacles pushed up into white hair. He holds himself the way people do who spend their lives handling things older than they are, and he speaks of the stone walls the way other men speak of their grandchildren.";
        Level = 20;
        MaxHp = 90;

        AddChat("Welcome to the Chronicle Gallery. I am Perrin, the Chronicler. Take your time; nothing here is going anywhere. Ask me what the Chronicle is, if you like.");
        AddChat("Levels are power. Renown is fame. The Chronicle is legacy. A person may become strong, and a person may become famous, but only a few are remembered.");
        AddChat("You may have noticed the book is blank, and the walls nearly so. The records are not yet open to readers. When they are, you will hear it first from me.");
        AddChat("Whatever you do out there, do it as though someone will write it down. Someone may.");
        AddTopic("chronicle,grand chronicle,book,record,records", "The Chronicle is the official record of Hollowmere, kept by the Hall of Legends. It holds the events, the deeds, the first discoveries, the milestones of factions and guilds, the great victories and the making of great items. Some day it will be like a library, a museum and an archive in one. But I must be honest: it is not yet open for reading here. The lectern waits, and so do I.");
        AddTopic("renown,fame,tiers,tier", "Renown is how widely a character is known, from the first tale to living folklore. The board by the entrance gives the nine tiers: Unknown, Local, Recognized, Acclaimed, Renowned, Famous, Heroic, Legendary and Mythic. It is not the standing a faction gives you; it is simply whether people have heard of you. It is hard to lose and has to be earned. We do not yet keep the records here.");
        AddTopic("heroes,hero,hall of heroes", "The Hall of Heroes is for those who reach Legendary Renown, win major story victories, lead a faction or make an exceptional contribution. Once a name is carved there, it remains, even if its owner never walks these halls again. The stone is bare for now.");
        AddTopic("legends,legend,hall of legends", "The Hall of Legends is a separate honor from the Hall of Heroes, and a rarer one: the first Mythic victory, world-changing decisions, realm-saving deeds, the founders of great guilds. Only a tiny number of names are ever meant to be carved there. Do not take it amiss that this building shares the name. It is the building's ambition.");
        AddTopic("infamy,villains,hall of infamy,notorious,criminals", "History remembers villains too, so the Hall of Infamy keeps the notorious: the most wanted criminals, infamous betrayals, legendary bandits, dark rituals, political assassinations and world-altering villainy. It is no honor, but it is accurate.");
        AddTopic("firsts,first,discoveries,discovery,explorers", "The Chronicle records who found a thing first: who first reached a distant wood, who first defeated a great beast, who first mapped a whole region. Those firsts are written down in the person's name, and they stay written. I would dearly like to see the first one. Nothing is entered yet.");
        AddTopic("hall,hall of legends building,building,hallmasters", "The Hall of Legends is the guild confederation's seat. It keeps the Chronicle, oversees advanced training and certifies the guilds. Beyond that, the hallmasters in the chamber to the south will tell you better than I.");
        AddTopic("wall,walls,stone,carvings", "Three walls: Heroes, Legends and Infamy. Look at them, if you like. They are carved to wait.");
        AddEmote("Perrin sharpens a quill and tests it on the back of his hand.", 8);
        AddEmote("Perrin runs a fingertip along the edge of the lectern, as one would stroke a sleeping cat.", 6);
    }
}
