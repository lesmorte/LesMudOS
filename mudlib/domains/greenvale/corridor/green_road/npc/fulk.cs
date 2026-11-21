// @inherit /std/npc

public class GrFulk : Npc
{
    public override void Create()
    {
        Name = "Fulk";
        Ids.Add("fulk");
        Ids.Add("tinker");
        Ids.Add("traveler");
        Ids.Add("lone traveler");
        Ids.Add("man");
        Ids.Add("mender");
        ShortDesc = "Fulk, a lone tinker";
        LongDesc = "A stooped, long-armed man with a pack frame piled with kettles and pans, a wiry grey beard and a battered hat that shades most of his face. He sits on the rim of the trough with a hammer across his knees, and a lame mule grazes by his side.";
        Level = 2;
        MaxVitality = 30;

        AddChat("Waiting for a wagon heading north. I walk alone, but I do not like to walk alone here.");
        AddChat("Kettles mended, pans patched. Pay in coin, or in company.");
        AddChat("A tinker hears every road. Mine says that the Green Road has been quieter than it should be.");
        AddTopic("wagon,wagons,wait,waiting,company,north", "A tinker alone is a sack of copper for a goblin to carry off. I go with the first wagon that will have me, and the first one is usually Hereward's. He lets me trail at the back, for a pot mended.");
        AddTopic("mule,beast,animal,lame", "Old Biddy. Went lame on the causeway. She will carry on once the stone bruise heals, or she will not, and I will have a pan of mule stew.");
        AddTopic("trough,spring,water", "Sweet water. The only sweet water between here and the Dark Hollow. I fill my kettles and I do not go thirsty.");
        AddTopic("goblins,raiders,camp,deep wood", "I mended a goblin's cooking pot once, in the Hollow, years ago. He paid me with a silver spoon, and I never saw him again. I do not know if I was lucky.");
        AddTopic("shrine,melisande,keeper", "The shrine keeper is a good soul. I mend her lamp-flask, and she pours me water. A fair trade.");
        AddTopic("west,path,cordwood,bear,bears", "Do not take the west path. The bear trees are down there. I have seen what is left of the last man.");
        AddEmote("Fulk taps a dent out of a kettle with small, precise blows.", 6);
        AddEmote("Fulk looks up the road and down the road and back to his kettle.", 6);
        AddEmote("The mule shifts her weight off one hoof and sighs.", 6);
    }
}
