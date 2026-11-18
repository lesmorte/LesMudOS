// @inherit /std/room

public class HwM10 : Room
{
    public override void Create()
    {
        ShortDesc = "The Heartwood Road, the Thinning";
        LongDesc = "The great trunks stand wider apart here, and between them the sky comes down in patches of real light. Bracken and young oak fill the gaps, and the road is dry and pale, with dust on it. Birds are singing again somewhere ahead, small and ordinary, and the air smells of cut wood and, faintly, of smoke. You have walked out of a very old place, and you are aware of it as you would be of leaving a shrine.";
        Exits["south"] = "/domains/greenvale/corridor/heartwood/m09";
        Exits["north"] = "/domains/greenvale/corridor/heartwood/m11";

        AddDetail("sky,light,patches,sunlight,sun", "The sunlight comes down in separate shafts and lies on the road in gold coins. After the green dusk, it feels warm enough to be a gift.");
        AddDetail("bracken,young oak,oak,undergrowth,saplings", "The bracken is waist-high, and among it stand young oaks no thicker than a wrist. They are the first things you have seen in a day that are not older than you can imagine.");
        AddDetail("birds,birdsong,song,singing", "The birds are finches and wrens and a distant cuckoo, small and busy. After the silence of the deep wood, the noise seems rude and welcome.");
        AddDetail("smoke,smell,air,woodsmoke", "The smell of woodsmoke comes down the road from the north, thin and domestic. Somewhere ahead people are burning hearths.");
        AddDetail("road,dust,ground", "The road is wider here and drier, with fresh wheel marks in the dust and the print of a mule. Someone has been clearing deadfall off the verge.");
        AddDetail("blaze,blazes,ring,trunks,stump,stumps", "The trunks at the road's edge carry old Riverwatch blazes, rings cut through the bark and healed over into thick scars. There are also stumps, cut clean with a saw, the first you have seen since the Green Road.");
        AddEncounter("/domains/greenvale/corridor/mob/black_bear", 8);
    }
}
