// @inherit /std/skill

/// <summary>Mage skill. Shows an item's exact numbers and any magical property.</summary>
public class MagicalLoreSkill : Skill
{
    public override void Create()
    {
        Title = "Magical Lore";
        Command = "lore";
        Summary = "Shows an item's exact numbers and any magical property. {cmd}lore{/} ITEM.";
        ManaCost = 5;
        Recovery = 0.5;
        Target = "item";
    }

    public override void Resolve(ObjectRef user, ObjectRef? target)
    {
        if (target == null) { TellObject(user, "There is nothing to study.\n"); return; }
        TellObject(user, "You study it with a trained eye.\n" + (target.Call<string>("Appraisal") ?? "It tells you nothing.\n"));
    }
}
