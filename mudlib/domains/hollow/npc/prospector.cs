// @inherit /std/npc

public class DrunkenProspector : Npc
{
    public override void Create()
    {
        Name = "Drunken Prospector";
        Ids.Add("prospector");
        Ids.Add("drunk");
        ShortDesc = "a drunken prospector";
        LongDesc = "A grizzled old man with a sunburnt nose, a battered pick leaning against his stool, and a tankard that never seems to empty. His eyes are bloodshot but sharp when they settle on you.";
        Level = 1;
        MaxHp = 10;

        AddChat("*hic* Tunnels, I tell ya. Dug under these hills by somebody who knew exactly what they were doin'.");
        AddChat("There's somethin' old down there. I put my ear to the cellar wall last night and it knocked back at me.");
        AddChat("Keep seein' that crow sittin' on the palisade every mornin'. Same bird, I swear. Watches me like it's owed money.");
        AddTopic("tunnels,tunnel,hills,mine", "Old workings, older than the Hollow. Somebody dug a long way under these hills, and then somebody filled it all back in.");
        AddTopic("crow,bird,raven", "Black as a bad debt, and clever. It sits on the palisade and never once makes a sound. That's the part I don't like.");
        AddEmote("The prospector hiccups, then peers into his empty tankard with great suspicion.", 6);
        AddEmote("The prospector taps his pick against the floor and mumbles a song about gold.", 4);
    }
}
