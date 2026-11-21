// @inherit /std/npc

/// <summary>Brann Odell, yard master of the Practice Yard. Sells nothing; talks. The Warrior's and Adventurer's rank trainer is to be taught here later.</summary>
public class VhYardMaster : Npc
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
        AddTopic("teach,teaching,teacher,trainer,train,drill,drills,lessons,rank,ranks,skill,skills,warrior,warriors,adventurer,adventurers", "Soon. I have drill for those who fight in the front line, to teach them to stand and take it and give better. I will be taking recruits here before long, once the yard is in order and the guild has told me who is sound. Ask me again soon.");
        AddTopic("village,hollow,vincent,guard,gate,north,road,wall", "I used to stand where the gate guard stands. Now I stand here, and the view is better, because the people are shooting at things that are not me. The Hollow has always had a yard. Vincent drilled his people on this same patch of ground, and they say you can still hear him count.");
        AddTopic("cellar,cellars,goblin,goblins,tunnels,overseer", "I have been down there once. Goblins fight in a mob, and they fight for the one with the loudest voice. Take the voice away and they fall apart like a badly packed cart. The Overseer is the voice. But do not go down there until you can win a straight fight on this yard. They do not fight fair, and neither should you.");
        AddTopic("rumour,rumor,rumours,rumors,news,gossip,night,footprints,prints", "Somebody has been coming into the yard at night. Not a thief: a thief takes the practice swords, and the swords are all here. Something is taking arrows out of the butts, one or two a night, always the ones that went in crooked. The prints are small and bare. I have not caught it. I have not tried very hard.");
        AddEmote("Brann watches a blow land on the dummy and shakes his head slowly.", 8);
        AddEmote("Brann rolls his shoulder with a grimace and leans harder on his practice sword.", 5);
    }
}
