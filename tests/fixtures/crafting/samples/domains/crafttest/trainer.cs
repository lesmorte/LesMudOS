// @inherit /std/trainer
public class CtTrainer : Trainer
{
    public override void Create()
    {
        Name = "Old Marta";
        Ids.Add("marta");
        Ids.Add("trainer");
        ShortDesc = "Old Marta the cook";
        LongDesc = "A cook who teaches.";
        AddLesson("berry_juice", 5);
        AddLesson("stamina_tonic", 0);
    }
}
