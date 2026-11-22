// @inherit /std/room

/// <summary>The Proving Yard: trainers at their drills.</summary>
public class GqProvingYard : Room
{
    public override void Create()
    {
        ShortDesc = "The Proving Yard";
        LongDesc = "The Proving Yard is a long, level court of packed earth, scuffed pale by generations of boots and enclosed on three sides by a low wall with a rail for spectators. A row of stout wooden posts runs down its middle, each scarred and notched, while racks of blunted practice weapons line the walls. Trainers at their drills call out in short, sharp voices, and apprentices and veterans alike sweat in the sun. The courtyard to the north is the way out.";
        Exits["north"] = "/domains/greenvale/oakenfell/guild/crafters_court";

        AddDetail("posts,training posts,row of posts,post,row", "A row of twelve oak posts, each as thick as a man's waist and sunk deep into the earth. Their faces are scarred, notched and splintered by generations of swords, maces and staves, and several have been replaced so often that the old ones are stacked behind the rail as firewood. They are for practice, and they do not fight back.");
        AddDetail("racks,weapons,practice weapons,blunted weapons", "Racks of blunted swords, padded staves, wooden maces and practice bows. Each is tagged with a chalk number, and the tags are re-chalked every morning.");
        AddDetail("earth,ground,yard,court,dirt", "The earth is packed pale and hard, and swept every dawn. In the middle of it, a faint circle has been worn into the ground by countless feet.");
        AddDetail("wall,rail,spectators", "A low wall with a wooden rail along the top, polished by the elbows of those who lean there to watch. A few apprentices are doing so now, and trying to look as if they are not.");
        AddDetail("trainers,trainer,drills", "Trainers move among the posts in their shirt-sleeves, calling corrections and demonstrating strokes. They have the look of people who care very much and show it only by being strict.");
        AddReadable("slate,champions slate,names,yard slate,champions",
            "A slate bolted to the wall, chalked and wiped every season. This season's names are:\n" +
            "  \"For Might: Hanna Stoke, who broke two posts and apologised to both.\"\n" +
            "  \"For Grace: Pell of the Westwood Road, who put twelve arrows through one ring.\"\n" +
            "  \"For Resolve: a boy from the river, who did the full drill with a fever and told nobody.\"",
            "A chalked slate hangs by the wall. You could {cmd}read{/} it.");
        AddReadable("board,notice,notices,sign",
            "THE PROVING YARD\n" +
            "The yard is open to all who wish to watch or to practice on the posts. Formal training with the trainers will be opened at a later date. Please return all practice weapons to their racks. Please do not take the posts home.",
            "A small painted sign hangs by the rail. You could {cmd}read{/} it.");

        AddSpawn("/domains/greenvale/oakenfell/guild/npc/trainer_rurik");
        AddSpawn("/domains/greenvale/oakenfell/guild/npc/trainer_wren");
    }
}
