// @inherit /std/npc

public class OaRanger : Npc
{
    public override void Create()
    {
        Name = "Constance Ardley";
        Ids.Add("ranger");
        Ids.Add("constance");
        Ids.Add("ardley");
        Ids.Add("constance ardley");
        Ids.Add("riverwatch ranger");
        Ids.Add("riverwatch");
        ShortDesc = "Constance Ardley, a Riverwatch ranger";
        LongDesc = "A tall, weathered woman in a green-grey cloak with a longbow slung across her back and a rider's boots worn pale at the toe. A silver badge in the shape of an eye is pinned at her collar, and her gaze never rests in one place for long.";
        Level = 7;
        MaxVitality = 70;

        AddChat("The road's quiet today. I like it quiet, but I don't trust it.");
        AddChat("Ever vigilant, that's the Riverwatch's word. Mostly it means standing here looking at things.");
        AddChat("If you're bound for Oakenfell, keep your coin close at the gate and your wits closer.");
        AddTopic("road,roads,patrol,watch", "We walk the roads from the Hollow to Watchfort Crossing, and on the quiet days I count it a victory. From here to the Toll House the road is ours. West of it, I'd not answer for you.");
        AddTopic("riverwatch,rangers,ranger", "We keep the roads safe, escort what needs escorting and hunt what needs hunting. Our headquarters is at Watchfort Crossing, far to the east, but we keep an office in Oakenfell by the quay if you want to speak to the sergeant.");
        AddTopic("oakenfell,city,ahead,rise", "Largest city in Greenvale, and you'll see it from the Rise, a little farther on. Be civil, keep to the law, and ask the watch if you're lost; most of them are decent. For work and company, try the Silver Stag Inn, just past Southgate.");
        AddTopic("bandits,bandit,camp,leader,highwaymen", "There's a camp in the western woods, behind the old arches, with a man at the head of it who has taken the measure of us. We know where it is; we do not have the strength to go in. If you've the stomach and a good blade, it is not a thing I'd forbid you.");
        AddTopic("aqueduct,arches,old arches,ruin", "The Old Aqueduct is older than the city by a long way. Oakenfell did not build it and does not use it, and I've never met a mason who could tell you who did.");
        AddTopic("panther,cat,beast,ledge", "There's a big cat on the rocks above the old cistern. Not many go up there. The ones who did are why I tell you.");
        AddTopic("mere,river,bend", "The Mere's good water and a good road for the barges. Rivermen are a curious breed; they'd tell you the river listens.");
        AddTopic("goblins,goblin,veterans,den", "There is a den of old goblin soldiers in the woods beyond the hamlet's back fields. They're disciplined, which is the worst thing about them. The farmers don't go west of the last hedge, and neither should you without good reason.");
        AddTopic("grace,graceful,quiet foot,aim,steady,tracking,archery", "A ranger lives on Grace: the quiet foot, the steady draw, the shot that goes where the eye went. I have seen stronger people than me fall short with a bow because they could not hold still. Grace comes of doing it a thousand times badly.");
        AddEmote("Constance Ardley shades her eyes and studies the far hills.", 8);
        AddEmote("The ranger checks the pennants on the signal post and nods to herself.", 6);
    }
}
