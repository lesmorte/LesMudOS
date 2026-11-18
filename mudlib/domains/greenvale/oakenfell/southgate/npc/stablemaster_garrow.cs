// @inherit /std/npc

public class SgStablemasterGarrow : Npc
{
    public override void Create()
    {
        Name = "Garrow Hale";
        Ids.Add("garrow");
        Ids.Add("stablemaster");
        Ids.Add("garrow hale");
        Ids.Add("master of horse");
        ShortDesc = "Garrow Hale, the stablemaster";
        LongDesc = "A bow-legged, weathered man in a leather apron smelling of horse. He has a gentle touch with the animals and a sharp tongue for fools, and his eyes are the pale grey of old iron.";
        Level = 3;
        MaxHp = 40;

        AddChat("Gently there, Marigold. She bites, that one. Always did.");
        AddChat("I've nothing to rent you yet, friend. Mounts are for those the city knows better.");
        AddChat("Horses can tell a good person, and I trust them over most men.");
        AddTopic("horses,horse,mounts,mount,ride,riding", "Mounts are for those the city knows better. Come back when your name's known in Oakenfell, and we'll see.");
        AddTopic("stabling,stable,board", "I board horses for travelers by the night. You don't have one yet, so you don't need me.");
        AddTopic("marigold,thunder,nell,duke", "Marigold bites, Thunder sleeps, Nell is nearly twenty and wiser than I am, and Duke thinks he's a person.");
        AddTopic("city,oakenfell", "A good city for horses, if you keep to the lanes. A bad city for horses on market day.");
        AddEmote("Garrow Hale rubs a horse's nose and murmurs to it.", 8);
        AddEmote("Garrow Hale forks fresh straw into a stall.", 6);
    }
}
