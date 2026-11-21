// @inherit /std/room

/// <summary>The Armourer's Shed: a lean-to built against the side of the Iron Oak. Oswy Thackett sells padded and leather armour.</summary>
public class VhArmourersShed : Room
{
    public override void Create()
    {
        ShortDesc = "The Armourer's Shed";
        LongDesc = "A lean-to has been propped against the flank of the smithy, and it shares the smithy's warmth through the common wall. Racks of tanned leather, bolts of padding and spools of waxed thread fill it from floor to roof, and three wooden dummies in different stages of dress stand in a row like patient customers. A lamp hangs over a cutting table, and a stool by the door is worn to the shape of whoever sits on it. A low door in the east wall leads back through to the Iron Oak.";
        Exits["smithy"] = "/domains/hollow/village/ironoak";
        Safe = true;

        AddDetail("racks,rack,leather,tanned leather,hides", "Racks of leather in three weights, from supple jerkin hide to stiff, nearly black hide for belts. The best of it came from Dunstan at the tannery, and the smell comes with it.");
        AddDetail("padding,bolts,fleece,flax,wool,thread,spools", "Bolts of quilting and fleece stuffing, and spools of waxed thread in grey, brown and black. A jerkin is mostly wool, in the end, and Oswy is particular about the wool.");
        AddDetail("dummies,dummy,wooden dummies,figures,customers", "Three wooden dummies stand in a row. The first wears only a padded jerkin, the second a leather one, and the third has a leather cap set at a jaunty angle, as if it had been asked a question and enjoyed it.");
        AddDetail("table,cutting table,lamp,shears,awls,awl", "A cutting table scored with a hundred chalk patterns, with shears and awls laid out in order of size. The lamp over it burns steadily, and not a thing on the table is out of place.");
        AddDetail("stool", "A three-legged stool worn smooth in the middle. It is warm, from the smithy wall, and nobody has ever been seen sitting in it.");
        AddDetail("door,low door,east wall,wall,smithy", "A low door through the common wall leads into the Iron Oak, where you can hear Torren's hammer going. Oswy keeps it propped with a boot to let the warmth in.");

        AddSpawn("/domains/hollow/npc/oswy_thackett");
    }
}
