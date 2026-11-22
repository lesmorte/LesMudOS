// @inherit /std/npc
// @use /std/item

/// <summary>
/// An NPC that sells from a stock list and buys items back at half their value (quality included). Adds the commands
/// browse, buy, sell and value for players in the room. Stock is unlimited. Rare stock is rolled when the shopkeeper is
/// made and again at each reset of its room. A stock entry may have a quality; its price is the listed price times the
/// quality's multiplier (/std/item). Stackable stock can be bought by the number ("buy 5 skins") and arrives as one stack.
/// A stock path may be a save record with properties, for generic items whose kind is a property: AddStock("/std/skin?p.kind=rabbit", 4).
/// </summary>
public class Shopkeeper : Npc
{
    private sealed class Stock
    {
        public string Path = "";
        public int Price;
        public int Percent = 100;
        public int Quality = 1;
        public ObjectRef? Sample; // kept inside the shopkeeper, to answer to names and to describe the goods
    }

    /// <summary>The most items one purchase may be for.</summary>
    private const int MaxPurchase = 1000;

    private readonly List<Stock> stock = new();

    /// <summary>Always on sale.</summary>
    protected void AddStock(string path, int price) => Add(path, price, 100, 1);

    /// <summary>Always on sale, at a quality (0 Poor to 5 Legendary). The price is for Common and is multiplied by the quality's multiplier.</summary>
    protected void AddStock(string path, int price, int quality) => Add(path, price, 100, quality);

    /// <summary>On sale only if a roll of percent succeeded when the shopkeeper was made or last restocked.</summary>
    protected void AddRareStock(string path, int price, int percent) => Add(path, price, percent, 1);

    private void Add(string path, int price, int percent, int quality)
    {
        var entry = new Stock { Path = path, Price = price, Percent = percent, Quality = Math.Clamp(quality, 0, 5) };
        stock.Add(entry);
        Roll(entry);
    }

    private void Roll(Stock entry)
    {
        var wanted = entry.Percent >= 100 || Random(100) < entry.Percent;
        if (wanted && entry.Sample is not { IsAlive: true })
        {
            entry.Sample = CloneStated(entry.Path); // a path, or a record with properties: "/std/skin?p.kind=rabbit"
            if (entry.Sample == null) return; // the item refused that state (Goblin Blood cannot be stocked)
            if (entry.Quality != 1) entry.Sample.Call("SetQuality", entry.Quality);
            MoveObject(entry.Sample, Self);
        }
        else if (!wanted && entry.Sample is { IsAlive: true } old)
        {
            old.Call("Vanish");
            entry.Sample = null;
        }
    }

    /// <summary>The price of one item of a stock entry: the listed price, times the quality multiplier for quality other than Common.</summary>
    private static int UnitPrice(Stock entry) => entry.Quality == 1 ? entry.Price : Math.Max(1, (int)Math.Round(entry.Price * Item.QualityMultipliers[entry.Quality], MidpointRounding.AwayFromZero));
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

    /// <summary>What happens when a purchase of one has been paid for. Return false to refuse (the buyer is refunded).</summary>
    protected virtual bool OnBuy(ObjectRef buyer, string path) => CreateIn(path, 1, StockQuality(path), buyer) != null;

    /// <summary>What happens when a purchase of several of a stackable item has been paid for: one stack. Return false to refuse (the buyer is refunded).</summary>
    protected virtual bool OnBuyStack(ObjectRef buyer, string path, int count) => CreateIn(path, count, StockQuality(path), buyer) != null;

    /// <summary>The quality of the stock entry being bought (several entries may sell one file at different qualities).</summary>
    private int StockQuality(string path) => buying != null && buying.Path == path ? buying.Quality : stock.FirstOrDefault(s => s.Path == path)?.Quality ?? 1;

    private Stock? buying;

    protected override void Noticed(ObjectRef other)
    {
        base.Noticed(other);
        if (!Interactive(other) || stock.Count == 0) return; // a trainer with nothing to sell is not a shop
        AddAction("browse", CmdBrowse);
        AddAction("buy", CmdBuy);
        AddAction("sell", CmdSell);
        AddAction("value", CmdValue);
        AddAction("price", CmdValue);   // the same question in the word players reach for first
    }

    private Stock? Find(string name)
    {
        var id = name.ToLowerInvariant();
        var first = stock.FirstOrDefault(s => s.Sample is { IsAlive: true } sample && sample.Call<bool>("Id", id));
        if (first != null) return first;
        // "buy sword 2": the second thing for sale that answers to "sword", in the order browse lists them.
        var position = SplitOrdinal(id, out var bare);
        if (position == 0) return null;
        var same = stock.Where(s => s.Sample is { IsAlive: true } sample && sample.Call<bool>("Id", bare)).ToList();
        return position <= same.Count ? same[position - 1] : null;
    }

    private bool CmdBrowse(string args)
    {
        var text = new StringBuilder($"{Name} has these for sale:\n");
        foreach (var entry in stock)
            if (entry.Sample is { IsAlive: true } sample)
            {
                // Padded by the width of the visible text, so markup in a name cannot shift the prices.
                var item = Cap(sample.Call<string>("Short") ?? "something");
                text.Append($"  {item}{new string(' ', Math.Max(0, 32 - Markup(item, false).Length))}{UnitPrice(entry),5} gold\n");
            }
        Write(text.ToString());
        return true;
    }

