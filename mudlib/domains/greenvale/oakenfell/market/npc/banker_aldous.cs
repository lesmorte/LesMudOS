// @inherit /std/npc

public class MkBankerAldous : Npc
{
    public override void Create()
    {
        Name = "Aldous Venn";
        Ids.Add("aldous");
        Ids.Add("aldous venn");
        Ids.Add("banker");
        Ids.Add("master venn");
        Ids.Add("clerk");
        ShortDesc = "Aldous Venn, the banker";
        LongDesc = "A tall, stooping man in a grey coat with silver buttons, peering down a long nose at a ledger. His hands are thin and spotless, and his voice is dry and precise.";
        Level = 3;
        MaxHp = 40;

        AddChat("Welcome to the Oakenfell Bank. Please keep your voice low.");
        AddChat("Accounts and vaults are coming services. The books are not yet open to new arrivals.");
        AddChat("Patience is the first virtue of a banker. The second is arithmetic.");
        AddTopic("account,accounts,deposit,withdraw,balance", "Accounts, deposits and withdrawals are coming services of this bank, but not yet open to new arrivals. When the books open, banked gold will be safe from loss.");
        AddTopic("vault,vaults,strongroom,strongrooms,rent", "A vault for keeping items is also planned. Not yet open. Do come back, when the books are ready.");
        AddTopic("consortium,owners", "The bank was founded in the Consortium's second century, and remains in good hands.");
        AddTopic("safe,safety,theft,robbery", "No one has robbed this bank in a hundred years. The one who tried is the reason for the guards.");
        AddEmote("Aldous Venn turns a page of his ledger with a faint rustle.", 8);
        AddEmote("Aldous Venn adjusts his spectacles and frowns at a column of figures.", 6);
    }
}
