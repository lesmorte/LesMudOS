// @inherit /std/room

public class FbGuardianHall : Room
{
    public override void Create()
    {
        ShortDesc = "The Hall of the Standing Guard";
        LongDesc = "The gallery opens into a hall so wide that the far wall is only a suggestion, with two rows of seven square pillars marching away into the dark. Every sound you make comes back from a long way off, a little late. At the north end, before a tall door of the same stone, a great frame stands upon a wide slab, hung with bands and straps that have been cut, with pale chips scattered on the floor around it. Along the walls the last of the carved figures have turned, each one, to face the way you came.";
        Exits["south"] = "/domains/greenvale/corridor/barrow/long_gallery";
        Exits["north"] = "/domains/greenvale/corridor/barrow/borrowed_tomb";
        ResetSeconds = 1800;
        DangerBand(9, 10);

        AddDetail("pillars,pillar,rows,columns,column", "There are seven pillars to a row, square and plain, each carrying nothing on its faces but the interlaced line in bands of seven. They are set so exactly that from the door you look down two lanes of perfect shadow. Their feet are bare of dust, as though something had been walking among them.");
        AddDetail("frame,cradle,slab,plinth,stand,place", "The frame is of dark stone and metal, taller than a tall man, standing on a slab worn by the weight of something heavy that stood there a very long time. It is the place of a guardian, and it is empty. The slab is scored with fresh marks, as by a tool and as by a heavy tread.");
        AddDetail("bands,band,straps,strap,bindings,binding,cut,cord,cords,chips,chip,pale", "The bindings are bands of grey metal as wide as a hand, set into the stone with lead, and cords of something black. They have been cut through, clean, with a tool made for it, and the ends are bright. The chips on the floor are pale where they have been struck from something recently, and dull with age where they were struck long ago. Something was broken here, and bound again, and broken. It no longer lies in the frame.");
        AddDetail("door,great door,north,slab door,north door", "The door is a single slab, taller than the pillars, slid aside in a groove in the floor until a narrow gap is left. The stone sill is thick with dust that no one has crossed in a very long time, and it has no chalk upon it. Whoever stood here stopped before it.");
        AddDetail("ticks,tick,marks,chalk,floor,paving,dots", "Along the paving between the pillars runs a line of small chalk ticks, one beside each of the fourteen pillars, neatly spaced, as a surveyor sets them. The last tick lies three paces from the door. There the line ends, and no chalk lies beyond it.");
        AddReadable("walls,wall,figures,figure,band,carving,carvings,frieze,procession,border,last figures", "The procession ends here. The last seven figures have turned about, so that they stand with their backs to the great door and their faces toward the gallery, toward you; their arms are down and their hands are open. Above them the border that was unfinished in the court in the wood, and ran on through every passage since, bends at last and meets itself, a closed ring. It is the only closed ring in the barrow. The faces, to the end, are blank.", "The last of the carved figures face you along the walls. You could {cmd}read{/} the band.");
        AddSpawn("/domains/greenvale/corridor/mob/restless_spirit", 1);
    }
}
