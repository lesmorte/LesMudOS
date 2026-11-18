// @inherit /std/room

public class RvShipwright : Room
{
    public override void Create()
    {
        ShortDesc = "The Shipwright's Yard";
        LongDesc = "The Shipwright's Yard slopes down to the river, and on the slip a great barge hull stands on blocks, half-planked, its ribs bare like the bones of a fish. Stacks of seasoned oak and elm lie under a lean-to roof, the air is thick with the scents of tar, hot pitch and fresh-cut wood, and the ring of mallets and the rasp of adzes never stops. A steam-box smokes at the edge of the yard, and a half-hull model sits on a bench beside it. Across the yard, a small hut serves as an office, with plans pinned to the door.";
        Exits["west"] = "/domains/greenvale/oakenfell/river/quay_north";

        AddDetail("hull,barge,slip,blocks,ribs,planking,keel,frame", "A river barge half built on the slip, a long flat-bottomed hull with a bluff bow, its ribs bare to the sky and its lower planks bent on and nailed. The keel is a single squared baulk of oak, longer than a house is wide.");
        AddDetail("timber,oak,elm,stacks,planks,lean-to,wood", "Seasoned oak and elm stacked under a lean-to, each plank stickered with spacers to dry, the ends sealed with pitch. The oak is dark and heavy, the elm pale and twisted.");
        AddDetail("steam-box,steam box,steambox,smoke,kettle", "A long wooden box on a brick firepit, belching steam. A plank is inside, softening, to be bent round the hull's curve.");
        AddDetail("model,half-hull,half hull,bench", "A carved half-hull model of a river barge, polished with handling, set on a bench with a rule and a pair of dividers. Notes are scribbled all down its side.");
        AddDetail("tools,adze,adzes,mallets,mallet,caulking,pitch,tar,pot", "Adzes, mallets, caulking irons and a smoking pitch-pot hang from hooks or lie at the foot of the slip, each worn smooth by a master's hand.");
        AddReadable("plans,plan,door,hut,notice", "The plans pinned to the office door are covered in figures. A card in a big, square hand reads:\n\"BARGES BUILT, REPAIRED AND CAULKED. Seasoned timber only. No orders taken for sea-going vessels; the Mere is quite enough river for us.\"", "Plans and a card are pinned to the door of the office. You could {cmd}read{/} them.");

        AddSpawn("/domains/greenvale/oakenfell/river/npc/gunnar_holt");
    }
}
