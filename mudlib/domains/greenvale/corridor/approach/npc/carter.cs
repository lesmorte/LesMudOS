// @inherit /std/npc

public class OaCarter : Npc
{
    public override void Create()
    {
        Name = "Jessamy Wick";
        Ids.Add("jessamy");
        Ids.Add("wick");
        Ids.Add("jessamy wick");
        Ids.Add("carter");
        Ids.Add("cart");
        Ids.Add("carter jessamy");
        ShortDesc = "Jessamy Wick, a carter";
        LongDesc = "A square, freckled young woman in a flour-dusted leather jerkin and a wide straw hat, hefting sacks onto a tall farm cart with the ease of long practice. Her carthorse, a patient grey, wears a ribbon in its forelock.";
        Level = 3;
        MaxVitality = 35;

        AddChat("Careful, there. These are the city's loaves you're leaning on.");
        AddChat("Flour for Oakenfell, every morning. If it rains, flour for Oakenfell, wet.");
        AddChat("Take a lift? Not if you're armed. The horse is nervous.");
        AddTopic("flour,sacks,mill,miller", "The miller's flour goes to the bakeries by the Market District. I take forty sacks a day, and they take the credit.");
        AddTopic("cart,horse,grey,carthorse", "That's Barley. She knows the road without me. Sometimes I think she would rather drive.");
        AddTopic("oakenfell,city,gate,southgate", "I go in by the Southgate and out by the Southgate. The watch know my face, and the bakers know my prices.");
        AddTopic("road,roads,bandits,camp,aqueduct", "I drive in daylight and I stay on the stone. Everybody knows what's out there, and nobody says it.");
        AddTopic("rise,view,sight", "I stop my cart on the Rise every day and look. I don't know why. It always looks different.");
        AddEmote("Jessamy Wick swings a flour sack onto the cart and slaps the dust from her hands.", 8);
        AddEmote("The carter checks Barley's harness and feeds her a crust.", 6);
    }
}
