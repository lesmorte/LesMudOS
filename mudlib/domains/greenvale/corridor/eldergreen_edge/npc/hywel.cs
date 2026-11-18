// @inherit /std/npc

public class EeHywel : Npc
{
    public override void Create()
    {
        Name = "Hywel";
        Ids.Add("hywel");
        Ids.Add("ranger");
        Ids.Add("old ranger");
        Ids.Add("veteran");
        Ids.Add("riverwatch ranger");
        ShortDesc = "Hywel, a veteran Riverwatch ranger";
        LongDesc = "A lean, stooped man in a faded green cloak, with a grey beard braided to his chest and a longbow older than most of the cadets. The silver eye at his collar has worn smooth, and he has the unhurried manner of a man who has outlasted a great many worse mornings.";
        Level = 7;
        MaxHp = 75;

        AddChat("Thirty years in this wood. It still sounds different every dawn.");
        AddChat("I knew a man who tried to count the oaks between here and the Green Gate. He lost count at four hundred and went home.");
        AddChat("Listen to the birds. When they stop, you stop.");
        AddTopic("wood,forest,eldergreen,trees", "The Eldergreen is an old wood. Walk far enough in and the trees change, and so does the air. I'd rather sit here and watch it than go in. Not cowardice. Experience.");
        AddTopic("wolves,wolf,tracks,pack", "A wolf will follow you for an hour before it decides. Look back sometimes. If it ducks down, you know.");
        AddTopic("birds,silence,quiet,listen", "When the jays stop calling, something is moving that's bigger than a deer. When the finches stop, something is watching. When everything stops, run.");
        AddTopic("rangers,riverwatch,order,creed", "I joined at sixteen, and I'll die in the green cloak, if the wood lets me. The order's smaller than it was. We're stubborn, but not many.");
        AddTopic("charcoal,burners,clearing", "Good folk, the burners. They keep the road clear of brush and see everything that passes. Ask them what they saw.");
        AddTopic("narrows,ambush", "The Narrows is the worst stretch for a carter. Trees close, bank on one side, drop on the other. We watch it more than any other mile.");
        AddTopic("morwenna,hunter", "I taught her to read a track. She was better than me by the end. If she's gone quiet, she's gone somewhere she chose, and she thought she could get back.");
        AddTopic("pact,thorn pact,oak,marks", "I've seen the marks. I've never seen who makes them. Thirty years, never once. That tells you something, but I'll not tell you what.");
        AddEmote("Hywel strings and unstrings his bow, checking the draw.", 6);
        AddEmote("Hywel stares into the trees for a long moment, then looks away.", 6);
        AddEmote("Hywel rubs his bad knee and mutters about weather.", 6);
    }
}
