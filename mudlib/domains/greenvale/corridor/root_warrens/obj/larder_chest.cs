// @inherit /std/find

/// <summary>The goblin's hidden chest in the Cold Larder: a find (sixty gold for the first finder, then less, and a healing potion for each of the first five).</summary>
public class RwLarderChest : Find
{
    public override void Create()
    {
        Name = "Small Chest";
        Ids.Add("chest");
        Ids.Add("small chest");
        Ids.Add("box");
        Ids.Add("hoard");
        ShortDesc = "a small chest under a scrap of hide";
        LongDesc = "The chest is small, plain and iron-bound, and it has stood unopened so long that the scrap of hide over it has dried to a board. It is a goblin's private hoard: the hidden savings of a digger who meant to come back for them.";
        TakenShort = "a small chest, its lid propped open";
        TakenLong = "The small iron-bound chest stands with its lid propped open and the dried hide pushed aside. You have taken what it held.";
        EmptyShort = "a small chest, its lid propped open";
        TakenHere = "A small chest lies here with its lid propped open.";
        EmptyHere = "A small chest lies here with its lid propped open.";
        EmptyLong = "The small iron-bound chest stands with its lid propped open and the dried hide pushed aside. Someone has been here before you, and the hoard is gone.";
        GoldLine = "You lift the dried hide, force the lid and count %gold% gold out of the digger's hoard.";
        ItemLine = "At the bottom lies %item%, and you take it too.";
        Gold = 60;
        ItemPath = "/domains/hollow/obj/healing_potion";
        ItemFinders = 5;
    }
}
