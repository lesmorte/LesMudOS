// @inherit /std/craft_data

/// <summary>
/// The beast table: every creature kind that can be skinned or bled. Each line makes two materials, "{kind}_skin" (/std/skin)
/// and "{kind}_blood" (/std/blood), named "{Name} Skin" and "{Name} Blood", in the categories "skin" or "blood" and the kind
/// itself. A creature yields them by declaring Skin = "wolf" and Blood = "wolf" (public string properties, see BUILDING.md).
/// Columns: kind, display name, skin value, blood value (gold, Common), difficulty (1 easy to 20; x5 is the work level, plus
/// the creature's level). scarceBlood: the blood can come from nothing but bleeding a real corpse (Goblin Blood).
/// </summary>
public class CraftBeasts : CraftData
{
    public override void Create()
    {
        Beast("rabbit", "Rabbit", 3, 2, 1);
        Beast("rat", "Rat", 1, 1, 1);
        Beast("toad", "Toad", 2, 3, 2);
        Beast("bat", "Bat", 1, 2, 2);
        Beast("deer", "Deer", 6, 3, 3);
        Beast("wolf", "Wolf", 6, 4, 4);
        Beast("goblin", "Goblin", 0, 8, 4, scarceBlood: true);
        Beast("boar", "Boar", 7, 4, 5);
        Beast("bear", "Bear", 12, 6, 8);
        Beast("panther", "Panther", 14, 6, 9);
    }
}
