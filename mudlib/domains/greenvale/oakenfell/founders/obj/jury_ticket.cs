// @inherit /std/item

/// <summary>A number from the Chancery's ticket machine (docs/specs/bureaucrat.md section 2.4). Harmless; the Registrar takes it when she swears the juror.</summary>
public class JuryTicket : Item
{
    public override void Create()
    {
        Name = "ticket";
        Ids.Add("ticket");
        Ids.Add("number");
        Ids.Add("paper ticket");
        ShortDesc = "a paper ticket";
        LongDesc = "A small paper ticket with a number printed on it. You could {cmd}read{/} it.";
        QuestItem = true;
        Value = 0;
        ReadText = "No. 4. Please hold this ticket until you are called, and for some time afterwards.";
    }

    /// <summary>The number the machine printed.</summary>
    public void SetNumber(int number)
    {
        ReadText = $"No. {number}. Please hold this ticket until you are called, and for some time afterwards.";
        LongDesc = $"A small paper ticket printed with the number {number}. You could {{cmd}}read{{/}} it.";
    }
}
