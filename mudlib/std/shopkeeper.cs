// @inherit /std/npc

/// <summary>
/// An NPC that sells from a stock list and buys items back at half value. Adds
/// the commands browse, buy, sell and value for players in the room. Stock is
/// unlimited. Rare stock is rolled when the shopkeeper is made and again at
/// each reset of its room.
/// </summary>
public class Shopkeeper : Npc
{
    private sealed class Stock
    {
        public string Path = "";
        public int Price;
        public int Percent = 100;
        public ObjectRef? Sample; // kept inside the shopkeeper, to answer to names and to describe the goods
    }

    private readonly List<Stock> stock = new();

    /// <summary>Always on sale.</summary>
    protected void AddStock(string path, int price) => Add(path, price, 100);

    /// <summary>On sale only if a roll of percent succeeded when the shopkeeper was made or last restocked.</summary>
    protected void AddRareStock(string path, int price, int percent) => Add(path, price, percent);

    private void Add(string path, int price, int percent)
    {
        var entry = new Stock { Path = path, Price = price, Percent = percent };
        stock.Add(entry);
        Roll(entry);
    }

    private void Roll(Stock entry)
    {
        var wanted = entry.Percent >= 100 || Random(100) < entry.Percent;
        if (wanted && entry.Sample is not { IsAlive: true })
        {
            entry.Sample = CloneObject(entry.Path);
            MoveObject(entry.Sample, Self);
        }
        else if (!wanted && entry.Sample is { IsAlive: true } old)
        {
            old.Call("Vanish");
            entry.Sample = null;
        }
    }

    public override void OnReset()
    {
        foreach (var entry in stock)
            if (entry.Percent < 100)
            {
                if (entry.Sample is { IsAlive: true } old) old.Call("Vanish");
                entry.Sample = null;
                Roll(entry);
            }
    }

    public override void OnDestruct()
    {
        foreach (var entry in stock)
            if (entry.Sample is { IsAlive: true } sample) sample.Call("Vanish");
    }

    /// <summary>What happens when a purchase has been paid for. Return false to refuse (the buyer is refunded).</summary>
    protected virtual bool OnBuy(ObjectRef buyer, string path)
    {
        var item = CloneObject(path);
        MoveObject(item, buyer);
        return true;
    }

    protected override void Noticed(ObjectRef other)
    {
        base.Noticed(other);
        if (!Interactive(other)) return;
        AddAction("browse", CmdBrowse);
        AddAction("buy", CmdBuy);
        AddAction("sell", CmdSell);
        AddAction("value", CmdValue);
    }

    private Stock? Find(string name) => stock.FirstOrDefault(s => s.Sample is { IsAlive: true } sample && sample.Call<bool>("Id", name.ToLowerInvariant()));

    private bool CmdBrowse(string args)
    {
        var text = new StringBuilder($"{Name} has these for sale:\n");
        foreach (var entry in stock)
            if (entry.Sample is { IsAlive: true } sample)
            {
                // Padded by the width of the visible text, so markup in a name cannot shift the prices.
                var item = Cap(sample.Call<string>("Short") ?? "something");
                text.Append($"  {item}{new string(' ', Math.Max(0, 32 - Markup(item, false).Length))}{entry.Price,5} gold\n");
            }
        Write(text.ToString());
        return true;
    }

    private bool CmdBuy(string args)
    {
        if (args == "") { NotifyFail("Buy what?"); return false; }
        var entry = Find(args);
        var buyer = ThisPlayer();
        if (entry == null || buyer == null) { Write($"{Name} does not sell '{Esc(args)}'. Try {{cmd}}browse{{/}}.\n"); return true; }
        if (!buyer.Call<bool>("AddGold", -entry.Price)) { Write($"You cannot afford that. It costs {entry.Price} gold.\n"); return true; }
        var what = entry.Sample!.Call<string>("Short");
        if (!OnBuy(buyer, entry.Path))
        {
            buyer.Call("AddGold", entry.Price);
            return true;
        }
        Write($"You buy {what} for {entry.Price} gold.\n");
        Say($"{buyer.Call<string>("QueryName")} buys {what} from {Name}.\n");
        return true;
    }

    private bool CmdSell(string args)
    {
        var seller = ThisPlayer();
        var item = args == "" || seller == null ? null : Present(args, seller);
        if (item == null) { NotifyFail("Sell what?"); return false; }
        var value = item.Call<int>("QueryValue");
        if (item.Call<bool>("QueryQuestItem")) { Write($"{Name} will not buy that; it is too important.\n"); return true; }
        if (value / 2 < 1) { Write($"{Name} shrugs. \"That is not worth anything to me.\"\n"); return true; }
        var what = item.Call<string>("Short");
        var price = value / 2;
        seller!.Call("AddGold", price);
        item.Call("Vanish");
        Write($"You sell {what} to {Name} for {price} gold.\n");
        return true;
    }

    private bool CmdValue(string args)
    {
        var asker = ThisPlayer();
        var item = args == "" || asker == null ? null : Present(args, asker);
        if (item != null)
        {
            var value = item.Call<int>("QueryValue");
            var price = item.Call<bool>("QueryQuestItem") ? 0 : value / 2;
            Write(price < 1 ? $"{Name} would not buy {item.Call<string>("Short")}.\n" : $"{Name} would pay {price} gold for {item.Call<string>("Short")}.\n");
            return true;
        }
        var entry = args == "" ? null : Find(args);
        if (entry == null) { NotifyFail("Value what?"); return false; }
        Write($"{Cap(entry.Sample!.Call<string>("Short") ?? "It")} costs {entry.Price} gold here.\n");
        return true;
    }
}
