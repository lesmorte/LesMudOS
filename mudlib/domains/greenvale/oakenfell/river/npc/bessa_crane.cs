// @inherit /std/shopkeeper

public class RvBessaCrane : Shopkeeper
{
    public override void Create()
    {
        Name = "Bessa Crane";
        Ids.Add("bessa");
        Ids.Add("crane");
        Ids.Add("fishmonger");
        Ids.Add("shopkeeper");
        Ids.Add("trader");
        ShortDesc = "Bessa Crane, the fishmonger";
        LongDesc = "A wide, red-armed woman in a scaly leather apron, with a gutting knife on her belt and a voice that carries across the whole market. She weighs fish with one hand and counts coins with the other, and has never been known to get either wrong.";
        Level = 2;
        MaxHp = 30;

        AddChat("Smoked eel, perch pie, fresh from the Mere! Try {cmd}browse{/} and see for yourself.");
        AddChat("I do not haggle and I do not give credit. I do give a very fair price.");
        AddTopic("eel,eels,smoked eel,smoked", "Smoked over alder, a day and a night, and then wrapped in paper. It will keep a week in a pocket and not offend a soul. Three gold, and well worth it. {cmd}buy eel{/}.");
        AddTopic("pie,pies,perch,perch pie", "Perch from this morning's catch, with leeks and cream, in a crust my mother taught me. Five gold, still warm. {cmd}buy pie{/}.");
        AddTopic("fish,salmon,catch,river", "The Mere gives us perch, roach, eel and pike, and a salmon once in a good while. Salmon I sell by the pound when I have it, which is not today.");
        AddTopic("dog,hound,biscuit,lost", "The brindled hound? Talk to Marta. It is her dog and she has been in tears over it all week.");
        AddEmote("Bessa Crane slaps a fish onto the scale and reads the weight with a practiced glance.", 6);
        AddEmote("Bessa Crane bellows over the market: \"Perch pies! Hot perch pies!\"", 6);

        AddStock("/domains/greenvale/oakenfell/river/obj/smoked_eel", 3);
        AddStock("/domains/greenvale/oakenfell/river/obj/perch_pie", 5);
    }
}
