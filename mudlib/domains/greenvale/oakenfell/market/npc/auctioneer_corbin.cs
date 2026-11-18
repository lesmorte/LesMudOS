// @inherit /std/npc

public class MkAuctioneerCorbin : Npc
{
    public override void Create()
    {
        Name = "Corbin Pyke";
        Ids.Add("corbin");
        Ids.Add("corbin pyke");
        Ids.Add("auctioneer");
        ShortDesc = "Corbin Pyke, the auctioneer";
        LongDesc = "A stout, florid man in a plum coat with a lace cravat and a voice like a brass trumpet. He carries his gavel everywhere, tucked under his arm like a cane.";
        Level = 2;
        MaxHp = 25;

        AddChat("Going once! Going twice! Ah, but no one's here. Habit, friend.");
        AddChat("Between sales, I practice. A good chant is worth a hundred bidders.");
        AddChat("Sold! To the gentleman in the corner. Who is a shadow. Still, sold.");
        AddTopic("auction,auctions,sale,sales,bid,bidding", "The auction house is between sales, and not yet open to the public. When the books are opened, everything from rings to racehorses will go under my gavel.");
        AddTopic("lots,goods,sell,consign", "Consignments will be taken when sales open. Until then I rehearse.");
        AddTopic("gavel,hammer", "My gavel is eighty years old and has sold half of Greenvale. Do not touch it.");
        AddTopic("best,finest,famous", "The best sale? A signet ring, ten years ago. Eight bidders, three fistfights, and a duel. I sold it twice.");
        AddEmote("Corbin Pyke raps his gavel on the stand and calls to the empty benches.", 8);
        AddEmote("Corbin Pyke clears his throat and tries a new chant.", 6);
    }
}
