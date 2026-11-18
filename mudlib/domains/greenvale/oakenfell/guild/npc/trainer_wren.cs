// @inherit /std/npc

public class GqTrainerWren : Npc
{
    public override void Create()
    {
        Name = "Wren";
        Ids.Add("wren");
        Ids.Add("trainer");
        Ids.Add("archery trainer");
        Ids.Add("woman");
        ShortDesc = "Wren, a trainer of archery and footwork";
        LongDesc = "A lithe, quiet young woman with a braid down her back and a bow that is plainly older than she is. She moves among the posts as lightly as a cat, and corrects her students in a voice so soft that they lean in to hear it.";
        Level = 22;
        MaxHp = 120;

        AddChat("Breathe out. Now loose. You see? The arrow knows when you are ready.");
        AddChat("Footwork is everything. A bowman who cannot move is a target with a bow.");
        AddChat("Formal training will open later. Until then, watch the posts, and watch the others. You learn more from what they do wrong.");
        AddTopic("training,train,trainer,lessons,teach,learn", "Not yet, I am afraid. It will open later. When it does, I shall be glad to teach what I can.");
        AddTopic("archery,bow,bows,arrow,arrows,shooting", "Stand still, breathe slowly, and let go before you decide to. That is the whole of it. It is also the work of a lifetime.");
        AddTopic("footwork,feet,movement,step", "Light on the toes, weight forward, never planted. A foot is a promise: it says where you will be.");
        AddTopic("rurik,weapons,sword", "Rurik? Shouts a good deal. Teaches a great deal more. He is the best I know at making people stand correctly.");
        AddEmote("Wren draws and looses an arrow in a single smooth motion.", 9);
        AddEmote("Wren tilts a student's elbow up with two fingers and nods.", 7);
    }
}
