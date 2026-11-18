// @inherit /std/room

public class SgFreeLodge : Room
{
    public override void Create()
    {
        ShortDesc = "The Free Lodge";
        LongDesc = "The Free Lodge is a barn of a hall, smelling of oiled steel, spilled ale and old leather. Weapons hang from the rafters, racked on the walls and stacked in barrels by the door, and a great table is scarred with knife-play and dice. A red-and-black banner is nailed over the hearth, and a ledger-desk by the door is where companies and contracts are made. Hard people with easy manners sit at the benches and look you over. The motto is burned into the mantel: Coin Before Crown.";
        Exits["east"] = "/domains/greenvale/oakenfell/southgate/wayfarers_row";
        ResetSeconds = 120;

        AddDetail("banner,banners,flag", "The banner is red and black, with a gold coin on a black field. Every company that has marched under it has left a stitch or a scorch mark.");
        AddDetail("mantel,motto,coin before crown", "\"COIN BEFORE CROWN\" is burnt into the mantelpiece in letters a hand high. A joke has been carved under it, and then scratched out, and then carved again.");
        AddDetail("weapons,rack,racks,barrels,steel", "The weapons here are used and looked after: notched swords, pole-arms, crossbows and a good many knives. They are not for sale, but the sellswords will gladly tell you what they cost.");
        AddDetail("table,great table,dice", "A heavy table scarred by knives, dice and the elbows of a hundred companies. Someone has carved a map of the Elderwood into it, with crosses.");
        AddDetail("desk,ledger,ledger-desk,contracts", "A tall desk holds the lodge's ledger, bound in cracked red leather. The contracts are entered there, and the lodge takes its cut.");
        AddDetail("sellswords,mercenaries,benches", "Scarred, sunburnt men and women, in mismatched armor, mend straps, sharpen knives and gamble. They are friendly enough to those who pay, and polite to those who might.");
        AddReadable("notice,notices,contract board,jobs", "THE FREE LODGE\nContracts posted daily. Pay on delivery, bonus on survival.\nNO feuds inside. NO unpaid tabs. NO questions about our clients.\nOpen to any sword, any road, any coin.\nCOIN BEFORE CROWN", "A notice nailed to the door lists the lodge's terms. You could {cmd}read{/} it.");

        AddSpawn("/domains/greenvale/oakenfell/southgate/npc/recruiter_sera");
        AddSpawn("/domains/greenvale/oakenfell/southgate/npc/sellsword_brandt");
        AddSpawn("/domains/greenvale/oakenfell/southgate/npc/sellsword_maddok");
    }
}
