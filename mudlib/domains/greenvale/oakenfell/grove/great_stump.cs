// @inherit /std/room

public class GvGreatStump : Room
{
    public override void Create()
    {
        ShortDesc = "The Great Stump";
        LongDesc = "The path ends at the foot of the Great Stump, and for a moment the eye refuses to take it in. It rises out of the grove like a low grey hill with a flat top, some fifty feet across, sixteen long paces from one side to the other, and about ten feet high at its tallest rim. Its surface is ridged like bark but hard and cold as stone, for the oak has been turning to stone for centuries. Roots as thick as wagons run out from it across the grove in enormous arcs and vanish under the walls on every side, and its flank is covered with lightning scars, ancient carvings, bronze plaques and two slate markers. The only sound is birdsong.";
        Exits["south"] = "/domains/greenvale/oakenfell/grove/hidden_grove";

        AddDetail("stump,great stump,oak,great oak,tree,wood,stone", "Fifty feet across and ten high, a vast and almost circular drum of grey wood that has become stone, its bark ridged and furrowed as it was in life. Seen close, it is not one trunk but a union of trunks that grew together, as a great oak does in its extreme age. It is cold to the touch, and your hand comes away dry and faintly gritty, as from a quarried wall.");
        AddDetail("top,surface,rings,flat top,crown,rim", "The top is a broad grey floor split by a few long cracks, wide enough, you think, to hold a village feast. Rings circle in toward the center, so fine and so many that the eye loses count in the first handful. A thin coat of moss and lichen softens the edges.");
        AddDetail("roots,root,arcs", "The roots leave the stump like the spokes of a wheel and rise and sink across the grove, each as thick as a wagon and gone grey as the stump itself. Where they reach the old walls they dive beneath them. Standing here, you can feel how far they go: the whole grove, and some way beyond it, under the city.");
        AddDetail("scars,lightning scars,scar,lightning,char,burn,burned,burnt,charcoal,blackened", "Down the south-western flank run great black scars, wider than a man's span, forking and rejoining like rivers on a map. The wood there is fused and glassy, and where it has chipped away you can see layers of charcoal turned to stone, laid down in the three days the oak burned. Nothing grows in the scars.");
        AddDetail("carvings,carving,carved,marks,letters", "The flank of the stump is carved from the ground to the rim with the marks of generations: pictures, letters, symbols and a thousand pairs of initials, the oldest worn almost smooth. You could {cmd}read{/} the carvings.");
        AddReadable("carvings,carving,carved,marks,letters", "Low on the western face, so worn that you must trace them with a finger, are the oldest carvings: a tree of many branches, three wavering lines for the River Mere, and a ring of small figures with joined hands. Higher up, in later hands, run hundreds of initials in pairs, bound with knots and hearts, and short vows and names, some weathered, some bright. Deepest of all, cut along the rim in letters a hand high, runs one line:\n\"WHERE IT FELL, WE STAY\"", "The flank of the stump is carved with the marks of generations. You could {cmd}read{/} the carvings.");
        AddDetail("plaques,plaque,memorial,memorials,bronze,memorial plaques", "Four bronze plaques are set into the foot of the stump, each with a few lines of lettering. You could {cmd}read rowan plaque{/}, {cmd}read ambrose plaque{/}, {cmd}read rangers plaque{/} or {cmd}read hale plaque{/}.");
        AddReadable("plaques,plaque,memorial,memorials,memorial plaques", "Four bronze memorial plaques are set into the stump's foot. They are named after their subjects: the rowan plaque, the ambrose plaque, the rangers plaque and the hale plaque. Read whichever you like.", "Four bronze plaques are set into the foot of the stump. You could {cmd}read{/} the plaques.");
        AddDetail("rowan plaque,rowan,caretaker plaque", "A small bronze plaque, green with age, set low where the roots meet the stump. You could {cmd}read{/} it.");
        AddReadable("rowan plaque,rowan,caretaker plaque", "\"IN MEMORY OF HESTIA ROWAN, who kept this ground for forty years and told no one, and was remembered for her Spirit. Those who come after keep it as she kept it.\"", "A small bronze plaque, green with age, set low where the roots meet the stump. You could {cmd}read{/} it.");
        AddDetail("ambrose plaque,ambrose,vow plaque,vows plaque", "A bronze plaque with a border of oak leaves, kept bright by the loving hands of someone who comes often. You could {cmd}read{/} it.");
        AddReadable("ambrose plaque,ambrose,vow plaque,vows plaque", "\"CORIN AND LYSA AMBROSE spoke their vows beneath the Great Oak, and kept them for fifty-one years. Rooted together.\"", "A bronze plaque with a border of oak leaves. You could {cmd}read{/} it.");
        AddDetail("rangers plaque,rangers,ranger plaque,riverwatch plaque", "A heavy bronze plaque with an engraved border of linked rings. You could {cmd}read{/} it.");
        AddReadable("rangers plaque,rangers,ranger plaque,riverwatch plaque", "\"TO THE RIVERWATCH RANGERS OF THIS CITY who went out on the roads and did not come home. Ever vigilant. Ever remembered.\"", "A heavy bronze plaque with a border of linked rings. You could {cmd}read{/} it.");
        AddDetail("hale plaque,hale,brannoch,adventurer plaque", "A bronze plaque set a little apart, with a small worn sword engraved at the head. You could {cmd}read{/} it.");
        AddReadable("hale plaque,hale,brannoch,adventurer plaque", "\"IN MEMORY OF BRANNOCH HALE, adventurer, born in Oakenfell, remembered for his Vigor, who walked every road in Greenvale and came home by the last of them. Set here by his company.\"", "A bronze plaque with a small worn sword engraved at the head. You could {cmd}read{/} it.");
        AddDetail("markers,marker,historical markers,slate,slabs", "Two tall slabs of grey slate stand beside the path, each carved with a heading and several lines of text. You could {cmd}read founding marker{/} or {cmd}read burning marker{/}.");
        AddReadable("markers,marker,historical markers,slate,slabs", "Two slate markers stand beside the path. The founding marker tells how the Great Oak came to be a landmark and a village grew beneath it. The burning marker tells of the storm and what the settlers did after. Read whichever you like.", "Two tall slabs of slate stand beside the path. You could {cmd}read{/} the markers.");
        AddDetail("founding marker,first marker,founding,oak marker", "A tall slab of grey slate, lichened at the foot, carved with a heading and a few lines of text. You could {cmd}read{/} it.");
        AddReadable("founding marker,first marker,founding,oak marker", "\"THE GREAT OAK\nNearly four hundred years ago a company of settlers made their camp on the bank of the River Mere, beside an oak that dwarfed every other tree in Greenvale and could be seen for miles across the country. They called it the Great Oak. It was their landmark and their meeting-place, and beneath it they felt safe. A village grew in its shade, and the tree stood at the heart of it for generations.\nSet here by the Caretakers of the Grove.\"", "A tall slab of grey slate, carved with a heading and several lines. You could {cmd}read{/} it.");
        AddDetail("burning marker,second marker,storm marker,storm,burning,motto", "A tall slab of grey slate, its lower part blackened as if by smoke long ago, carved with a heading, several lines of text and a single line at the foot. You could {cmd}read{/} it.");
        AddReadable("burning marker,second marker,storm marker,storm,burning,motto", "\"THE STORM AND THE BURNING\nIn a terrible storm, generations after the founding, lightning struck the Great Oak. It burned for three days and three nights, and on the third day it fell. The settlers, who had steered by it, sheltered beneath it and made their vows before it, did not leave. They chose to rebuild around the place where it stood, and they named the town Oakenfell, for the oak that fell.\n\nFROM STRONG ROOTS GROW GREAT THINGS\"", "A tall slab of grey slate with a heading, several lines of text and a motto at the foot. You could {cmd}read{/} it.");
        AddDetail("grove,walls,wall,south", "South, between the yews, the path runs back to the grove. The old walls close in on every other side, and the roots go under them.");
    }

    // Tells the Chronicle that a player has come here. A character is credited once, so every visit may report.
    public override void Init()
    {
        base.Init();
        var who = ThisPlayer();
        if (who == null || !Interactive(who)) return;
        try
        {
            LoadObject("/secure/events").Call<string>("Report", "discover.great_stump", who, new List<ObjectRef> { who }, Self, "great-stump",
                new Dictionary<string, string> { ["name"] = "the Great Stump" });
        }
        catch (MudRuntimeException) { }
    }
}
