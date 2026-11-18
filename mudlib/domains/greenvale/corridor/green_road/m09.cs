// @inherit /std/room

public class GrBelowTheWatchtower : Room
{
    public override void Create()
    {
        ShortDesc = "The Green Road, below the Watchtower";
        LongDesc = "The land rises on the west side of the road to a wooded knoll, and above the treetops stands the stump of a stone tower with its upper storey fallen in. The road runs in its shadow, hemmed by tall trees and tangled thorn. A weathered milestone leans at the verge. On the knoll nothing moves, and no light shows, and the road is quieter than it has any right to be.";
        Exits["south"] = "/domains/greenvale/corridor/green_road/m08";
        Exits["north"] = "/domains/greenvale/corridor/green_road/m10";
        Safe = false;

        AddDetail("tower,watchtower,knoll,stump,hill,ruin", "A square stone tower, perhaps three storeys, with its top broken off and the rest furred with ivy. It was built to see the road and now cannot see anything. A path to it leaves the road farther on, in the woodland, and is not marked.");
        AddDetail("milestone,stone,marker,leaning", "A rough stone leaning with its top rubbed round by hands. Cut figures on its face show the distance to Eldergreen and to the Crossroads, and below them are words in a different, more recent chisel.");
        AddDetail("thorn,thorns,thicket,brambles,bramble,tangle", "A wall of blackthorn and bramble hems the road. In it there are gaps the width of a goblin and none the width of a man.");
        AddDetail("trees,wood,woods,forest", "Tall pines and oaks, black against the slope. The knoll above is thickly wooded, and the rock of the old tower shows through only in places.");
        AddDetail("road,verge,ditch,shadow", "A ditch runs along the roadside, half choked with leaf. Rangers cleared it once and no one has since.");
        AddDetail("smoke,fire,light,lights", "No smoke rises from the tower, and nobody has seen a light there in years, although drovers swear they have heard something from it at night.");
        AddReadable("milestone words,words,chiseled words,inscription,figures", "On the stone, in figures and then in a harder, later hand:\n\nEDGE POST 8 MI.\nCROSSROADS 2 MI.\n\nRIVERWATCH POST. EVER VIGILANT.\n(Beneath, scratched in with a knife:) NOBODY HOME.", "Figures and a few words are cut into the milestone. You could {cmd}read{/} them.");

        AddEncounter("/domains/greenvale/corridor/mob/goblin_raider", 10);
    }
}
