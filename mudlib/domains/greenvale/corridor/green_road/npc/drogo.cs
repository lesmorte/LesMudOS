// @inherit /std/npc

public class GrDrogo : Npc
{
    public override void Create()
    {
        Name = "Drogo";
        Ids.Add("drogo");
        Ids.Add("sellsword");
        Ids.Add("mercenary");
        Ids.Add("blade");
        Ids.Add("hired blade");
        Ids.Add("free company");
        ShortDesc = "Drogo, a hired blade of the Free Companies";
        LongDesc = "A lean, scarred man with a sellsword's easy slouch, a long sword across his knees and a dull red company badge stitched to his sleeve. He is cleaning a whetstone with great care, and he smiles with the lower half of his face only.";
        Level = 5;
        MaxHp = 60;

        AddChat("Coin before crown, friend. That is the only creed I trust.");
        AddChat("Hired to the end of the run, and not a mile more. I have a name for finishing the job I am paid for.");
        AddChat("Nobody pays to hear about the wood. They pay to be told it is safe.");
        AddTopic("free companies,company,companies,badge,mercenary", "The Free Companies. Sellswords and bounty hunters with a code of sorts, the main item being that you are paid. Some are noble and some are little better than the bandits, and the badge will not tell you which.");
        AddTopic("coin before crown,motto,creed", "It means I serve who pays. It also means I am not sworn to lords, thrones or Assemblies, and I sleep well on it.");
        AddTopic("goblins,raiders,wood,forest,deep wood", "Goblins are cowards in the open and brave in the dark. If you must go off the road, go as a pack and go in daylight. Better still, do not.");
        AddTopic("rogue bandits,bandits,lookout", "A bandit with a lookout is a bandit with patience. The kind who sends a boy to count your guards, then sends his friends.");
        AddTopic("work,job,hire,pay,contract", "Hereward has the hiring. I am a hired blade. Ask him, and bring a letter from someone he trusts.");
        AddTopic("rangers,riverwatch,watchtower", "The Rangers do honest work and do not pay for it. The tower on the knoll was theirs once. Whatever they left in it, they left on purpose.");
        AddEmote("Drogo draws the whetstone down his blade in one long, slow stroke.", 6);
        AddEmote("Drogo tosses a coin, catches it and tucks it away without looking.", 6);
        AddEmote("Drogo watches the dark and does not seem worried by it.", 6);
    }
}
