// @inherit /std/item

public class OverseersOrders : Item
{
    public override void Create()
    {
        Name = "Overseer's Orders";
        Ids.Add("orders");
        Ids.Add("overseers orders");
        Ids.Add("notes");
        Ids.Add("bundle");
        ShortDesc = "the Overseer's Orders";
        LongDesc = "A bundle of poorly translated notes covered in strange symbols and instructions. You could read it.";
        QuestItem = true;
        ReadText = "You smooth out the notes. Most of it is crude scrawl, but a few lines are legible:\n" +
                   "  \"The Hollow Below\"\n" +
                   "  \"The Key of Vincent\"\n" +
                   "  \"Continue excavation.\"\n" +
                   "  \"The old founder was telling the truth.\"\n" +
                   "  \"The entrance remains sealed.\"\n" +
                   "  \"The Crow demands progress.\"";
    }
}
