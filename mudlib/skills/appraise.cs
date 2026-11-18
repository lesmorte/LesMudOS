// @inherit /std/skill

/// <summary>Adventurer skill. Shows an item's exact numbers and what shops pay for it.</summary>
public class AppraiseSkill : Skill
{
    public override void Create()
    {
        Title = "Appraise";
        Command = "appraise";
        Summary = "Shows an item's exact numbers and what shops pay for it. {cmd}appraise{/} ITEM.";
        StaminaCost = 5;
        Recovery = 0.5;
        Target = "item";
    }

    public override void Resolve(ObjectRef user, ObjectRef? target)
    {
        if (target == null) { TellObject(user, "There is nothing to appraise.\n"); return; }
        var value = target.Call<int>("QueryValue");
        var sells = target.Call<bool>("QueryQuestItem") || value <= 0 ? "Shops will not buy it." : $"Shops pay {value / 2} gold for it.";
        TellObject(user, "You turn it over and weigh it up.\n" + (target.Call<string>("Appraisal") ?? "") + sells + "\n");
    }
}
