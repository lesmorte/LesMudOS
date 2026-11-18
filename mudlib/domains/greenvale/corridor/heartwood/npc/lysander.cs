// @inherit /std/npc

public class HwLysander : Npc
{
    public override void Create()
    {
        Name = "Lysander";
        Ids.Add("lysander");
        Ids.Add("traveler");
        Ids.Add("traveller");
        Ids.Add("man");
        Ids.Add("courier");
        ShortDesc = "Lysander, a hurrying traveler";
        LongDesc = "A thin man in a dusty coat with a pack cinched tight across his shoulders and a walking staff in his fist. He is standing on the old paving with one foot already pointed up the road, and he looks over his shoulder at each pause in the wind.";
        Level = 2;
        MaxHp = 25;

        AddChat("I can't stop. I have to be through before dark. Nobody should be in here after dark.");
        AddChat("Walk on the stone if you can. It feels safer, and I don't know why.");
        AddChat("Two days from the Hollow, and I've not seen the sun for half of it.");
        AddTopic("road,north,oakenfell,city", "Oakenfell is a half day north, past the big oak. If I walk fast, I will sleep in a bed tonight. Let me walk.");
        AddTopic("goblins,bears,danger,dark", "Bears at dusk, goblins in the shade. I've seen both, from a distance, and I intend to stay at that distance.");
        AddTopic("paving,stone,slabs", "It's good paving, whoever laid it. I wish they had laid all of it. I'd have been through by now.");
        AddTopic("hunter,memorial,ranger,riverwatch", "The man at the cairn gave me bread, and told me to keep walking. I took his advice, and I am taking it still.");
        AddEmote("Lysander glances back down the road, then at the trees, then at you.", 10);
        AddEmote("Lysander shifts his pack and shuffles from foot to foot on the paving.", 8);
    }
}
