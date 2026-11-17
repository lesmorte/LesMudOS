// @inherit /std/npc

public class Mara : Npc
{
    public override void Create()
    {
        Name = "Mara";
        Ids.Add("mara");
        Ids.Add("barmaid");
        Ids.Add("mara the barmaid");
        ShortDesc = "Mara the Barmaid";
        LongDesc = "A quick, freckled woman with auburn hair pinned up in a hurry and a bar towel tucked into her belt. She knows the name and the order of every regular in the room.";
        Level = 1;
        MaxHp = 12;

        AddChat("Mind your coin, friend. I've heard the scrabbling from that cellar every night this week, and it isn't mice.");
        AddChat("Hungry or weary? Harland's your man for a bowl of stew or a bed upstairs. Ask him at the bar.");
        AddTopic("cellar,noise,noises,scrabbling,sounds", "Scratching and squealing in the dark, and once a clatter like a dropped shovel. Harland says it's rats. Harland has never once gone down to look.");
        AddTopic("food,bed,room,harland", "Harland keeps the kitchen and the rooms upstairs. Tell him Mara sent you, and he might even believe it.");
        AddEmote("Mara wipes a tankard clean and sets it back on the shelf.", 6);
        AddEmote("Mara tilts her head toward the trapdoor, listening.", 4);
    }
}
