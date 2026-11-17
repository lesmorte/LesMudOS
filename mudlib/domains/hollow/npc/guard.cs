// @inherit /std/npc

public class VillageGuard : Npc
{
    public override void Create()
    {
        Name = "Guard";
        Ids.Add("village guard");
        Ids.Add("guard");
        ShortDesc = "the village guard";
        LongDesc = "A square-jawed man in a dented breastplate, with a spear leaning against one shoulder and a tired look in his eyes. He has stood at this gate through more nights than he cares to count.";
        Level = 3;
        MaxHp = 40;

        AddChat("If you're lookin' for glory, ask old Harland at the tavern. The cellar's got troubles no one's managed to solve.");
        AddChat("Some folk say Vincent still watches over us. I just know strange things have been happening underground.");
        AddTopic("cellar,cellars,underground,tunnels", "Rats at first, then goblins came crawling up through the floor. Now the goblins march like soldiers, and that worries me more than any rat.");
        AddTopic("vincent,holloway,founder", "Vincent founded this place, and the gate is named for his stockade. I stand where he would have stood, and I keep my eyes open.");
        AddTopic("harland,tavern,rest,hollowman", "Harland keeps the Hollowman's Rest, and the best ale in the valley. Ask him about the cellar, if you've the stomach for it.");
        AddEmote("The guard shifts his spear to his other shoulder and scans the road to the north.", 6);
    }
}
