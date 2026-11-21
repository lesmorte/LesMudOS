// @inherit /std/shopkeeper

/// <summary>Halbert Quill, the hedge-scholar of the loft above Fletcher's Lane. Sells the Ironshod Staff and the Scholar's Robe. The Mage's rank trainer is to be taught here later; for now he only talks of it.</summary>
public class VhHedgeScholar : Shopkeeper
{
    public override void Create()
    {
        Name = "Halbert Quill";
        Ids.Add("halbert");
        Ids.Add("quill");
        Ids.Add("scholar");
        Ids.Add("hedge-scholar");
        Ids.Add("hedge scholar");
        Ids.Add("halbert quill");
        Ids.Add("man");
        ShortDesc = "Halbert Quill the hedge-scholar";
        LongDesc = "A tall, stooped man with ink to his wrists, a squint and a robe that has been mended in four different colours of thread. He has spent a lifetime learning without once having been properly taught, and he regards the whole business as a personal triumph.";
        Level = 3;
        MaxHp = 28;

        AddChat("No university would have me, so I made one. It has one student, and he is not very good, and he is me.");
        AddChat("Magic is mostly reading. The rest is deciding when to stop reading and start pointing.");
        AddChat("Buy, {cmd}sell{/} or just {cmd}browse{/}. Mind the third shelf, it is not for sale and it bites.");
        AddTopic("staff,staves,ironshod,ironshod staff,trade,stock,sell,prices,shop,browse,runes", "A staff will not make you a mage, but it makes you look like one, and that has turned more fights than a spell ever did. The ironshod staff is ash capped in iron, the runes cut small round the cap: they do nothing you can see, but they sharpen the mind of whoever holds it, a little. Fifty-five gold. {cmd}browse{/}, then {cmd}buy staff{/}.");
        AddTopic("robe,robes,scholar's robe,scholars robe,wear,wearing,insight,mage,mages,priest,priests", "A scholar's robe, forty gold. Undyed wool, the first runes of a student stitched in the hems. It stops nothing at all, and it clears the head. It is cut for mages and for the temple folk, and it hangs on anyone else like a tent. Insight, if you want it in a word: seeing what is there instead of what you expected.");
        AddTopic("teach,teaching,teacher,trainer,train,arcane,bolt,arcane bolt,second,working,workings,skill,skills,lessons,rank,ranks", "The Arcane Bolt has more than one working, and the second is a finer thing than the first. I will be teaching it here, in this loft, before long. Not yet. It wants a proper lesson, with a proper blackboard, and I have not found the blackboard. Ask me again soon.");
        AddTopic("village,hollow,vincent,lane,loft,books", "I came for the quiet, and found the net-maker. The Hollow is a marvellous place to read, if you do not mind goblins under your feet. Vincent Holloway kept a library, they say. I have looked for it. A man who builds a village on top of something, and then keeps a library, is not a man who meant to forget.");
        AddTopic("cellar,cellars,goblin,goblins,tunnels,mushrooms,mushroom,cavern,spore,spores", "The cellars are not a place for the unprepared, and I speak as one who has been unprepared in several of them. The mushroom cavern is a marvel: those caps glow with something that is not quite light. A hedge-scholar could write a lifetime of papers on the air in it. Someone should, and it should not be me, because I would be unable to stop.");
        AddTopic("rumour,rumor,rumours,rumors,news,gossip,oak,hollow oak,root,roots", "Everyone will tell you Hollow is short for Holloway, and so it may be. But there is an older story, and I found it in a ledger that nobody had read in sixty years: a great oak, the Hollow Oak, which stood where the square is now. They felled it for the palisade, and the stump went down and down. I think the root is still alive. I think it goes through the cellars. I think, if you were very quiet in the mushroom cavern, you might see where it has come out.");
        AddEmote("Halbert holds a page up to the window, squints, and sighs in a way that suggests betrayal.", 8);
        AddEmote("Halbert dips a pen, writes three words and crosses out two.", 6);

        AddStock("/domains/hollow/obj/ironshod_staff", 55);
        AddStock("/domains/hollow/obj/scholars_robe", 40);
    }
}
