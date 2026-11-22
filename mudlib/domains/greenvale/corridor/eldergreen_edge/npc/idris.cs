// @inherit /std/npc

public class EeIdris : Npc
{
    public override void Create()
    {
        Name = "Idris";
        Ids.Add("idris");
        Ids.Add("ranger");
        Ids.Add("young ranger");
        Ids.Add("scout");
        Ids.Add("riverwatch ranger");
        ShortDesc = "Idris, a young Riverwatch ranger";
        LongDesc = "A lanky young man in a new green cloak that is a little too big for him, with a short bow, a quiver of fresh-fletched arrows and a keen, eager face. The silver eye-badge at his collar has not yet a scratch on it.";
        Level = 5;
        MaxVitality = 60;

        AddChat("Posted here from Watchfort Crossing a month ago. It's quieter than the stories, and louder than I'd like.");
        AddChat("I saw a goblin scout on the east trail last week. A real one. Small, quick, grey as a stone. It looked right at me and vanished.");
        AddChat("Do you think I'll make sergeant? Hywel says I talk too much.");
        AddTopic("goblin,goblins,scout,scouts,goblin scouts", "They're small, grey-skinned and sneaky. They carry cords with knots in them and they count things, wagons and men. Not warriors. Scouts. The question is what they're scouting for, and nobody can answer that.");
        AddTopic("watchfort,watchfort crossing,headquarters,crossing", "Watchfort Crossing, east of Oakenfell. A great fort, with a drill yard and a hall with every Ranger's name carved in it. I miss the food.");
        AddTopic("north,green road,gate,green gate", "I've walked as far as the Green Gate. It's like walking into a cellar. The trees close up and the light goes green. I'm told it gets darker.");
        AddTopic("south,hollow,vincents hollow,farms", "I came up the South Road. It was pleasant. Sheep, farms, a bridge. Then the fences just stopped, and there was the wood.");
        AddTopic("patrol,patrols,route", "Hywel and I walk the Patrol Mile together most days. He sets the pace. He's very slow. I'm not complaining.");
        AddTopic("hunters,camp,hunters camp,hunter", "The hunters at the camp north of the ride? Delwyn and Elowen. Decent sorts. They know the wood better than we do and tell us things. Their friend Morwenna vanished, though.");
        AddTopic("rangers,riverwatch,join,recruit", "You can't just join. You have to be asked, and you have to earn it. Do the road's work and the Rangers will notice. I did, mostly.");
        AddEmote("Idris checks the fletching on an arrow and grins.", 6);
        AddEmote("Idris climbs the watch platform and peers northward.", 6);
        AddEmote("Idris practises drawing his bow, and nearly drops it.", 6);
    }
}
