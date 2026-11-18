// @inherit /std/npc

public class FdTownCrier : Npc
{
    public override void Create()
    {
        Name = "Edric Voss";
        Ids.Add("edric");
        Ids.Add("voss");
        Ids.Add("crier");
        Ids.Add("town crier");
        ShortDesc = "Edric Voss, the town crier";
        LongDesc = "A barrel-chested man in a green coat with brass buttons and a tall hat with an oak leaf pinned to it. A handbell hangs from his belt on a leather thong, and his face has the permanently startled look of someone who has been shouting for thirty years.";
        Level = 2;
        MaxHp = 25;

        AddChat("Hear ye, hear ye! No, that is only my greeting. How may I help you, friend?");
        AddChat("If it matters in Oakenfell, I have cried it. If it does not matter, I have cried it twice.");
        AddChat("Mind the speaker's stone. The last man who stood on it for a toast stayed for the whole afternoon.");
        AddTopic("assembly,greenvale assembly,delegates", "The Assembly sits at the ninth bell on the first day of each week, in the hall across the square. The gallery is open to any who will keep quiet. Anything beyond watching is for those whom the Assembly knows, and I cry only what they permit me to cry.");
        AddTopic("hall of legends,legends,hall,guild row", "Go west from the square onto Guild Row. The Hall of Legends is the great tangle of timber and stone at the end, you cannot miss it. That is where adventurers are made into something better, or so they say there.");
        AddTopic("market,stalls,greenvale market", "South of the square, down the North Rows, and everything you could want is there, and several things you do not know you want. Mind your purse; the pickpockets there are almost as old as the stalls.");
        AddTopic("news,notice,notices,board,feast", "The board has the notices, but I have the news. The Feast of the Three Days is near, the western road is not to be trusted past the second milestone, and the Fish Market has lost a hound. Hear ye!");
        AddEmote("Edric Voss rings his handbell and bellows: \"Hear ye, hear ye! The Greenvale Assembly sits at the ninth bell, and the public gallery is open to all who can hold their tongues!\"", 6);
        AddEmote("Edric Voss clangs his handbell. \"Hear ye! A brindled hound, one white ear, answers to Biscuit, lost on Mere Bridge. Two gold to the finder!\"", 5);
        AddEmote("Edric Voss mounts the speaker's stone, clears his throat, and cries: \"Hear ye! The Westwood Road is no longer watched past the second milestone! Travel in company or not at all!\"", 5);
        AddEmote("Edric Voss mops his brow with a green kerchief and rings the bell to see if it still works.", 5);
    }
}
