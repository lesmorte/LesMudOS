// @inherit /std/npc

public class FdWidowPerrin : Npc
{
    public override void Create()
    {
        Name = "Widow Thackeray";
        Ids.Add("thackeray");
        Ids.Add("widow");
        Ids.Add("woman");
        ShortDesc = "Widow Thackeray, sweeping her step";
        LongDesc = "A small, upright old woman in a black dress and a white cap, a stiff-bristled broom in her hands. She sweeps the same three stones with great determination, whether or not they need it.";
        Level = 1;
        MaxVitality = 12;

        AddChat("This step has been in my husband's family for nine generations. It has not once been clean.");
        AddChat("Mind the street, dear. It was here before you and it will be here after, whatever the Assembly says.");
        AddTopic("house,family,step,husband", "My husband's people built it after the Burning. It was their grandfather's house before that, on the same ground. We put it up again out of the same stones.");
        AddTopic("burning,fire,oak,storm", "Three days it burned, my mother said, and her mother said the same. They say the streets were hot for a week. And then they rebuilt. That is what we do here. We rebuild.");
        AddTopic("lane,elder lane,wall", "I never go down there. Not for any reason. It is only that it is quiet, and I like my quiet where I can keep an eye on it.");
        AddEmote("Widow Thackeray sweeps her doorstep in short, furious strokes.", 6);
        AddEmote("Widow Thackeray pauses, leans on the broom and watches the street with sharp eyes.", 5);
    }
}
