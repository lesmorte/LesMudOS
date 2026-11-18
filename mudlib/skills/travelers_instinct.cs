// @inherit /std/trait

/// <summary>Adventurer trait: the exits line of a room also shows where each exit leads, and marks locked ones.</summary>
public class TravelersInstinctTrait : Trait
{
    public override void Create()
    {
        Title = "Traveler's Instinct";
        Summary = "The exits line of a room also shows where each exit leads, and marks locked ones.";
        Mods["exit_detail"] = 1;
    }
}
