// @inherit /std/light_source

public class PitchTorch : LightSource
{
    public override void Create()
    {
        Name = "Pitch Torch";
        Ids.Add("torch");
        Ids.Add("pitch torch");
        Ids.Add("pitch");
        ShortDesc = "a pitch torch";
        LongDesc = "A stout ash handle with a head of pitch-soaked tow. It burns steadily for twenty-five minutes. You could {cmd}light{/} it.";
        LightValue = 1;
        Capacity = 1500;
        WarnAt = 60;
        Noun = "torch";
        Lantern = false;
        Value = 4;
    }
}
