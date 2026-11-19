// @inherit /std/craft_data

/// <summary>
/// Every number of the gathering and crafting formulas, in one place. docs/CRAFTING.md explains each one and shows the odds
/// they give; tests/craft_sim.py reads this file. Rows "per quality" run Poor, Common, Fine, Excellent, Masterwork, Legendary.
/// After changing a number, `update /crafting/numbers`; the crafting daemon picks it up within five seconds.
/// </summary>
public class CraftNumbers : CraftData
{
    public override void Create()
    {
        // ---- Harvest Quality Score = base + proficiency + tool modifier - work x difficulty factor + variation ----
        Number("harvest.base", 30);
        Number("harvest.variation", 15);                          // two rolls of 0..15, minus 15: -15..+15, most often near 0
        Numbers("harvest.tool_quality", -10, 0, 5, 10, 15, 20);    // per tool quality
        Number("harvest.difficulty_factor", 0.5);                  // per point of work level
        Numbers("harvest.thresholds", 20, 50, 80, 110);            // the score for Common, Fine, Excellent, Masterwork
        Number("harvest.max_quality", 4);                          // Masterwork: Legendary never comes from harvesting
        // ---- Success: base + proficiency x per point + tool - work x difficulty, between min and max (percent) ----
        Number("harvest.success_base", 75);
        Number("harvest.success_per_point", 0.2);
        Numbers("harvest.success_tool", -10, 0, 5, 10, 15, 20);
        Number("harvest.success_difficulty", 0.4);
        Number("harvest.success_min", 30);
        Number("harvest.success_max", 98);
        // ---- A bonus unit: proficiency x per point + tool, at most max (percent) ----
        Number("harvest.bonus_per_point", 0.15);
        Numbers("harvest.bonus_tool", 0, 0, 5, 10, 15, 20);
        Number("harvest.bonus_max", 40);
        // ---- Work level (0 to 100) of a corpse: beast difficulty x scale + creature level x weight; of a node: difficulty x scale ----
        Number("difficulty.beast_scale", 5);
        Number("difficulty.level_weight", 1);
        Number("difficulty.node_scale", 5);
        Number("bleed.fresh_seconds", 300);                        // bleeding needs a corpse younger than this
        // ---- Growth: a successful piece of work rolls a chance at one whole point, climbing a ladder of chances ----
        Number("growth.near_band", 10);     // work no more than this far below your skill (or above it) is "near": the rung's chance
        Number("growth.easy_band", 30);     // up to this far below: easy_factor of the rung's chance; further below: nothing
        Numbers("growth.ladder", 20, 25, 50, 75, 100);   // percent; a miss moves up a rung, a gain starts again
        Number("growth.repeat_every", 20);  // each rung is used 1 + skill / this times before the next
        Number("growth.easy_factor", 0.5);
        // ---- Crafted quality = average ingredient quality + shift + noise, rounded ----
        Number("craft.shift_per_point", 0.02);   // tiers per point of (proficiency - recipe work level)
        Number("craft.shift_min", -1.5);
        Number("craft.shift_max", 1.0);
        Number("craft.noise", 0.75);             // the noise runs from -noise to +noise, most often near 0
        Number("craft.noise_steps", 20);
        Number("craft.max_quality", 4);          // Masterwork
        Number("craft.bonus_margin", 20);        // a bonus output: (proficiency - work - margin) x per point, at most max (percent)
        Number("craft.bonus_per_point", 0.5);
        Number("craft.bonus_max", 25);
        Number("craft.return_margin", 30);       // an ingredient saved: the same shape
        Number("craft.return_per_point", 0.4);
        Number("craft.return_max", 20);
        Number("recipe.max_ingredients", 3);
        // ---- Time on the combat clock: preparation, recovery, reaction modifier (units of 2 seconds) ----
        Numbers("time.gather", 1.0, 0.5, 0.0);   // the result comes after 2 s; the next action after (Reaction Time + 0.5) x 2 s
        Numbers("time.craft", 1.5, 0.0, -1.0);   // the result comes after 3 s, and the next can begin then
        // ---- What a crafted consumable's effects are multiplied by, per quality ----
        Numbers("effect.quality_scale", 0.6, 1.0, 1.25, 1.5, 1.8, 2.2);
        // ---- Recipe difficulties: the work level each stands for ----
        Difficulty("simple", 0);
        Difficulty("novice", 10);
        Difficulty("apprentice", 25);
        Difficulty("journeyman", 45);
        Difficulty("expert", 65);
        Difficulty("master", 85);
    }
}
