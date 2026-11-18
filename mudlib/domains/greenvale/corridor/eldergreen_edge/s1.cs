// @inherit /std/room

public class EeHuntersHide : Room
{
    public override void Create()
    {
        ShortDesc = "The Hunter's Hide";
        LongDesc = "A cunningly woven platform of branches and lashed saplings nestles in the great beech's fork, forty feet above the forest floor and roofed over with living boughs so well that it cannot be seen from below. There is room for one to lie full length with a bow, a lantern and a day's food, and slits in the leaf-wall look out on the trail. Everything is dusty and silent, and everything has been left as it was. The air up here is warm, green and smells of dust, bark and old canvas.";
        Exits["west"] = "/domains/greenvale/corridor/eldergreen_edge/d2";
        ResetSeconds = 120;

        AddDetail("platform,hide,branches,saplings,lashings,floor", "The platform is sound, lashed with rawhide that has dried hard as iron. It has been mended, more than once, by someone who meant to stay.");
        AddDetail("slits,slit,leaf-wall,window,view,trail", "Through the slits you can see the whole of the trail below, and the cord-hung trees along it. Nothing on it could see you.");
        AddDetail("bedroll,bed,blanket,canvas", "A bedroll lies unrolled along one side, its blankets stiff with dust, a folded cloak for a pillow. It has not been slept in since the leaves fell.");
        AddDetail("bow,quiver,arrows,lantern,flask,food", "A good yew bow is propped in the corner, unstrung, with a quiver of arrows and an unlit lantern beside it. A flask is empty and a heel of bread is stone.");
        AddDetail("ladder,spikes,rungs,holes", "Iron spikes have been driven into the trunk for a ladder and disguised with bark. You came up by them, and they lead down to the trail.");
        AddDetail("dust,silence,air", "The dust is undisturbed except by your own feet, and by a tidy line where something small has walked the length of the platform.");
        AddReadable("notes,journal,pages,tally,notebook,book,writing", "A little oilskin notebook, its pages scrawled in a quick tidy hand:\n\n\"Morwenna, her book. Goblins on the trail, by the moon.\n\nNew moon: three scouts, northbound, cords at the belt, no spears. Counting.\nFirst quarter: two, southbound, with a pack. They stopped at the beech and sniffed, then left. Did they know?\nFull moon: five, in a line, very quiet. They went to the Oak and stopped twenty paces short. Walked round it, left hand always inward, as though round a grave. They will not touch the swept ground. I wonder who sweeps it.\nLast quarter: they count the road. One knot a wagon, two for an armed party. Told no one. The commander should be told. Am going to the Edge Post at first light, will tell her myself, and then I will go to the Oak and look properly.\"\n\nThe last page has been torn out.", "A small oilskin notebook lies on the bedroll. You could {cmd}read{/} it.");

    }

    // Tells the Chronicle that a player has come here. A character is credited once, so every visit may report.
    public override void Init()
    {
        base.Init();
        var who = ThisPlayer();
        if (who == null || !Interactive(who)) return;
        try
        {
            LoadObject("/secure/events").Call<string>("Report", "discover.hidden", who, new List<ObjectRef> { who }, Self, "hunters-hide",
                new Dictionary<string, string> { ["name"] = "the Hunter's Hide" });
        }
        catch (MudRuntimeException) { }
    }
}
