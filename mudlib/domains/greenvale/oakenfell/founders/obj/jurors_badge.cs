// @inherit /std/item

/// <summary>A juror's badge: a loan from the court, asked for back by the Registrar. The key of the east door of the Chancery's waiting room.</summary>
public class JurorsBadge : Item
{
    public override void Create()
    {
        Name = "juror's badge";
        Ids.Add("badge");
        Ids.Add("jurors badge");
        Ids.Add("juror's badge");
        ShortDesc = "a juror's badge";
        LongDesc = "A small brass badge stamped JUROR, with a twelve in the corner. It is a loan from the court and will be asked for back.";
        QuestItem = true;
        Bound = true;
        BoundLine = "It belongs to the court.";
        Value = 0;
    }
}
