// @inherit /std/armor

public class GuildHelm : Armor
{
    public override void Create()
    {
        Name = "Guild Initiate's Helm";
        Ids.Add("helm");
        Ids.Add("helmet");
        Ids.Add("guild helm");
        Ids.Add("initiate's helm");
        Ids.Add("guild initiate's helm");
        ShortDesc = "a Guild Initiate's helm";
        LongDesc = "A sturdy iron cap with a nose guard, stamped with the guild's sword-and-banner mark. It is awarded to those who have proved themselves.";
        ArmorValue = 1;
        Slot = "head";
        ReactionMod = 0.0;
        Category = "light";
        Value = 15;
    }
}
