// @inherit /std/shopkeeper

/// <summary>Oswy Thackett, the armourer of the shed beside the Iron Oak. Sells padded and leather armour. Does not teach yet.</summary>
public class VhArmourer : Shopkeeper
{
    public override void Create()
    {
        Name = "Oswy Thackett";
        Ids.Add("oswy");
        Ids.Add("thackett");
        Ids.Add("armourer");
        Ids.Add("armorer");
        Ids.Add("oswy thackett");
        Ids.Add("man");
        ShortDesc = "Oswy Thackett the armourer";
        LongDesc = "A narrow, careful man in shirtsleeves with a measuring cord round his neck and a mouthful of pins. He has never in his life been hurried, and he sizes you up from boots to hat in a single unhurried pass, as if you were a bolt of cloth he had already priced.";
        Level = 3;
        MaxHp = 30;

        AddChat("Every good fight is won before it begins, in the fit of a jerkin. Stand still, if you please.");
        AddChat("Padding for the careless, leather for the careful, and neither for the dead. I sell both.");
        AddChat("Buy, {cmd}sell{/} or just {cmd}browse{/}. I will pay half of what any piece is worth, and I will check the stitching first.");
        AddTopic("armour,armor,jerkin,jerkins,leather,padding,padded,cap,trade,stock,sell,prices,shop,browse", "A padded jerkin for twenty gold, quilted wool and flax, and any back may wear it. A leather jerkin for fifty, boiled and laced, twice the protection and stiff as a board for the first week. A leather cap for twenty-five, and I will not hear it called a helm. {cmd}browse{/} to see them, {cmd}buy{/} what you like.");
        AddTopic("wear,wearing,calling,who,mage,mages,priest,warrior,scout,adventurer,fit,fitting,restrictions", "Any calling will wear padding. Leather is cut heavy in the shoulder, and a mage will be sorry for it the first time they try to cast; I will sell it to them if they insist, but I will say so first. The scholar's robe is not mine, it is Halbert's, and it is cut for the temple folk and the mages and for no one else. Anything heavier than leather I do not make, and neither does anyone in the Hollow.");
        AddTopic("vest,commander,commander's vest,overseer,best,better", "The Leather Commander's Vest is better than anything on my racks: boiled leather, studded at the shoulder, made for an officer. I would love to see the maker's mark, and I never have, because the Overseer wears it. If you take it off him, bring it by and let me look.");
        AddTopic("teach,teaching,teacher,trainer,train,skill,skills,lessons,rank,ranks", "Teaching? I mend and I sell. If the guild wants someone to show a recruit how to move in armour, they know where I am. Perhaps, soon. Ask me again when the Practice Yard has its drill master.");
        AddTopic("village,hollow,vincent,iron oak,torren,smithy,smith,shed", "My shed shares a wall with Torren's forge, which is why I am never cold and always deaf. We came to an arrangement years ago: he does the metal and I do the rest. The rest is more.");
        AddTopic("cellar,cellars,goblin,goblins,tunnels", "Goblins in armour! They wear whatever they steal, and they do not know how to tie it. I once saw one with a padded jerkin on upside down. It was winning.");
        AddTopic("rumour,rumor,rumours,rumors,news,gossip,maker,mark,marks,crow", "That vest the Overseer wears was never goblin work. Human stitching, an officer's cut, a maker's mark scratched out. Somebody made armour for a company of soldiers in this village, long ago, and I would like to know where they went. There is nothing in the Hollow's old ledgers, and there should be.");
        AddEmote("Oswy takes a pin from his mouth, pushes it into a seam and takes another.", 8);
        AddEmote("Oswy squints along a seam with one eye closed, and says nothing at all.", 6);

        AddStock("/domains/hollow/obj/padded_jerkin", 20);
        AddStock("/domains/hollow/obj/leather_jerkin", 50);
        AddStock("/domains/hollow/obj/leather_cap", 25);
    }
}
