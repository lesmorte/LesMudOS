// @inherit /std/room

public class EeBrokenWaystone : Room
{
    public override void Create()
    {
        ShortDesc = "The Broken Waystone";
        LongDesc = "A tall waystone leans at the roadside here, split from crown to middle by some old frost or lightning, one half still standing and the other lying in the bracken at its foot. The stone is the grey of old iron and furred with lichen, and the road has bent a little to go round it, as if the stone had been there first. Birdsong thins to silence for a breath and then returns. The sun reaches the ground but the air under the great oaks is distinctly cooler.";
        Exits["south"] = "/domains/greenvale/corridor/eldergreen_edge/m04";
        Exits["north"] = "/domains/greenvale/corridor/eldergreen_edge/m06";
        ResetSeconds = 120;

        AddDetail("stone,marker,crack,split", "The waystone is shoulder-high even broken, a single block of grey stone with its face smoothed for lettering. The crack runs jaggedly down it and has been widened by seeds and roots; a small fern grows in the gap.");
        AddDetail("lichen,moss,fern", "The lichen grows in rings, orange and grey, and some of the rings are wider than your hand. They cover more of the inscription than the weather has.");
        AddDetail("fallen half,half,bracken,fallen stone", "The fallen half lies face-up in the bracken, and the letters on it have fared better than those on the standing half, being out of the wind. The break falls through the middle of every line.");
        AddDetail("oaks,trees,roots", "Oaks crowd the stone on the east and west, and one great root has crept up the foot of the standing half as if to hold it upright.");
        AddDetail("birds,birdsong,silence", "The birds fall quiet around the stone, and then take up their calls again a little further off, as though the stone itself were something to be passed in silence.");
        AddDetail("scratch,scratches,spiral,rough hand,rough letters", "Near the base, in letters rougher than the rest and scratched rather than cut, someone has added a line of their own. It is worn and very old.");
        AddReadable("waystone,inscription,letters,writing,inscription stone", "What can be read of the inscription, standing half and fallen half together:\n\n   . . . ED BY THE WARDENS OF . . . . . .\n   FROM THE HOL . . . . . TWELVE MILES\n   TO OAKENF . . . . . . . . DAYS\n   . . . . . . . . . . . . . . .\n   KEEP TO THE ROAD. THE WOOD . . . . .\n\nBelow, in rougher letters that were scratched in later:\n   . . . FOREST REMEMBERS.\n\nA line in the middle has been chiselled away entirely, on purpose, by someone who did not want it read.", "A cracked waystone with a half-legible inscription leans here. You could {cmd}read{/} it.");

        AddEncounter("/domains/greenvale/corridor/mob/forest_wolf", 10);
        AddEncounter("/domains/greenvale/corridor/mob/forest_spider", 5);
    }
}
