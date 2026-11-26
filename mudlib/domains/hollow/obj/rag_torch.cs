// @inherit /std/light_source

public class RagTorch : LightSource
{
    public override void Create()
    {
        Name = "Rag Torch";
        Ids.Add("torch");
        Ids.Add("rag torch");
        Ids.Add("rag");
        ShortDesc = "a rag torch";
        LongDesc = "A strip of cloth wound tight round a stick and soaked in tallow. It gives a poor, smoky light for ten minutes. You could {cmd}light{/} it.";
        LightValue = 1;
        Capacity = 600;
        WarnAt = 30;
        Noun = "torch";
        Lantern = false;
        Value = 0;
    }
}
