// @inherit /std/npc

public class OaDroverLorcan : Npc
{
    public override void Create()
    {
        Name = "Lorcan Pike";
        Ids.Add("lorcan");
        Ids.Add("pike");
        Ids.Add("lorcan pike");
        Ids.Add("drover");
        Ids.Add("old drover");
        Ids.Add("mule driver");
        ShortDesc = "Lorcan Pike, a drover";
        LongDesc = "A grizzled, bandy-legged drover of fifty with a pipe clamped in his teeth, a wide hat and the leathery face of a man who has slept under more wagons than roofs. A string of pack mules stands behind him, nose to tail.";
        Level = 3;
        MaxVitality = 35;

        AddChat("Mules or oxen, it's all the same. They know every road better than you do.");
        AddChat("Thirty years on the road, and the best thing I ever learned was when to turn round.");
        AddChat("You'll want to see the Rise, lad. First time on the road, the Rise is worth the whole walk.");
        AddTopic("rise,view,city,oakenfell,sight", "North of the hamlet, over the hill. First sight is the best sight, and no one's ever told me otherwise. I've seen it a hundred times and it still gets me.");
        AddTopic("mules,mule,pack,string", "Six mules, and every one a rogue. This one bites, that one kicks, the grey one just stares at you. But they get me there.");
        AddTopic("road,roads,patrol,rangers,riverwatch", "The Rangers keep the pavement, but no further. Stay on it, lad, and you'll be as safe as anyone gets.");
        AddTopic("bandits,bandit,camp,leader,boss", "There's a camp west of the road, past the arches, with a big man at the head who knows what he's about. Never seen him, myself. The men who have don't speak of it.");
        AddTopic("goblins,goblin,veterans,soldiers,den", "Old goblins, soldiers once, my father said, in somebody's army. Discharged and not asked back. They live out in the western woods, and they keep a drill. Don't go looking.");
        AddTopic("aqueduct,arches,old arches", "Older than anything. My gran said it ran to somewhere else once. That the water never came to the city at all.");
        AddEmote("Lorcan Pike knocks out his pipe on a wagon wheel and relights it.", 8);
        AddEmote("The old drover pats the nearest mule and tells it something rude.", 8);
    }
}
