// @inherit /std/room

public class FdSquare : Room
{
    public override void Create()
    {
        ShortDesc = "Founder's Square";
        LongDesc = "Founder's Square is the oldest open ground in Oakenfell, a wide cobbled space ringed by sober stone buildings under steep slate roofs. Underfoot, pale and dark stones are laid in a great mosaic of an oak in full leaf, worn smooth at its heart by nearly four centuries of boots. The open doors of City Hall ({cmd}cityhall{/}) and the tall bronze-bound doors of the Assembly Hall ({cmd}assembly{/}) face one another across the stones, and between them stand a long notice board and a waist-high block of grey stone. The air smells of candle-smoke, wet cobbles and hot pies drifting up from the market, and the talk of the square never quite stops.";
        Exits["south"] = "/domains/greenvale/oakenfell/market/north_rows";
        Exits["north"] = "/domains/greenvale/oakenfell/founders/old_stone_street";
        Exits["east"] = "/domains/greenvale/oakenfell/river/old_river_street";
        Exits["west"] = "/domains/greenvale/oakenfell/guild/guild_row";
        Exits["cityhall"] = "/domains/greenvale/oakenfell/founders/city_hall";
        Exits["assembly"] = "/domains/greenvale/oakenfell/founders/assembly_hall";

        AddDetail("cobbles,cobblestones,stones,paving,ground,floor", "The cobbles are grey river-stone, each one rounded by feet and wheels and set so tight that not even a weed has found a seam. Here and there a stone is carved with a mason's mark so old it has nearly gone.");
        AddDetail("mosaic,oak,great oak,tree,pattern", "Dark river-stones and pale limestone make an oak whose branches spread almost to the edges of the square. The trunk, at the center, is as thick as a cottage, and the roots are drawn nearly as broad as the crown, reaching out between the cobbles in every direction. A ring of lettering runs around the rim. You could {cmd}read{/} it.");
        AddReadable("lettering,motto,ring,words,inscription,letters", "Around the rim of the mosaic, in letters a hand long, the stones spell:\n\"FROM STRONG ROOTS GROW GREAT THINGS\"", "Letters of pale limestone run around the rim of the mosaic. You could {cmd}read{/} them.");
        AddDetail("board,notice board,notices,papers", "A long frame of black oak under a little slate roof, thick with papers pinned over older papers. Some notices are crisp and new, others are curled and brown. You could {cmd}read{/} the board.");
        AddReadable("board,notice board,notices,notice", "The newest notices on the board read:\n\n\"FEAST OF THE THREE DAYS. The city keeps its remembrance of the Burning at harvest-end, with a lamp lit in every window for three nights. Stalls in the square by leave of the Clerk of Markets.\"\n\n\"THE GREENVALE ASSEMBLY sits at the ninth bell on the first day of each week. The public gallery is open to observers. Petitions to be entered at City Hall by registered citizens only.\"\n\n\"WESTERN ROAD. Carters and travelers are warned that the Westwood Road toward the Elderwood is no longer patrolled beyond the second milestone. Do not go alone. By order of the Riverwatch.\"\n\n\"LOST. A brindled hound, answers to Biscuit, one white ear, last seen on Mere Bridge. Two gold to whoever returns him. Ask at the Fish Market for Marta Gill.\"\n\n\"HALL OF LEGENDS. Those of proven deeds are welcome at the Great Doors on Guild Row. Apprentices to the crafting guilds enroll at Crafters' Court, and nowhere else.\"", "A long frame of black oak thick with papers. You could {cmd}read{/} the board.");
        AddDetail("speaker's stone,speakers stone,block,grey stone,stone block,speaking stone", "A flat-topped block of grey stone with a worn step on one side, hollowed in the middle by countless feet. Anyone may stand on it and speak, and by old custom no one may be shouted down until they have finished their first sentence. It is mostly used for sermons, price complaints and marriage proposals, in about that order.");
        AddDetail("city hall,hall,hall doors", "A plain, broad-fronted building of grey stone with a clock face above its open double doors and a long queue of patient people inside. It looks as though it has been in the same spot since before anyone thought to put a door in it. Its doors are to the {cmd}cityhall{/}.");
        AddDetail("assembly hall,assembly,bronze doors,tall doors,doors", "Tall oak doors bound in green-weathered bronze, closed to the square but for a smaller door cut into the left leaf. Above them is carved an oak with seven boughs. There are no guards, only a steward's lamp burning behind a window. You can go {cmd}assembly{/}.");
        AddDetail("roofs,buildings,slate,houses", "Steep roofs of dark slate, the buildings below them faced with grey stone and trimmed with timber gone black with age. Every window on the square seems to have a face in it at some hour or other.");
        AddDetail("pies,smell,air", "The smell of hot pastry comes up from the market to the south, on a breeze that also carries the sound of a hundred haggles.");

        AddSpawn("/domains/greenvale/oakenfell/founders/npc/town_crier");
        AddSpawn("/domains/greenvale/oakenfell/founders/npc/dalla_pryce");
        AddSpawn("/domains/greenvale/oakenfell/founders/npc/tomas_wick");
    }
}
