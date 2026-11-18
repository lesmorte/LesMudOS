// @inherit /std/npc

public class EeDelwyn : Npc
{
    public override void Create()
    {
        Name = "Delwyn";
        Ids.Add("delwyn");
        Ids.Add("hunter");
        Ids.Add("bearded hunter");
        ShortDesc = "Delwyn, a hunter";
        LongDesc = "A heavy-shouldered man with a bristling black beard, a skinning knife at his belt and a bow across his back. His hands are scarred and his boots are patched, and he moves quietly for such a big man.";
        Level = 5;
        MaxHp = 65;

        AddChat("Deer, boar, and the odd wolf. Nothing here you can't kill with enough arrows.");
        AddChat("Keep downwind. Everything in the wood has a better nose than you.");
        AddChat("Fire's lit. Sit. You look like you've walked far.");
        AddTopic("hunting,hunter,hunters,game", "Deer on the lawn, boar in the wallow. We take what we need and leave the rest. The Rangers pay for wolf pelts. The inns pay for venison.");
        AddTopic("boar,boars,wallow,tusker", "The boar east of the lawn are bad-tempered. A tusker will charge from thirty paces. Climb a tree, or stand behind one and pray. Aim for the shoulder.");
        AddTopic("wolves,wolf,pack,tracks", "Wolves hunt in threes here. They split to flank you. Back to a tree and keep your bow.");
        AddTopic("spiders,spider,thicket", "Elowen was bitten once. Big as my hand, that one. We keep clear of the thicket now.");
        AddTopic("goblins,goblin,scouts,cords", "They come through the deep wood, east of the lawn. They carry cords. Morwenna said they counted. I thought she was daft. Now I'm not sure.");
        AddTopic("morwenna,hunter,missing,friend", "She went out on the east trail in the autumn with a pack, and a week's food. I thought she'd gone to the Rangers. The commander says she never came. I don't like it.");
        AddTopic("camp,hunters camp,hide,skins", "Our camp has stood here ten years. If you want to learn the wood, you can sit by the fire and keep your mouth shut.");
        AddTopic("oak,marked oak,markings,pact", "East of the trail there's a great oak, carved. I've seen the animals go round it. They never cross the ring. Neither will I.");
        AddEmote("Delwyn turns the hare on the spit.", 6);
        AddEmote("Delwyn sharpens his skinning knife with long slow strokes.", 6);
        AddEmote("Delwyn listens to the wood, head cocked.", 6);
    }
}
