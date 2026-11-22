// @inherit /std/trainer

/// <summary>Brann Odell, yard master of the Practice Yard. Sells nothing; the Warrior's and the Adventurer's rank II trainer (docs/specs/skill-ranks.md).</summary>
public class VhYardMaster : Trainer
{
    public override void Create()
    {
        Name = "Brann Odell";
        Ids.Add("brann");
        Ids.Add("odell");
        Ids.Add("yard master");
        Ids.Add("master");
        Ids.Add("brann odell");
        Ids.Add("man");
        ShortDesc = "Brann Odell the yard master";
        LongDesc = "A square, weathered man with a drill-master's carry and a stoop where an old wound has set. He leans on a blunted practice sword the way other men lean on a stick, and he watches every blow that lands on the dummy with an expression of long, quiet disappointment.";
        Level = 4;
        MaxHp = 50;

        AddChat("Footwork. Eighty in a hundred fights are lost on the feet. The other twenty are lost on the stairs afterwards.");
        AddChat("The dummy does not hit back, and neither does the next fellow, if you are quick enough. Hit it. Hit it again.");
        AddChat("Type {cmd}verbose on{/} and every blow tells you what it did. Then {cmd}kill dummy{/}, and learn what your weapon is worth.");
        AddTopic("dummy,target,training,training dummy,practice,verbose,damage,hit,hits,blows,straw", "It cannot be killed, so do not try; and it pays no experience, so do not stay. It is here to show you what you do. Type {cmd}verbose on{/} and {cmd}kill dummy{/}, and each blow will tell you the damage it did. Try a new weapon on it before you trust it with your life. When you are done, {cmd}flee{/} or just walk away, and the straw mends itself.");
        AddTopic("yard,butts,archery,bow,bows,arrows,range,shooting", "The butts are at the far end, and you are very welcome to put arrows in them, so long as you keep them out of the gatepost. Wenna in the lane sells a good bow. I sell nothing, and I charge nothing, and I will tell you when you are doing it wrong.");
        AddTopic("teach,teaching,teacher,trainer,train,drill,drills,lessons,rank,ranks,skill,skills,warrior,warriors,adventurer,adventurers", "The yard is in order, and I am taking recruits. I drill warriors and adventurers: the second rank of the shield bash, and of the double strike. The fifth level and forty gold, and I do not care whose name you drop. Say {cmd}ranks{/} to see where you stand, and {cmd}train bash{/} or {cmd}train double{/}. After that it is the Hall of Legends in Oakenfell, and they will want more of both.");
        AddTopic("village,hollow,vincent,guard,gate,north,road,wall", "I used to stand where the gate guard stands. Now I stand here, and the view is better, because the people are shooting at things that are not me. The Hollow has always had a yard. Vincent drilled his people on this same patch of ground, and they say you can still hear him count.");
        AddTopic("cellar,cellars,goblin,goblins,tunnels,overseer", "I have been down there once. Goblins fight in a mob, and they fight for the one with the loudest voice. Take the voice away and they fall apart like a badly packed cart. The Overseer is the voice. But do not go down there until you can win a straight fight on this yard. They do not fight fair, and neither should you.");
        AddTopic("rumour,rumor,rumours,rumors,news,gossip,night,footprints,prints", "Somebody has been coming into the yard at night. Not a thief: a thief takes the practice swords, and the swords are all here. Something is taking arrows out of the butts, one or two a night, always the ones that went in crooked. The prints are small and bare. I have not caught it. I have not tried very hard.");
        AddTopic("resolve,endurance,wind,breath,tire,tired,tiring,discipline,stand,grit", "Resolve is what is left when the legs have quit. A fighter with plenty of it has Endurance to spend on the third blow and the fifth, while the fellow with none is leaning on my fence. Drill builds it. So does being too stubborn to sit down.");
        AddTopic("remembered,best soldier,legend,fame", "The best soldier I ever drilled was remembered for his Resolve and not his sword. He lost every bout on this yard, and was still standing at the end of every one.");
        TeachCalling("warrior", 2, 2);
        TeachCalling("adventurer", 2, 2);
        ElsewhereLine = "The third rank and beyond are taught in Oakenfell, at the Hall of Legends.";
        AddEmote("Brann watches a blow land on the dummy and shakes his head slowly.", 8);
        AddEmote("Brann rolls his shoulder with a grimace and leans harder on his practice sword.", 5);
    }
}
