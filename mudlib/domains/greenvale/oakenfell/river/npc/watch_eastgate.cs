// @inherit /std/npc

public class RvWatchEastgate : Npc
{
    public override void Create()
    {
        Name = "Edmund Rask";
        Ids.Add("edmund");
        Ids.Add("rask");
        Ids.Add("watchman");
        Ids.Add("watch");
        Ids.Add("guard");
        ShortDesc = "Edmund Rask, a city watchman";
        LongDesc = "A heavy, calm watchman in a green surcoat marked with a white oak, a halberd in the crook of his arm and a thumb hooked in his belt. He has seen every kind of wagon, and nothing surprises him any more.";
        Level = 3;
        MaxVitality = 40;

        AddChat("Eastgate. Open dawn to dusk. Stay out of the way of the carts.");
        AddChat("That road runs east to the Crossing. Fine, flat, well-patrolled. A traveler could do worse.");
        AddTopic("road,crossing road,east,beyond,watchfort,watchfort crossing", "The Crossing Road runs east to Watchfort Crossing, where the Riverwatch Rangers have their headquarters. It's a good road, but not yet open to travelers. We have no orders to let anyone go through.");
        AddTopic("rangers,riverwatch,patrol,patrols", "The Rangers come and go by this gate, all hours. Green cloaks, quiet voices, mud to the knee. If they stop to speak, listen. They are not a talkative lot.");
        AddTopic("watch,gate,duty,eastgate", "We keep the gate and inspect the carts. Mostly salt fish, wool and grain. Sometimes a smuggler, and then we get to do something.");
        AddEmote("Edmund Rask waves a cart through the arch with a lazy hand.", 6);
        AddEmote("Edmund Rask looks east along the road, then at the sky, then at the road again.", 5);
    }
}
