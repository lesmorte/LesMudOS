// @inherit /std/item

/// <summary>Exhibit Two: the mason's receipt (docs/specs/bureaucrat.md section 11.3).</summary>
public class MasonChit : Item
{
    public override void Create()
    {
        Name = "mason's chit";
        Ids.Add("chit");
        Ids.Add("receipt");
        Ids.Add("slip");
        Ids.Add("mason's chit");
        ShortDesc = "a mason's chit";
        LongDesc = "A slip of paper, curled with age. You could {cmd}read{/} it.";
        QuestItem = true;
        Bound = true;
        BoundLine = "It belongs to the court.";
        Value = 0;
        ReadText = "\"Received of Mr G. Pruett, senior, three gold, for moving the great elm two paces east, the old one being in his opinion in the wrong place. Entered. H. Hollis, mason. (Nobody asked for a receipt. This is it.)\"";
    }
}
