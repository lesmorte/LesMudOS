// @inherit /std/npc

public class GqLibrarian : Npc
{
    public override void Create()
    {
        Name = "Hester";
        Ids.Add("hester");
        Ids.Add("librarian");
        Ids.Add("woman");
        ShortDesc = "Hester, the librarian";
        LongDesc = "A small, upright woman in a grey dress with a ring of keys at her belt and a pencil stuck through her bun. She has the gentle, unflinching manner of someone who has quieted a great many rooms, and a dry humour that appears so rarely that people treasure it.";
        Level = 12;
        MaxVitality = 50;

        AddChat("Welcome to the Athenaeum. I am Hester, the librarian. Silence is requested; questions are welcome. They are not the same thing, though many visitors think so.");
        AddChat("There are four short works on the reading table by the door, for newcomers: the founding, the storm, the name and motto, and the five districts. They are the best place to begin. Each is only a few pages.");
        AddChat("The archives below are not open to casual visitors. The shelves above are, so long as you return what you take down.");
        AddTopic("books,book,works,shelves,reading", "Four short works lie on the reading table by the door, for the newcomer. You can read the founding, the storm, the name, or the districts. The rest of the shelves are for the serious reader, and I am happy to say they exist.");
        AddTopic("founding,founders,settlement", "A short account of the first settlers and their great oak. Read it first. Everything else follows from it.");
        AddTopic("storm,oak,great oak,lightning,burning", "A short account of the storm and the three days the oak burned. It does not say a great deal more than that, I am afraid. I have looked.");
        AddTopic("name,motto,oakenfell", "A note on how the city got its name and its motto. It is a pleasant small thing, and quite accurate.");
        AddTopic("districts,district,city,five districts", "A plain account of the five districts, which every citizen ought to know and many do not.");
        AddTopic("archives,archive,records,downstairs", "The archives are kept below and are open by appointment only. I regret that I cannot make exceptions, though many have asked.");
        AddTopic("scholar,visitor,visiting scholar,man at the table", "A visiting scholar. He has been here some weeks, and he is a diligent reader and a courteous guest. He asks a great many questions, mostly of the shelves, and of anyone else who will listen.");
        AddTopic("rules,silence,quiet", "Silence, tidiness, and return what you borrow. Those are all the rules, and I am prepared to enforce them.");
        AddTopic("insight,study,learning,knowledge,scholars,wisdom", "Insight is the reason there are books. A reader with plenty will find in a short work what another misses in a long one. The Athenaeum is built for the first sort of reader and puts up with the second.");
        AddEmote("Hester slides a returned book onto a cart and straightens its spine.", 8);
        AddEmote("Hester looks over her spectacles at a whispering pair of apprentices, and the whispering stops.", 6);
    }
}
