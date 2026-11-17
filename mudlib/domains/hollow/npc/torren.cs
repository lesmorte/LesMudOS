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
        MaxHp = 40;

        AddChat("Quality steel saves lives. Cheap steel fills graveyards.");
        AddChat("Buy, {cmd}sell{/} or just {cmd}browse{/}. I pay half what a blade is worth, no more, no less.");
        AddTopic("sword,swords,steel,blade,blades", "A sword's only as good as the arm behind it. The rusty ones are cheap, but they'll hold an edge if you respect them. Try {cmd}browse{/}, then {cmd}buy sword{/}.");
        AddTopic("longsword,fine,rare", "Now and then I make something fine, when the iron is right. If I have one, it'll be on the rack. Sixty gold, and worth every coin.");
        AddTopic("goblins,goblin,cellar", "Goblin steel? Scrap. They bring me broken blades sometimes, and I pay by the pound.");
        AddEmote("Torren hammers a glowing blade on the anvil; sparks fly across the smithy.", 8);
        AddEmote("Torren plunges a blade into the quench barrel with a hiss of steam.", 5);

        AddStock("/domains/hollow/obj/rusty_sword", 10);
        AddStock("/domains/hollow/obj/training_spear", 8);
        AddStock("/domains/hollow/obj/iron_dagger", 6);
        AddStock("/domains/hollow/obj/woodcutter_axe", 18);
        AddStock("/domains/hollow/obj/small_shield", 12);
        AddRareStock("/domains/hollow/obj/fine_iron_longsword", 60, 30);
    }
}