    private bool CmdBuy(string args)
    {
        if (args == "" || !ParseAmount(args, out var count, out var all, out var noun) || all) { NotifyFail("Buy what?"); return false; }
        var entry = Find(noun);
        var buyer = ThisPlayer();
        if (entry == null || buyer == null) { Write($"{Name} does not sell '{Esc(args)}'. Try {{cmd}}browse{{/}}.\n"); return true; }
        var sample = entry.Sample!;
        if (count > 1 && !sample.Call<bool>("QueryStackable")) { Write("You can buy that only one at a time.\n"); return true; }
        if (count > MaxPurchase) { Write($"{Name} cannot sell more than {MaxPurchase} at once.\n"); return true; }
        var total = (long)UnitPrice(entry) * count;
        if (total > int.MaxValue || !buyer.Call<bool>("AddGold", -(int)total))
        {
            Write(count == 1 ? $"You cannot afford that. It costs {total} gold.\n" : $"You cannot afford that. {count} cost {total} gold.\n");
            return true;
        }
        var what = count == 1 ? sample.Call<string>("Short") : sample.Call<string>("DescribeCount", count) ?? sample.Call<string>("Short");
        bool bought;
        buying = entry;
        try { bought = count == 1 ? OnBuy(buyer, entry.Path) : OnBuyStack(buyer, entry.Path, count); }
        finally { buying = null; }
        if (!bought)
        {
            buyer.Call("AddGold", (int)total);
            return true;
        }
        buyer.Call("RestDisturbed", "buy");
        Write($"You buy {what} for {total} gold.\n");
        Say($"{buyer.Call<string>("QueryName")} buys {what} from {Name}.\n");
        return true;
    }

    /// <summary>What a shopkeeper pays for one of an item: half its value, quality included; 0 for quest items.</summary>
    private static int OfferFor(ObjectRef item) => item.Call<bool>("QueryQuestItem") ? 0 : item.Call<int>("QueryValue") / 2;

    /// <summary>What a shopkeeper pays for n of a stack: half the value of the whole lot, rounded down. So eleven skins worth 1
    /// gold each fetch 5 gold, though one alone fetches nothing. Never more than buying them costs, so there is no loop in it.</summary>
    private static long StackOffer(ObjectRef item, int n) => item.Call<bool>("QueryQuestItem") ? 0 : (long)item.Call<int>("QueryValue") * Math.Max(0, n) / 2;

    private bool CmdSell(string args)
    {
        var seller = ThisPlayer();
        if (args == "" || seller == null || !ParseAmount(args, out var count, out var all, out var noun)) { NotifyFail("Sell what?"); return false; }
        List<ObjectRef> found;
        if (!all && count == 1) found = Present(noun.ToLowerInvariant(), seller) is { } one ? new List<ObjectRef> { one } : new List<ObjectRef>();
        else found = Matching(seller, noun).Where(o => !(all && seller.Call<bool>("IsEquipped", o))).ToList();
        if (found.Count == 0) { NotifyFail("Sell what?"); return false; }
        if (found.Any(o => o.Call<bool>("QueryQuestItem")) && !all) { Write($"{Name} will not buy that; it is too important.\n"); return true; }
        found = found.Where(o => !o.Call<bool>("QueryQuestItem") && o.Call<int>("QueryValue") >= 1).ToList();
        if (found.Count == 0) { Write($"{Name} shrugs. \"That is not worth anything to me.\"\n"); return true; }
        var have = found.Sum(QuantityOf);
        if (!all && have < count) { Write($"You have only {have} that {Name} would buy.\n"); return true; }
        var left = all ? int.MaxValue : count;
        // One line per kind of thing sold: "a rusty sword (x3)" for three, not three lines.
        var order = new List<string>();
        var sold = new Dictionary<string, (string single, int n, long price)>();
        foreach (var item in found)
        {
            if (left <= 0) break;
            var n = Math.Min(left, QuantityOf(item));
            left -= n;
            var key = SameKey(item);
            var single = Single(item);
            var price = StackOffer(item, n);
            if (price < 1) { Write($"{Name} will not pay for {ListName(single, n)}: bring more and they are worth something by the lot.\n"); continue; }
            var part = n >= QuantityOf(item) ? item : item.Call<ObjectRef>("Split", n);
            if (part == null) continue;
            part.Call("Vanish");
            if (part.IsAlive) continue; // not gone, so not paid for
            seller.Call("AddGold", (int)Math.Min(price, int.MaxValue));
            if (!sold.TryGetValue(key, out var g)) { g = (single, 0, 0); order.Add(key); }
            sold[key] = (g.single, g.n + n, g.price + price);
        }
        if (order.Count > 0) seller.Call("RestDisturbed", "sell");
        foreach (var key in order)
            Write($"You sell {ListName(sold[key].single, sold[key].n)} to {Name} for {sold[key].price} gold.\n");
        return true;
    }

    private bool CmdValue(string args)
    {
        var asker = ThisPlayer();
        var item = args == "" || asker == null ? null : Present(args, asker);
        if (item != null)
        {
            var n = QuantityOf(item);
            var unit = item.Call<bool>("QueryQuestItem") ? 0 : item.Call<int>("QueryValue");
            var total = StackOffer(item, n);
            Write(unit < 1 ? $"{Name} would not buy {Single(item)} at any number: it is worth nothing here.\n"
                : total < 1 ? $"{Name} would pay nothing for {Single(item)} alone, but 1 gold for every two.\n"
                : n == 1 ? $"{Name} would pay {total} gold for {item.Call<string>("Short")}.\n"
                : unit % 2 == 0 ? $"{Name} would pay {unit / 2} gold each for {item.Call<string>("Short")}, {total} gold in all.\n"
                : $"{Name} would pay {total} gold for {item.Call<string>("Short")} as a lot.\n");
            return true;
        }
        var entry = args == "" ? null : Find(args);
        if (entry == null) { NotifyFail("Value what?"); return false; }
        Write($"{Cap(entry.Sample!.Call<string>("Short") ?? "It")} costs {UnitPrice(entry)} gold here.\n");
        return true;
    }
}
