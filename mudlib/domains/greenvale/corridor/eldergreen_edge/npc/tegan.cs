// @inherit /std/npc

public class EeTegan : Npc
{
    public override void Create()
    {
        Name = "Tegan";
        Ids.Add("tegan");
        Ids.Add("ranger");
        Ids.Add("quartermaster");
        Ids.Add("riverwatch ranger");
        ShortDesc = "Tegan, the Outpost quartermaster";
        LongDesc = "A stout, brisk woman with her sleeves rolled to the elbow and a ledger tucked under one arm, a green cloak slung over a stool beside her. Her hair is pinned up with a quill, her fingers are ink-stained, and she counts everything twice.";
        Level = 5;
        MaxHp = 65;

        AddChat("Supplies are short. They are always short. Don't ask me what's in the store; it's whatever is left.");
        AddChat("If you've killed something off the board, bring me proof. I don't take stories.");
        AddChat("Boots. Everyone wants boots. Nobody asks about the roof.");
        AddTopic("quartermaster,stores,supplies,store,ledger", "I keep the stores, the horses, the roster and the ledger. If it isn't written in my book, it didn't happen. Provisions and arrows go to patrols first. Travelers get what is left, and a kind word.");
        AddTopic("bounties,bounty,board,work,notice", "The board by the gate is mine to keep. Wolves along the road, boar east of it, goblin scouts on the old trail, spiders in the thicket. Bring me proof of a kill and I will mark it in the book. The commander decides what the Rangers owe you, and she is careful with promises.");
        AddTopic("wolves,wolf,pelt,pelts", "Wolf pelts are good. Thick, and they sell in Oakenfell. If you bring me three, I'll have a tale to tell the commander.");
        AddTopic("boar,boars,tusks,tusk", "Boar tusks and hides are useful, and the meat is good. But they hit like a cart, and I won't be stitching you up after.");
        AddTopic("goblin,goblins,scouts,cords", "Goblin scouts carry knotted cords. If you take one, bring it. The commander wants to read the knots.");
        AddTopic("herbs,herb,patch,healing,yarrow", "Yarrow, self-heal, comfrey, watercress. They grow along the road, at the Deer Lawn, and by the stream at the Fern Banks. A good pouch of them stops a wound bleeding.");
        AddTopic("gwenllian,commander", "The commander is fair, and fierce, and tired. She is also right more often than anyone else. Don't waste her time.");
        AddTopic("rangers,riverwatch", "We are the Riverwatch Rangers. Ever Vigilant. What the motto doesn't say is that someone has to count the bandages.");
        AddEmote("Tegan counts a stack of arrows and writes a number in the ledger.", 6);
        AddEmote("Tegan weighs a sack in her hand and frowns at it.", 6);
        AddEmote("Tegan blows on her ink and turns a page.", 6);
    }
}
