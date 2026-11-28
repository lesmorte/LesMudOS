// @inherit /std/item

/// <summary>Exhibit One: the Boundary Roll for Marrow Lane (docs/specs/bureaucrat.md section 11.3).</summary>
public class BoundaryRoll : Item
{
    public override void Create()
    {
        Name = "Boundary Roll";
        Ids.Add("roll");
        Ids.Add("boundary roll");
        Ids.Add("boundary");
        ShortDesc = "a Boundary Roll";
        LongDesc = "A long roll of old paper tied with green tape. You could {cmd}read{/} it.";
        QuestItem = true;
        Bound = true;
        BoundLine = "It belongs to the court.";
        Value = 0;
        ReadText = "BOUNDARY ROLL: MARROW LANE.\n\"The line runs from the great elm, west two paces to the stake, and thence to the pump. Weather: fair. Entered in the year of the late frost. Witness: H. Hollis, mason.\"";
    }
}
