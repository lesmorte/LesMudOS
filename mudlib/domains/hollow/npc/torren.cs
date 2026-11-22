// @inherit /std/shopkeeper

public class Torren : Shopkeeper
{
    public override void Create()
    {
        Name = "Torren";
        Ids.Add("blacksmith");
        Ids.Add("smith");
        Ids.Add("blacksmith torren");
        ShortDesc = "Blacksmith Torren";
        LongDesc = "A broad, soot-streaked man with forearms like anvils and a leather apron scorched by a thousand sparks. He eyes every blade you carry as if grading it.";
        Level = 3;
        MaxVitality = 40;

        AddChat("Quality steel saves lives. Cheap steel fills graveyards.");
        AddChat("Buy, {cmd}sell{/} or just {cmd}browse{/}. I pay half what a blade is worth, no more, no less.");
        AddTopic("sword,swords,steel,blade,blades", "A sword's only as good as the arm behind it. The rusty ones are cheap, but they'll hold an edge if you respect them. Try {cmd}browse{/}, then {cmd}buy sword{/}.");
        AddTopic("longsword,fine,rare", "Now and then I make something fine, when the iron is right. If I have one, it'll be on the rack. Sixty gold, and worth every coin.");
        AddTopic("mace,staff,bow,bows,maces,staves", "A mace for the temple folk, a staff for the scholars, a bow for the ones who like their enemies at arm's length. Cheap, all three. Not everyone is trained for every weapon, mind: swords for those who know them, and so on.");
        AddTopic("better,improved,felling,flanged,boar,dirk,upgrade,armour,armor,jerkin,shed", "Once you have a few coins, there is better steel on the rack: a felling axe, a flanged mace, a boar spear and a steel dirk. For armour, go through the door in the north wall to Oswy's shed; he does leather and padding, and I do not. Bows are Wenna's, in Fletcher's Lane, and staves are Halbert's, in the loft above it.");
        AddTopic("goblins,goblin,cellar", "Goblin steel? Scrap. They bring me broken blades sometimes, and I pay by the pound.");
        AddTopic("might,strength,strong,arm,arms,muscle,power,heavy,swing", "Might is in the arm, and no steel can lend it. The same sword hits harder in a mighty hand, and a rusty blade swung with Might will out-do a fine one swung by a weakling. I make the iron; the arm is your business.");
        AddTopic("remembered,famous smith,fame", "The smith before me was remembered for his Might. He could bend a horseshoe straight with his hands, and the village still keeps one of his, bent back the wrong way, over the forge door.");
        AddEmote("Torren hammers a glowing blade on the anvil; sparks fly across the smithy.", 8);
        AddEmote("Torren plunges a blade into the quench barrel with a hiss of steam.", 5);

        AddStock("/domains/hollow/obj/rusty_sword", 10);
        AddStock("/domains/hollow/obj/training_spear", 8);
        AddStock("/domains/hollow/obj/iron_dagger", 6);
        AddStock("/domains/hollow/obj/woodcutter_axe", 18);
        AddStock("/domains/hollow/obj/small_shield", 12);
        AddStock("/domains/hollow/obj/novice_mace", 12);
        AddStock("/domains/hollow/obj/apprentice_staff", 10);
        AddStock("/domains/hollow/obj/hunting_bow", 20);
        AddRareStock("/domains/hollow/obj/fine_iron_longsword", 60, 30);
        AddStock("/domains/hollow/obj/felling_axe", 65);
        AddStock("/domains/hollow/obj/flanged_mace", 55);
        AddStock("/domains/hollow/obj/boar_spear", 50);
        AddStock("/domains/hollow/obj/steel_dirk", 45);
    }
}
