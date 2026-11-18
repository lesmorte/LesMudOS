// @inherit /std/npc

public class EeBerenice : Npc
{
    public override void Create()
    {
        Name = "Berenice";
        Ids.Add("berenice");
        Ids.Add("burner");
        Ids.Add("charcoal burner");
        ShortDesc = "Berenice, a charcoal burner";
        LongDesc = "A broad, black-streaked woman with a scarf tied over her hair and a rake in her hands. Soot has worked into every line of her face, so that only her eyes and teeth are clean, and she smells strongly of woodsmoke.";
        Level = 3;
        MaxHp = 45;

        AddChat("A clamp can't be hurried. Three days of watching, and one lapse spoils the lot.");
        AddChat("Don't touch anything black unless I say. It's hotter than it looks.");
        AddChat("Charcoal's what keeps the smiths in Oakenfell, and the smiths keep the Rangers in blades.");
        AddTopic("charcoal,burning,clamp,clamps,mounds", "You stack the billets in a dome, cover it with turf, light it from the middle and then you do not let it flame. Slow smoulder, three days, no more. Too much air and it turns to ash, too little and it goes out.");
        AddTopic("cadoc,brother,partner", "My brother, over by the other clamp. He sleeps on the job, I'll not deny it, but he can smell a bad draught from the road.");
        AddTopic("road,north,south,travelers", "Carters pass every day. The Rangers pass twice a day. If you don't see the Rangers, that's what to worry about.");
        AddTopic("wolves,wolf,spiders,beasts", "Wolves smell the smoke and stay off, mostly. The spiders don't mind it. That's why we've a bucket by every clamp.");
        AddTopic("forester,alwin,leave", "Alwin gives us leave to burn what the woodcutters leave behind. Offcuts, windfall, branches too crooked to use. Nothing blazed, ever.");
        AddTopic("birch,birches,trail,east,stand", "The trail east goes through the Birch Stand and on into the deep wood. We don't go that way. It gets quiet.");
        AddEmote("Berenice rakes a vent open and a plume of blue smoke curls out.", 6);
        AddEmote("Berenice wipes her forehead with the back of a sooty hand and leaves a black stripe.", 6);
        AddEmote("Berenice sniffs the smoke and nods, content.", 6);
    }
}
