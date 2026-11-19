// @inherit /std/trainer

public class VhTanner : Trainer
{
    public override void Create()
    {
        Name = "Dunstan Skell";
        Ids.Add("dunstan");
        Ids.Add("skell");
        Ids.Add("tanner");
        Ids.Add("man");
        ShortDesc = "Dunstan Skell the tanner";
        LongDesc = "A lean, long-armed man with his sleeves rolled to the shoulder and hands stained the colour of tea. He has stopped noticing the smell, and he stands close to you anyway, quite cheerfully.";
        Level = 2;
        MaxHp = 22;

        AddChat("You get used to the smell. Everyone says that, and everyone is lying, but I do mean it kindly.");
        AddChat("Bring a hide in fresh and I'll make you something that lasts. Bring it in rotten and I'll make you a lesson.");
        AddChat("They put me out past the square so the tavern wouldn't have to smell it. I've never been so well rested.");
        AddTopic("tanner,tanning,tannery,trade,leather,hides,hide", "A hide is just a skin that hasn't made up its mind. I scrape it, soak it, work it, and in the end it's leather and will keep out the rain for twenty years.");
        AddTopic("rack,tanning rack,frame,station,treatment,scraping", "The rack holds a hide taut while I work it. Leather and hide treatment, that's all it's for, and it's no small thing. Stretch it wrong and you've wasted a wolf.");
        AddTopic("materials,skin,skins,rabbit,wolf,deer,rabbit skin,wolf skin,deer skin,rabbit blood,wolf blood,blood,vats,vat", "Rabbit, wolf and deer skins, mainly. Rabbit's soft, wolf is tough and deer is the best of the three. A little blood in the vat helps the treatment, rabbit or wolf; I never ask where it came from. I sell the skins and that blood, and I buy hides too. {cmd}browse{/} to see them.");
        AddTopic("work,craft,learn,teach,teaching,recipe,recipes,apprentice,lessons,lesson,shop,sell,stock,prices,knife,kit,bleeding kit,skinning knife,tools", "I've sorted out the vats, and I'll teach you one thing: treated leather, a rabbit skin cured with wolf blood on my rack. Type {cmd}lessons{/} for the price and {cmd}learn treated leather{/} to be taught, then {cmd}craft treated leather{/} at the rack. I sell skins, rabbit and wolf blood, and the tools for taking your own: a skinning knife and a bleeding kit. {cmd}browse{/} will show you. Carry the tool, don't wield it, and {cmd}skin corpse{/} or {cmd}bleed corpse{/} when something is dead.");
        AddTopic("smell,stink,tavern,harland", "Harland pretends he doesn't mind it. He stands upwind and holds his breath when he comes by for the boots.");
        AddTopic("village,hollow,guard,wolves,road", "A wolf's been seen on the North Road, and that's good news for me and bad news for whoever meets it. I hear the guard is keeping watch.");
        AddLesson("treated_leather", 8);

        AddStock("/std/skin?p.kind=rabbit", 3);
        AddStock("/std/skin?p.kind=wolf", 6);
        AddStock("/std/skin?p.kind=deer", 6);
        AddStock("/std/blood?p.kind=rabbit", 2);
        AddStock("/std/blood?p.kind=wolf", 4);
        AddStock("/domains/hollow/obj/craft/skinning_knife", 6);
        AddStock("/domains/hollow/obj/craft/bleeding_kit", 8);
        AddEmote("Dunstan drags a scraper down a stretched hide with a long, wet rasp.", 8);
        AddEmote("Dunstan prods a vat with a pole, nods, and wipes his hands on his apron.", 5);
    }
}
