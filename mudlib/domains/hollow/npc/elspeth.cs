// @inherit /std/trainer

public class VhHealer : Trainer
{
    public override void Create()
    {
        Name = "Elspeth Wortley";
        Ids.Add("elspeth");
        Ids.Add("wortley");
        Ids.Add("healer");
        Ids.Add("herbalist");
        Ids.Add("woman");
        ShortDesc = "Elspeth Wortley the healer";
        LongDesc = "A small, brisk woman in a green-stained apron, with grey hair tied up in a cloth and sleeves pushed above the elbow. She smells of mint and woodsmoke, and she looks at your hands first, then your face, as if both were going to tell her something.";
        Level = 2;
        MaxHp = 20;

        AddChat("Cuts, burns and bad chests, that's my trade. Sit down if you're bleeding, but not on the herbs.");
        AddChat("Half the Hollow has been through that door at one time or another. Harland comes for his knees every winter.");
        AddChat("Everything on the table was growing in a field not long ago. Remember that, and you'll treat it kindly.");
        AddTopic("healer,healing,herbalist,herbalism,trade,herbs,herb", "I mend what can be mended and ease what can't. Mostly with plants that grow within a morning's walk, and a good deal of patience.");
        AddTopic("table,herbalist table,mortar,scales,salve,salves,tonic,tonics,poultice,poultices,potion,potions", "That table is where I make salves for burns and cuts, tonics for the tired, poultices for the deep hurts and, when I have the leisure, potions. Crush, weigh, steep, bind. It's slow, and it can't be hurried.");
        AddTopic("materials,ingredients,healing herbs,wildflowers,thistle,blueberries,honey,water,cloth,bottle,bottles,bandage,bandages", "Healing herbs and wildflowers from the meadows, thistle from the ditches, blueberries and honey for the tonics, clean water, cloth for bandages and empty bottles for everything else. I sell the herbs, flowers, thistle, honey, water, cloth and bottles myself; {cmd}browse{/} to see them. For blueberries, try Harland at the inn. Nothing I use is rare, except the one thing.");
        AddTopic("goblin blood,blood,goblin,goblins", "Goblin blood. Of course you've heard. It's the one thing in all my trade that nobody in the Hollow can supply. The scribes want it for their inks and I want it for a good poultice, and not one of us has a drop. Whoever brings some down from the cellar will be very popular. It comes from goblins, in plain words: kill one, and while the body is still fresh, draw the blood with a bleeding kit. Dunstan sells the kits. Do it at once, before the body goes cold, with {cmd}bleed corpse{/}.");
        AddTopic("work,craft,learn,teach,teaching,recipe,recipes,apprentice,lessons,lesson,shop,sell,stock,prices", "The table is set up at last. I teach herbal bandages, minor healing salve, minor stamina tonic and, for anyone who can bring me goblin blood, the hunter's poultice. Type {cmd}lessons{/} to see my prices and {cmd}learn herbal bandage{/} (or any of the others) to be taught. Then {cmd}craft{/} it at my table with the right things in your pack. I also sell what goes into them: {cmd}browse{/}.");
        AddTopic("harland,knees,tavern", "Harland's knees creak louder than his cellar door. I've told him it's the damp. He says it's the goblins.");
        AddTopic("village,hollow,vincent", "I came here as a girl, when the palisade was still new. Vincent's name was already a story by then. The village has never needed a healer more than it does now, though nobody says so.");
        AddLesson("herbal_bandage", 5);
        AddLesson("minor_healing_salve", 8);
        AddLesson("minor_stamina_tonic", 6);
        AddLesson("hunters_poultice", 15);

        AddStock("/domains/hollow/obj/craft/healing_herb", 3);
        AddStock("/domains/hollow/obj/craft/wildflower", 2);
        AddStock("/domains/hollow/obj/craft/thistle", 1);
        AddStock("/domains/hollow/obj/craft/honey", 3);
        AddStock("/domains/hollow/obj/craft/clean_water", 1);
        AddStock("/domains/hollow/obj/craft/empty_bottle", 1);
        AddStock("/domains/hollow/obj/craft/cloth_strip", 2);
        AddEmote("Elspeth ties a fresh bunch of herbs and hangs it from a rack.", 8);
        AddEmote("Elspeth lifts the kettle lid, sniffs, and puts it back with a frown.", 5);
    }
}
