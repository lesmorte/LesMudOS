// @inherit /std/item

/// <summary>
/// A crafting material: herbs, berries, honey, cloth. Stackable by default, so a pouch of twenty healing herbs is one object.
/// A material file is a few lines of data (name, short, ids, value, long description). Its material id and categories are not
/// in the file: they are one line of the materials table under /crafting (Material("honey", "/domains/.../honey", "sweetener")),
/// which is what recipes and the matcher read. Any item can be made a material by such a line; this base only adds stacking and
/// the line that tells players it is used in crafting. See docs/CRAFTING.md.
/// </summary>
public class Material : Item
{
    public Material()
    {
        Stackable = true;
    }

    public bool IsMaterial() => true;

    /// <summary>The material id the crafting system knows this object by ("honey", "wolf_skin"), or "" if it is not in the table.</summary>
    public string QueryMaterialId()
    {
        try { return LoadObject("/secure/crafting").Call<string>("MaterialIdOf", Self) ?? ""; }
        catch (MudRuntimeException) { return ""; }
    }

    public override string Long() => base.Long() + (QueryMaterialId() != "" ? "It is used in crafting.\n" : "");
}
