// @inherit /std/npc

public class EeFinnian : Npc
{
    public override void Create()
    {
        Name = "Finnian";
        Ids.Add("finnian");
        Ids.Add("woodcutter");
        Ids.Add("cutter");
        Ids.Add("lumberjack");
        ShortDesc = "Finnian, a woodcutter";
        LongDesc = "A big fair-haired man in a sweat-dark shirt and leather apron, with a felling axe in his hand and sawdust in his hair. His forearms are corded with muscle and his grin is broad and unhurried.";
        Level = 4;
        MaxHp = 60;

        AddChat("Timber! Ha. Never get tired of saying it.");
        AddChat("Alwin blazes, I cut. We've never fallen out, and we've never been rich.");
        AddChat("Oak's slow to grow and quick to burn. Remember that, when you're warming your hands.");
        AddTopic("woodcutting,woodcutter,axe,felling,cutting,work", "You take a notch low on the fall side and cut a deeper one on the other. When she goes, she goes where you aim her. When she doesn't, run.");
        AddTopic("forester,alwin,blaze,blazes", "Alwin blazes the tree and I fell it. He picks them for a reason I can't see. The best trees are the ones he won't let me touch.");
        AddTopic("oak,oaks,timber,logs,wood", "Oakenfell buys everything we send. Beams, ship-knees, gate-posts. Some of these logs are older than the walls they'll go into.");
        AddTopic("sled,ox,oxen,hauling,ride", "The oxen haul the logs out to the road, where the carters take them south or north. My ox is called Biscuit. He is smarter than I am.");
        AddTopic("thistle,thistles,gaps,herbs", "Thistles grow wherever we clear. They're a nuisance, but the bees like them. Take what you like.");
        AddTopic("hunters,camp,hunters camp", "The hunters' camp is north of here, up the ride. They're decent folk. They shoot over my head some days, but they mean no harm.");
        AddTopic("goblins,goblin,wolves,danger", "We hear them in the evenings. We pack up before dusk, and walk back down to the road in a bunch. The ride is safe enough by day.");
        AddEmote("Finnian swings his axe and a chip of oak spins away.", 6);
        AddEmote("Finnian wipes his brow and takes a long drink from a leather bottle.", 6);
        AddEmote("Finnian leans on his axe and whistles a tune.", 6);
    }
}
