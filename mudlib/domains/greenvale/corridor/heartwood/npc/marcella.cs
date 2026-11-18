// @inherit /std/npc

public class HwMarcella : Npc
{
    public override void Create()
    {
        Name = "Marcella";
        Ids.Add("marcella");
        Ids.Add("scholar");
        Ids.Add("woman");
        Ids.Add("athenaeum scholar");
        ShortDesc = "Marcella, a scholar of the Athenaeum";
        LongDesc = "A woman in her thirties in a sensible brown travelling coat, with ink on her cuffs and a drawing board strapped across her knees. She sits on a camp stool before the wall, a satchel of pencils beside her, and looks from the stone to the paper and back with a frown that suggests the two refuse to agree.";
        Level = 3;
        MaxHp = 30;

        AddChat("I have drawn this gateway eleven times and I will draw it again. Each time I find something I got wrong.");
        AddChat("I am from the Athenaeum in Oakenfell. They sent me to see what is here. I have yet to tell them what it is.");
        AddChat("Forgive me if I seem distracted. It is the proportions. They are not ours, and I can't see why they work.");
        AddTopic("ruins,mossgate,wall,gateway,gate", "The wall is a single build, as far as I can see, and the court behind it is of the same hand. Nothing was added or rebuilt. That alone is strange, for anything this old.");
        AddTopic("arch,fallen arch,carving,carvings,pattern,writing,letters", "The pattern repeats in sevens and then varies. That is exactly how writing behaves, or music, or a border made by someone who loved counting. I cannot tell which. I have copied nine hundred strokes and found no stroke that I recognise.");
        AddTopic("old,age,builders,people,who,history,date", "Oakenfell's founding was nearly four hundred years ago, and everything in the Athenaeum agrees on that. This is older. How much older, I cannot say, and no record that I have seen mentions it at all. I wish I could tell you more.");
        AddTopic("stone,masonry,quarry,green,grey", "Not quarried anywhere in Greenvale that I know of. I took a chip, as the Athenaeum asked. It would not burn, would not dissolve in vinegar, and is lighter than it should be. That is all I know.");
        AddTopic("athenaeum,oakenfell,city,sketch,drawings", "The Athenaeum keeps the city's records. They will want my drawings. I would rather bring them an explanation, but I shall have to bring the drawings.");
        AddTopic("danger,bears,goblins,safe,ranger", "The hunter at the memorial looks in on me. The bears keep to the road at dusk, and the goblins come in the mornings. I draw with my back to the wall, and I jump at everything.");
        AddEmote("Marcella erases a line with her thumb, studies the wall, and draws it again.", 10);
        AddEmote("Marcella measures the gateway with a knotted cord, muttering numbers.", 8);
        AddEmote("Marcella sighs and turns a page, and her pencil begins again.", 6);
    }
}
