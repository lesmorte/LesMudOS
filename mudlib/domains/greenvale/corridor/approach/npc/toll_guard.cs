// @inherit /std/npc

public class OaTollGuard : Npc
{
    public override void Create()
    {
        Name = "Oswin Hale";
        Ids.Add("oswin");
        Ids.Add("hale");
        Ids.Add("oswin hale");
        Ids.Add("guard");
        Ids.Add("assembly guard");
        Ids.Add("tabard");
        ShortDesc = "Oswin Hale, an Assembly guard";
        LongDesc = "A broad, weathered man in a green tabard over a mail shirt, with a spear grounded at his side and a green oak badge on his breast. His eyes move constantly from wagon to wagon to the tree-line, and he looks like a man who has seen the pole raised on trouble before.";
        Level = 5;
        MaxHp = 55;

        AddChat("Keep moving, keep to the paving. Plenty of road ahead.");
        AddChat("I stand here so the toll keeper can count in peace. That is all the Assembly asks of me, and a good deal more than I would ask of myself.");
        AddChat("Adventurers with a pass go through. The rest of you, queue with the wagons.");
        AddTopic("pass,travel pass,adventurers,adventurer", "A Greenvale Travel Pass means you have been vouched for at the Northgate. I am to let you through the pole without a question. Go on, then.");
        AddTopic("bandits,bandit,highwaymen,camp,robbers", "There is a camp somewhere in the west woods. We know it, the Rangers know it, and it has a man at the head of it who is no fool. Not our job to go in. Our job is the pole.");
        AddTopic("assembly,guard,guards,duty", "The Assembly pays me to stand at the pole and be seen. Half of my work is being seen.");
        AddTopic("rangers,riverwatch,ranger", "The Rangers walk the road to the city, and the road is safer for it. There is one at the River Bend, a day-and-night sort of woman. Ever vigilant, they say.");
        AddTopic("aqueduct,arches,old arches", "Old arches. Older than the city, older than the road. My granddad said the Assembly keeps the one over the road standing out of habit.");
        AddTopic("hale,hale's acre,family,farm,name", "Hale's Acre, down by the Hollow? That was ours, my grandmother's people. The well failed and they walked away from it, and the thorn has it now. I have never seen the place. A man can carry a name a long way from the ground it came from.");
        AddEmote("Oswin Hale shifts his spear and scans the hedgerow.", 8);
        AddEmote("The guard waves a wagon through and mutters something about axle grease.", 6);
    }
}
