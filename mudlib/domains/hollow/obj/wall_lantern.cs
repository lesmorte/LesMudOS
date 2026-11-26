// @inherit /std/wall_light

/// <summary>A storm lantern hung on a hook, a prop or a crate: +1 light, never goes out, cannot be taken.</summary>
public class WallLantern : WallLight
{
    public override void Create()
    {
        Name = "Wall Lantern";
        Ids.Add("lantern");
        Ids.Add("storm lantern");
        Ids.Add("lamp");
        Ids.Add("wall lantern");
        ShortDesc = "a storm lantern on a hook";
        LongDesc = "An old storm lantern burns steadily where someone hung it. It is bolted fast; it is not for taking.";
        HereLine = "A storm lantern hangs from a hook.";
        LightValue = 1;
    }
}
