// @inherit /std/npc

public class VhCraftsman : Npc
{
    public override void Create()
    {
        Name = "Tobin Fettle";
        Ids.Add("tobin");
        Ids.Add("fettle");
        Ids.Add("craftsman");
        Ids.Add("joiner");
        Ids.Add("tinker");
        Ids.Add("man");
        ShortDesc = "Tobin Fettle the craftsman";
        LongDesc = "A stocky, bald man with sawdust on his shoulders and a pencil stub behind each ear. His apron pockets bulge with nails, pegs and string, and he looks at everything you carry as if it might need mending.";
        Level = 2;
        MaxHp = 24;

        AddChat("If it's broken, bring it here. If it isn't broken yet, bring it here and I'll tell you when.");
        AddChat("Joiner by training, tinker by temper, and mender of everything by accident. Mind the chair, it's not finished.");
        AddChat("Benson's gate hinge has been in for a week. He says there's no hurry. There is always a hurry with Benson.");
        AddTopic("craftsman,joiner,tinker,mender,trade,repairs,repair,mending", "I make what the village needs and fix what it breaks: hinges, stools, buckles, pots and carts. A good deal of it is patching. A little of it is proper making, and that's the part I like.");
        AddTopic("workbench,bench,vice,vise,station,tools,tool,utility,utilities", "The bench is where utility items and tools are made. A vice for holding, a lamp for seeing, a good pair of hands for the rest. I've made a hundred things on it, and I'll make a hundred more.");
        AddTopic("materials,cloth,cloth strip,cloth strips,leather,hide,wildflowers,flour,glue,blood ink,ink", "Cloth strips for binding, leather from Dunstan, glue from flour and water if there's nothing better, and now and then wildflowers for a dye. I'm always short of cloth; everyone is.");
        AddTopic("work,craft,learn,teach,teaching,recipe,recipes,apprentice", "I'll teach you what the bench can do and trade for what it makes, once I've set up for it. Soon. I've got to clear a place for you to stand.");
        AddTopic("benson,gate,hinge", "Benson's hinge is the oldest thing I've ever been given to fix. I think he wants it fixed to look the same as it did seventy years ago, and I can't promise that.");
        AddTopic("village,hollow,harland,cellar,door", "Harland's asked me to look at his cellar door. He wouldn't say why. I didn't ask, but I brought a bigger bolt.");
        AddEmote("Tobin squints along the edge of a plank, grunts and reaches for a plane.", 8);
        AddEmote("Tobin pats his pockets, finds a pencil behind his ear and looks pleased.", 5);
    }
}
