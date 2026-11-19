// @inherit /std/craft_data

/// <summary>
/// The proficiencies, the words for their ranks, the station kinds and which proficiency and tool each gathering command uses.
/// A new proficiency (Woodcutting, Mining, Fishing ...) is one Proficiency line; a node kind gathered with it is one NodeKind
/// line in a table (see docs/CRAFTING.md). Every character has every proficiency, from 0; there are no profession locks.
/// </summary>
public class CraftProficiencies : CraftData
{
    public override void Create()
    {
        Proficiency("skinning", "Skinning", "gathering", "Taking the hides of beasts.");
        Proficiency("bleeding", "Bleeding", "gathering", "Drawing the blood of fresh kills.");
        Proficiency("harvesting", "Harvesting", "gathering", "Gathering berries, herbs and flowers.");
        Proficiency("cooking", "Cooking", "crafting", "Food and drink, at a cooking fire.");
        Proficiency("herbalism", "Herbalism", "crafting", "Salves, tonics and poultices, at a herbalist table.");
        Proficiency("tanning", "Tanning", "crafting", "Treating hides, at a tanning rack.");
        Proficiency("tinkering", "Tinkering", "crafting", "Utility items and tools, at a workbench.");

        Rank(0, "untrained");
        Rank(10, "novice");
        Rank(25, "apprentice");
        Rank(45, "journeyman");
        Rank(65, "expert");
        Rank(85, "master");
        Rank(100, "grandmaster");

        Station("herbalist table", "Herbalist Table", "herbalism");
        Station("tanning rack", "Tanning Rack", "tanning");
        Station("workbench", "Workbench", "tinkering");
        Station("cooking fire", "Cooking Fire", "cooking");
        Station("scribing desk", "Scribing Desk", "");   // reserved: no recipe uses it at launch

        Gather("skin", "skinning", "skinning knife");
        Gather("bleed", "bleeding", "bleeding kit");
        Gather("harvest", "harvesting", "harvest sickle");
    }
}
