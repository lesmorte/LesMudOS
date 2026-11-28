// @inherit /std/item

/// <summary>The keepsake of a juror who declined (docs/specs/bureaucrat.md section 11.6).</summary>
public class JuryCertificate : Item
{
    public override void Create()
    {
        Name = "certificate";
        Ids.Add("certificate");
        Ids.Add("card");
        Ids.Add("certificate of jury service");
        ShortDesc = "a Certificate of Jury Service";
        LongDesc = "A small stamped card with a red border. You could {cmd}read{/} it.";
        Value = 0;
        ReadText = "CERTIFICATE OF JURY SERVICE.\nThe bearer has served. The bearer is exempt from further service for seven years, or until the Chancery forgets, whichever is the longer.";
    }
}
