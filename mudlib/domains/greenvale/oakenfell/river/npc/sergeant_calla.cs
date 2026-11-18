// @inherit /std/npc

public class RvSergeantCalla : Npc
{
    public override void Create()
    {
        Name = "Calla Wren";
        Ids.Add("calla");
        Ids.Add("wren");
        Ids.Add("sergeant");
        Ids.Add("duty sergeant");
        Ids.Add("ranger");
        ShortDesc = "Calla Wren, the duty sergeant";
        LongDesc = "A lean, sun-browned woman in a long green cloak, with a pale shield sewn on the breast and a scar across the back of one hand. Her eyes never quite settle on you; they keep returning to the door.";
        Level = 4;
        MaxHp = 55;

        AddChat("Ever vigilant. That is the whole of the creed, friend. The rest is boots.");
        AddChat("We keep the roads, we guide travelers through, and we go where we are needed. If you have news from the road, I want it.");
        AddChat("Look at the map if you want to know where the Rangers go. Every pin is a post.");
        AddTopic("riverwatch,rangers,riverwatch rangers,order,creed", "The Riverwatch Rangers are a frontier order. We patrol the borders of Greenvale, protect the roads, escort travelers, hunt dangerous creatures and try to spot threats before they reach the settled lands. Ever Vigilant. We are not a large order, but we are a stubborn one.");
        AddTopic("watchfort,watchfort crossing,headquarters,hq,crossing", "Watchfort Crossing, east of here, along the Crossing Road. That is our headquarters. This office is a city post: we keep an eye on the river and the gates, and we carry word to and from the Crossing. The road east is not yet open to travelers.");
        AddTopic("roads,patrol,patrols,road,map", "We patrol the Crossing Road east, the Vale Road south to Vincent's Hollow, the Northern Road, and the Westwood Road as far as the second milestone. The map on the wall shows the posts. Beyond the milestone we do not go, not at present, and I would advise you not to.");
        AddTopic("assembly,allies,consortium,hall of legends,friends", "We work with the Assembly, which gives us our charge, and the Hall of Legends, which gives us recruits. The Consortium pays for the roads they travel, and so we are on good terms.");
        AddTopic("enemies,rivals,raiders,monsters,thorn pact,thorn", "Monsters, raiders and what the old reports call extremists of the Thorn Pact. That last I will not discuss further. Not with strangers.");
        AddTopic("join,recruit,enlist,work,help,quest", "We have no work for strangers at this desk yet. The Rangers know their own, and we will know you when you have earned it. Keep your eyes open on the road and come back.");
        AddEmote("Calla Wren runs a finger down a patrol report and makes a note in the margin.", 6);
        AddEmote("Calla Wren glances at the door, at the map and back at the door.", 5);
    }
}
