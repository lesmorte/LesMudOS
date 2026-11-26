// @inherit /std/light_source

public class StormLantern : LightSource
{
    public override void Create()
    {
        Name = "Storm Lantern";
        Ids.Add("lantern");
        Ids.Add("storm lantern");
        Ids.Add("lamp");
        ShortDesc = "a storm lantern";
        LongDesc = "A squat iron lantern with a horn window and a wire guard, made to be waved about in a gale and not go out. It burns lamp oil: a full reservoir lasts an hour, and a flask of lamp oil fills half of it. You could {cmd}light{/} it, and {cmd}fill{/} it.";
        LightValue = 2;
        Capacity = 3600;
        WarnAt = 120;
        Noun = "lantern";
        Lantern = true;
        Value = 30;
    }
}
