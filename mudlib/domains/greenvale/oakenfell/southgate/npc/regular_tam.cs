// @inherit /std/npc

public class SgRegularWick : Npc
{
    public override void Create()
    {
        Name = "Old Tam";
        Ids.Add("tam");
        Ids.Add("old tam");
        Ids.Add("regular");
        Ids.Add("old man");
        Ids.Add("oldest regular");
        Ids.Add("tam tanner");
        ShortDesc = "Old Tam, the oldest regular";
        LongDesc = "A very old man with a white beard, a bent back and a stick propped on the bench beside him. His eyes are bright and sharp, and his hands tremble only when they hold nothing.";
        Level = 2;
        MaxHp = 20;

        AddChat("I was drinking in this room before most of you were born, and I'll be drinking here when you've all been buried.");
        AddChat("They say the Great Oak was bigger than the Hall of Legends. Nonsense. It was bigger than the Hall, the Square and the Row put together.");
        AddChat("The old city had better beer, and worse manners.");
        AddTopic("oak,great oak,tree,stump", "The Great Oak burned for three days, lad, and I knew a man whose grandfather watched it. The wood went to ash, but the stump... the stump's still standing, somewhere behind the old walls of the Founder's District. Where the old houses crowd up against ivy, maybe. Look where nobody looks.");
        AddTopic("founders,founder's district,old walls,walls", "Oldest part of the city. Quiet streets, old stones and old families. Go up Old Stone Street, past the Square, and mind the lanes that end in walls.");
        AddTopic("history,city,oakenfell,old days", "Four hundred years, near enough. Settlers by the river, and a tree to meet under. Don't let the Assembly tell you it was all their doing.");
        AddTopic("rumor,rumors,gossip", "Dessa knows the roads and Fenn knows the offices. I know the old things, and old things don't change. Buy me a drink and I'll think of one.");
        AddEmote("Old Tam stares into the fire, lips moving as if counting something.", 8);
        AddEmote("Old Tam taps his stick on the floor and mutters, \"Not like that, never like that.\"", 6);
    }
}
