// @inherit /std/room

/// <summary>The Strung Yew: Wenna Farrow the bowyer's shop. She sells the improved bow; the Scout's rank trainer and the Aimed Shot belong here later.</summary>
public class VhStrungYew : Room
{
    public override void Create()
    {
        ShortDesc = "The Strung Yew";
        LongDesc = "Staves of yew in every stage of becoming a bow lean against the walls of this narrow shop, from pale green billets to dark, finished limbs. A bench runs the length of one wall, strewn with spokeshaves, gut, hide glue and little pots of wax. Strung bows hang from the rafters on hooks by the hundred, each tagged with a name and a weight in chalk, and in the back a boy is trimming goose feathers into neat, matching fans. The place smells of linseed and curing glue, and a stout woman is plainly in charge of all of it.";
        Exits["out"] = "/domains/hollow/village/fletchers_lane";
        Safe = true;

        AddDetail("staves,stave,billets,limbs,yew,wood", "Yew, cut in winter and dried for years. Each stave has the pale sapwood on its back and the dark heartwood on its belly, which is the whole secret of a yew bow, and Wenna will tell you so whether you ask or not.");
        AddDetail("bench,workbench,spokeshaves,glue,gut,wax,pots", "A scarred bench worn into a shallow trough by the work. Gut for strings hangs in hanks above it, and the glue pot at the end is kept warm over a tiny flame.");
        AddDetail("bows,bow,rafters,hooks,tags,chalk", "Bows of every size hang from the rafters, tagged in chalk with a name and a number. The number is the weight of the draw in pounds, and the names are the people who are going to be sorry they asked for something stronger.");
        AddDetail("boy,feathers,goose feathers,fans,fletching,fletchings", "A boy of about ten sits on an upturned bucket, cutting goose feathers into matched fans with a little curved knife, three to an arrow. He does not look up. His tongue is between his teeth.");

        AddSpawn("/domains/hollow/npc/wenna_farrow");
    }
}
