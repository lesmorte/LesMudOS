// @inherit /std/npc

public class VrRanger : Npc
{
    public override void Create()
    {
        Name = "Ranger Calla Weir";
        Ids.Add("ranger");
        Ids.Add("calla");
        Ids.Add("riverwatch ranger");
        Ids.Add("calla weir");
        Ids.Add("riverwatch");
        ShortDesc = "Calla Weir, a Riverwatch ranger";
        LongDesc = "A lean woman in a green-grey cloak with a longbow slung across her back and a rider's boots worn pale at the toe. A silver badge in the shape of an eye is pinned at her collar, and her gaze never rests in one place for long.";
        Level = 6;
        MaxHp = 60;

        AddChat("The road's quiet today. I like it quiet, but I don't trust it.");
        AddChat("Ever vigilant, that's the Riverwatch's word. Mostly it means standing here looking at things.");
        AddChat("If you're bound for Oakenfell, keep your coin close at the gate and your wits closer.");
        AddTopic("road,roads,patrol,wolf,wolves", "We walk the roads from the Hollow to Watchfort Crossing, and on the quiet days I count it a victory. A wolf has been working the meadows south of here; it's wary but not shy, so mind it.");
        AddTopic("riverwatch,rangers,ranger", "We keep the roads safe, escort what needs escorting and hunt what needs hunting. Our headquarters is at Watchfort Crossing, far to the east, but we keep an office in Oakenfell by the quay if you want to speak to the sergeant.");
        AddTopic("oakenfell,city,ahead", "Largest city in Greenvale, and you'll see it from the Rise. Be civil, keep to the law, and ask the watch if you're lost; most of them are decent. For work and company, try the Silver Stag Inn, just past Southgate.");
        AddTopic("west,western road,elderwood,thorn pact", "There's been trouble on the western road toward the Elderwood: wagons turned back and stories I'd rather not repeat. Some blame the forest, some blame the Pact. I blame no one until I've seen the tracks.");
        AddTopic("mere,river", "The Mere's good water and a good road for the barges. Rivermen are a curious breed; they'd tell you the river listens.");
        AddEmote("Ranger Calla Weir shades her eyes and studies the far hills.", 8);
        AddEmote("The ranger checks the pennants on the signal post and nods to herself.", 6);
    }
}
