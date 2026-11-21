// @inherit /std/room

/// <summary>The Hedge-Scholar's Loft: Halbert Quill's loft above the net-maker's. The Ironshod Staff and the Scholar's Robe are sold here; the Mage's rank trainer belongs here later.</summary>
public class VhHedgeScholarsLoft : Room
{
    public override void Create()
    {
        ShortDesc = "The Hedge-Scholar's Loft";
        LongDesc = "A long, sloping loft under the roof, reached by an outside stair, where the rafters come down so low that tall visitors learn to walk with a bow. Every wall is books: real ones, spined in leather, and piles of loose pages tied up with twine. A broad table under the one window holds an inkstand, a lens on a stand, a dish of dried herbs and a skull of some small animal with a runic ring drawn around it. Staves lean in the corner, and robes hang from a peg. Below, a net-maker's shuttle clacks without cease.";
        Exits["out"] = "/domains/hollow/village/fletchers_lane";
        Safe = true;

        AddDetail("books,book,shelves,shelf,spines,pages,twine", "Hundreds of books, many of them borrowed from places that do not know it and are not likely to find out. One shelf is labelled \"WIZARDS: INCORRECT\" in a small, firm hand, and a second \"WIZARDS: CORRECT BUT RUDE\".");
        AddDetail("table,inkstand,ink,lens,herbs,dish", "A table stained with years of ink. The lens on its stand magnifies a rune cut into a scrap of bone so far that you could count the grains. The dried herbs in the dish are none you know.");
        AddDetail("skull,animal skull,ring,runic ring,rune,runes", "The skull of something small and furry, set in the middle of a ring of runes drawn in chalk. The ring is complete and the skull has an air of being slightly insulted by it.");
        AddDetail("staves,staff,corner", "Several staves lean in a corner, some bare ash and some capped with iron. The iron-shod ones are worked with small runes around the upper cap, and they make your fingers itch.");
        AddDetail("robes,robe,peg,pegs", "Robes of undyed wool hang from a peg, their hems and cuffs stitched over with the first runes a student learns. They are scholarly in a way that makes you stand up straighter.");
        AddDetail("window,roof,rafters,eaves,net-maker,shuttle", "A single window looks out over the lane. Through the floorboards comes the clack of the net-maker's shuttle, steady as a clock.");

        AddSpawn("/domains/hollow/npc/halbert_quill");
    }
}
