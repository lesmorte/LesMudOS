// @inherit /std/skin

/// <summary>
/// The blood of any creature, drawn into a vial by bleeding a fresh corpse: one object for every beast, like /std/skin, its kind
/// held as the item property "kind". A beast whose blood the table marks scarce (the goblin) cannot be given that kind by
/// anyone but the crafting daemon (bleeding a real corpse) and a player body restoring its own saved belongings, so no shop,
/// recipe, quest or wizard's object can make Goblin Blood: it comes only from goblins.
/// </summary>
public class Blood : Skin
{
    protected override string Part => "blood";

    protected override bool PropertyAllowed(string key, string value)
    {
        if (key != "kind" || value == "") return true;
        bool scarce;
        try { scarce = LoadObject("/secure/crafting").Call<bool>("QueryScarceBlood", value); }
        catch (MudRuntimeException) { return false; }
        if (!scarce) return true;
        var caller = PreviousObject();
        if (caller == null) return false;
        return (!caller.IsClone && caller.ProgramPath == "/secure/crafting") || (caller.IsClone && caller.ProgramPath == "/std/player");
    }
}
