// @inherit /std/item

/// <summary>A flask of lamp oil: pours into a lantern (the fill command), 30 minutes' worth.</summary>
public class LampOil : Item
{
    public LampOil() { Stackable = true; }

    public override void Create()
    {
        Name = "Flask of Lamp Oil";
        Ids.Add("oil");
        Ids.Add("lamp oil");
        Ids.Add("flask");
        Ids.Add("flask of oil");
        ShortDesc = "a flask of lamp oil";
        Plural = "flasks of lamp oil";
        LongDesc = "A squat stoppered flask of clear lamp oil. Half of it fills a storm lantern. You could {cmd}fill{/} a lantern with it.";
        Value = 4;
    }

    public bool IsLampOil() => true;

    public int QueryOilSeconds() => 1800;

    /// <summary>Use up one flask. Only a lantern being filled may ask.</summary>
    public bool SpendOne()
    {
        var caller = PreviousObject();
        if (caller == null || !caller.Call<bool>("QueryRefillable")) return false;
        UseOne();
        return true;
    }
}
