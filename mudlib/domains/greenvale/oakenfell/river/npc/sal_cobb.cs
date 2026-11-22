// @inherit /std/npc

public class RvSalCobb : Npc
{
    public override void Create()
    {
        Name = "Sal Cobb";
        Ids.Add("sal");
        Ids.Add("cobb");
        Ids.Add("net mender");
        Ids.Add("netmender");
        Ids.Add("old woman");
        ShortDesc = "Sal Cobb, a net-mender";
        LongDesc = "A bent, tiny old woman perched on an upturned crate with a net across her knees and a wooden needle flying in her fingers. Her skin is the color and texture of a walnut.";
        Level = 1;
        MaxVitality = 12;

        AddChat("Hush, dear. A net should be mended in silence, or it comes undone.");
        AddChat("My mother mended nets here, and her mother, and when they were not mending nets they were mending husbands.");
        AddTopic("net,nets,mending,needle,knots", "A knot for each trouble, dear. Reef for the river, bowline for the bank, and a sheet bend for any boy who promises to come back.");
        AddTopic("river,mere,fish,fishing", "Fishing was better when I was young. Now the river is full of barges, and the fish have gone to live somewhere quieter.");
        AddTopic("gate,water gate,north,rivergate", "North of here. The wall, and the arch, and the river going out. Folk who go through do not often come back the same way.");
        AddEmote("Sal Cobb draws a knot tight with her teeth and spits out a strand of twine.", 6);
        AddEmote("Sal Cobb hums a tuneless river-song as her needle flies.", 5);
    }
}
