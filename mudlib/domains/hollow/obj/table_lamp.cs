// @inherit /std/wall_light

/// <summary>A black iron lamp on the Overseer's table: +1 light, never goes out, cannot be taken.</summary>
public class TableLamp : WallLight
{
    public override void Create()
    {
        Name = "Iron Lamp";
        Ids.Add("lamp");
        Ids.Add("iron lamp");
        Ids.Add("black iron lamp");
        ShortDesc = "a black iron lamp";
        LongDesc = "A black iron lamp burns on the table among the maps, its wick trimmed square. It is a heavy thing, set in a bracket; it is not for taking.";
        HereLine = "A black iron lamp burns on the table.";
        LightValue = 1;
    }
}
