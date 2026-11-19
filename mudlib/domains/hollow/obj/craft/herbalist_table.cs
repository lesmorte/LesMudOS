// @inherit /std/station
public class CrHerbalistTable : Station
{
    public override void Create()
    {
        Name = "Herbalist Table";
        Ids.Add("table");
        Ids.Add("herbalist table");
        Ids.Add("herbalist's table");
        Ids.Add("broad table");
        Ids.Add("scrubbed table");
        ShortDesc = "a herbalist table";
        LongDesc = "A broad table of pale pine, scrubbed so often that the grain stands up in ridges. Stains of green, brown and a surprising purple have soaked in anyway, and a stone mortar and brass scales stand ready at one end. This is where salves, tonics, poultices and bandages are made.";
        StationKind = "herbalist table";
    }
}
