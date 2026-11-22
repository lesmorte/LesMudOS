// @inherit /std/trainer

/// <summary>Wenna Farrow, bowyer of the Strung Yew. Sells the Yew Longbow and is the Scout's rank II trainer (docs/specs/skill-ranks.md). The Aimed Shot is not taught yet.</summary>
public class VhBowyer : Trainer
{
    public override void Create()
    {
        Name = "Wenna Farrow";
        Ids.Add("wenna");
        Ids.Add("farrow");
        Ids.Add("bowyer");
        Ids.Add("woman");
        Ids.Add("wenna farrow");
        ShortDesc = "Wenna Farrow the bowyer";
        LongDesc = "A stout, deep-voiced woman in a leather apron, with arms like a smith's and fingertips stained brown from a lifetime of gut and glue. She sights down a stave while she talks to you, and she does not stop to be polite about it.";
        Level = 3;
        MaxHp = 35;

        AddChat("A bow is only a stick that has agreed to bend. Everything else is persuading it.");
        AddChat("Yew, cut in winter, dried seven years and not a day less. If somebody tells you different, they are selling something.");
        AddChat("Buy, {cmd}sell{/} or just {cmd}browse{/}. I pay half what a bow is worth, which is fair, because I have to string it again.");
        AddTopic("bow,bows,bowyer,trade,yew,longbow,longbows,stave,staves,stock,sell,prices,shop,browse", "I make bows and I sell them, one kind at a time. The longbow is the one I would carry: yew, heartwood belly and sapwood back, a draw that will drive a shaft through a goblin's shield and out the other side. Sixty gold, strung and waxed. {cmd}browse{/}, then {cmd}buy longbow{/}. For a first bow, Torren sells the hunting kind, and there is no shame in it.");
        AddTopic("teach,teaching,teacher,trainer,train,aimed,aim,shot,aimed shot,skill,skills,archery,scout,scouts,rank,ranks,lessons", "I teach scouts here now: the second rank of what you already know, a truer strike and a longer ear for tracks. The fifth level and forty gold, and no cheating on either. Say {cmd}ranks{/} to see, and {cmd}train strike{/} or {cmd}train track{/}. The rank after that is the Hall of Legends' business, in Oakenfell. The aimed shot is another matter: hold the draw, hold the breath, let the arrow go on the end of the exhale. That lesson is not ready yet. Ask me again soon.");
        AddTopic("range,distance,closing,first shot,opening", "The best arrow in a fight is the first, shot while the other fellow is still wondering how to reach you. A man with a bow gets more time to think than a man with a sword, and a wise one uses it for thinking about the next arrow.");
        AddTopic("village,hollow,vincent,lane,fletcher,fletchers,fletcher's lane,neighbours,neighbors", "Fletcher's Lane has been fletchers since Vincent's day. That is why it is called that, and that is why it smells, and I will not hear a word against it. Benson says his grandfather bought arrows here. I do not doubt it. The shop is older than the bench.");
        AddTopic("cellar,cellars,goblin,goblins,tunnels,arrows,butts,yard,practice", "Goblins shoot dreadfully. They cut their shafts crooked and they fletch with crow's feathers, which is how I know they have not had an honest craftsman in the family for generations. If you must go down there, go with a bow, and shoot first. Brann at the yard will let you practise on the butts.");
        AddTopic("rumour,rumor,rumours,rumors,news,warden,recurve,riverwatch,gossip", "There was a Riverwatch warden through here, years back, when my father had the shop. He carried a short recurve in the old style, horn and yew, the finest bow I ever saw him look at twice. He went down looking for Vincent's tunnels, they say, laughing, and he did not come back up. The bow, I think, went with him. Or stayed. Nobody who goes into the cellars is quite sure which.");
        AddTopic("grace,graceful,steady,steadiness,accuracy,accurate,precise,precision,quick,critical", "Grace is the steady hand and the quick eye. It puts the arrow where you looked, and now and then a little deeper than it ought to go. A bow in a graceful hand beats a better bow in a clumsy one, and I say that as someone who sells the better bow.");
        AddTopic("remembered,fame,legend", "The Riverwatch warden with the recurve was remembered for his Grace. They say he never drew faster than anyone else; he just never wasted a breath in the drawing.");
        AddEmote("Wenna runs a thumb along a stave, frowns at it and puts it aside.", 8);
        AddEmote("Wenna tests a bowstring with a flat twang and nods, satisfied.", 6);

        TeachCalling("scout", 2, 2);
        ElsewhereLine = "The third rank and beyond are taught in Oakenfell, at the Hall of Legends.";

        AddStock("/domains/hollow/obj/yew_longbow", 60);
    }
}
