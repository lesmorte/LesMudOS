// @inherit /std/npc

public class GqTrainerRurik : Npc
{
    public override void Create()
    {
        Name = "Rurik";
        Ids.Add("rurik");
        Ids.Add("trainer");
        Ids.Add("drillmaster");
        Ids.Add("man");
        ShortDesc = "Rurik, a weapons trainer";
        LongDesc = "A heavy, balding man with a voice that carries across the whole yard and a limp that he pretends not to have. He wears a sweat-stained shirt and carries a wooden practice sword that is notched like an old saw.";
        Level = 25;
        MaxHp = 160;

        AddChat("Feet! Feet, I said! A fighter without feet is a statue with opinions.");
        AddChat("Practice is not what you do before the real thing. Practice is most of the real thing. The fighting is just the exam.");
        AddChat("Formal training will open later, traveler. For now, you may watch, and use the posts if you have the arm for it.");
        AddTopic("training,train,trainer,lessons,teach,learn", "Formal training is not yet open. When it is, there will be a notice, and I shall be at the front of the line to give it. For now, watch and learn.");
        AddTopic("practice,drill,drills,posts,post", "Strike the post. Strike it again. When you are bored, strike it a hundred more. That is the first lesson, the second, and the hundredth.");
        AddTopic("weapons,sword,swords,mace,staff", "A weapon is only as good as the arm that swings it, and the arm is only as good as the legs beneath it. Start with the legs.");
        AddTopic("wren,archery,bow", "Wren? Fine trainer. Knows more about a bow than most know about breakfast. Over there.");
        AddEmote("Rurik thumps a post with the flat of his practice sword and shouts a correction.", 10);
        AddEmote("Rurik rubs his bad knee, catches himself doing it, and stands straighter.", 7);
    }
}
