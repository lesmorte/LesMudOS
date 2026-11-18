// @inherit /std/room

public class RvFishMarket : Room
{
    public override void Create()
    {
        ShortDesc = "The Fish Market";
        LongDesc = "The Fish Market is a wide tarred awning over a floor of wet stone, where marble-topped slabs lie heaped with the morning's catch under drifts of crushed ice. Silver perch, brown eels and fat pink salmon glisten in the lamplight, and the stallholders shout their prices over each other in a river-flavored dialect. Smoke curls from a long rack of curing fish at the back, and the smell is overwhelming and, to those who love it, wonderful. A hand-painted board hangs above the biggest stall.";
        Exits["east"] = "/domains/greenvale/oakenfell/river/quay_north";

        AddDetail("slabs,slab,marble,fish,catch,perch,eels,eel,salmon,ice", "Slabs of grey marble laid out in rows, sloping to drains, piled with silver perch, brown eels, and the rare, pink flank of a river salmon, all glistening under crushed ice. A boy with a bucket runs between them, replacing the melted ice.");
        AddDetail("rack,smoke,smoking rack,curing fish,smoked fish", "A long rack at the back of the market, hung with split fish and eels, curls with pale smoke from a smoldering fire of alder wood. The smell is the best thing in the market.");
        AddDetail("awning,roof,floor,stone,drains", "A tarred canvas awning on stout poles, dripping gently. The wet stone floor slopes to a central drain, and every few minutes someone throws a bucket of water over it with a cry of 'Mind your feet!'");
        AddDetail("stallholders,stallholder,fishmongers,fishmonger,shouting,voices", "The stallholders are big, red-faced and loud, and they hold up their fish for inspection with the hands of men who have never once been embarrassed.");
        AddReadable("board,painted board,sign,prices", "The board above the biggest stall is painted in blue and white:\n\"CRANE'S FISH. Smoked eel, three gold. Perch pie, five gold. Salmon by the pound when there is salmon. We do not haggle. We do not give credit. We do not take fish back.\"", "A hand-painted board hangs above the biggest stall. You could {cmd}read{/} it.");

        AddSpawn("/domains/greenvale/oakenfell/river/npc/bessa_crane");
        AddSpawn("/domains/greenvale/oakenfell/river/npc/marta_gill");
    }
}
