using System.Globalization;

namespace SendChecklist.Proto;

// PROTO indkøb: a supplier's list held against the open purchase orders. The
// columns follow the present table: Lp, Lev. varenr., Varenr., Varenavn,
// Antal, Enhed, Udestående, Ordre, Stykpris, Aftalepris, Forskel, Status.
// Every line is made up: invented parts, numbers, orders and prices.
public record Order(int No, int Qty);

public record Line(int Lp, string LevSku, string Sku, string RawName, int Qty, decimal Price, decimal? Agreed, int? Outstanding = null, params Order[] Orders)
{
    /// A line with an open order is "Klar"; one without is "Ingen åben ordre".
    public bool Ready => Orders.Length > 0;
    public decimal? Diff => Agreed is null ? null : Price - Agreed;
    public bool Deviates => Diff is not null and not 0;
    public decimal Amount => Qty * Price;

    /// The source writes names in capitals. Sentence case reads faster.
    public string Name => RawName.Length < 2 ? RawName : RawName[..1] + RawName[1..].ToLowerInvariant();
}

public static class Ib
{
    public const string Title = "Indkøbsliste";
    public const string Unit = "stk.";
    public const string Cur = "EUR";

    public static readonly Line[] Lines =
    [
        new(1, "7314", "2204105", "PAKNINGSSÆT, PUMPEHUS, TYPE B, KOMPLET", 20, 14.25m, 14.25m),
        new(2, "7321", "2204106", "PAKNINGSSÆT, PUMPEHUS, TYPE C, KOMPLET", 20, 14.25m, 14.25m),
        new(3, "7388", "2204910", "KUGLEVENTIL / AFSPÆRRING, 3-VEJS MED HÅNDTAG, RUSTFRI, INDVENDIGT GEVIND", 5, 21.40m, 21.40m),
        new(4, "5102", "2210447", "MONTAGEBESLAG, VINKEL TIL VENSTRE SIDE, GALVANISERET, NEDERSTE DEL", 10, 8.12m, 8.12m),
        new(5, "5540", "1180301", "LEJEHUS, 2/3-DELT, VENSTRE MONTERING", 60, 31.75m, 32.40m, 90, new(61207, 45), new(61388, 15)),
        new(6, "5547", "1180302", "LEJEHUS, 2/3-DELT, HØJRE MONTERING", 60, 31.75m, 32.40m, 84, new(61207, 28), new(61388, 32)),
        new(7, "4419", "3050211", "AFSTANDSRING, STÅL, 40 MM", 120, 6.48m, 6.48m, 200, new Order(61388, 120)),
        new(8, "4426", "3050212", "AFSTANDSRING, STÅL, 50 MM", 120, 6.90m, 6.48m, 150, new Order(61388, 120)),
        new(9, "8203", "0471833", "KOBLINGSFLANGE / MELLEMSTYKKE, DREJET UDFØRELSE TIL VENSTRE, SERIE M UDVIDET", 20, 19.05m, 19.05m),
        new(10, "8210", "0471834", "KOBLINGSFLANGE / MELLEMSTYKKE, DREJET UDFØRELSE TIL HØJRE, SERIE M UDVIDET/", 5, 19.05m, 19.05m),
        new(11, "5519", "0926011", "LEJEHUS, 2-DELT, VENSTRE MONTERING", 40, 27.30m, 27.30m, 40, new Order(61388, 40)),
        new(12, "9066", "6612071", "MONTAGEBESLAG, VINKEL TIL VENSTRE SIDE, GALVANISERET, NEDERSTE DEL", 20, 4.95m, 4.95m),
        new(13, "9073", "6612072", "MONTAGEBESLAG, VINKEL TIL HØJRE SIDE, GALVANISERET, NEDERSTE DEL", 20, 4.95m, 4.95m),
        new(14, "6140", "3907118", "LEJEHUS, 2-DELT, HØJRE", 100, 21.40m, 21.40m),
        new(15, "6233", "3907120 / X3907120", "—", 50, 12.80m, null),
        new(16, "6198", "3907455", "MONTAGEBESLAG, VINKEL TIL VENSTRE SIDE, GALVANISERET, NEDERSTE DEL", 10, 5.36m, 5.36m),
        new(17, "6205", "3907456", "MONTAGEBESLAG, VINKEL TIL HØJRE SIDE, GALVANISERET, NEDERSTE DEL", 10, 5.36m, 5.36m, 10, new Order(61388, 10)),
        new(18, "6077", "3907701", "KOBLINGSFLANGE / MELLEMSTYKKE, DREJET UDFØRELSE TIL VENSTRE, STANDARD", 10, 11.94m, 11.94m, 25, new Order(61388, 10)),
        new(19, "6084", "3907702", "KOBLINGSFLANGE / MELLEMSTYKKE, DREJET UDFØRELSE TIL HØJRE, STANDARD", 10, 11.94m, 11.94m),
        new(20, "7752", "4419033", "LEJEHUS, 2-DELT, VENSTRE MONTERING", 30, 20.15m, 20.15m),
        new(21, "7745", "4419608", "KUGLEVENTIL / AFSPÆRRING, 2-VEJS TIL HØJRE, MED HÅNDTAG, FORSTÆRKET", 100, 9.26m, 9.26m, 140, new Order(61388, 100)),
        new(22, "8831", "7203516", "LEJEHUS, FORREST TIL HØJRE MONTERING", 20, 38.60m, 38.60m),
        new(23, "9415", "7203990", "KOBLINGSFLANGE / MELLEMSTYKKE STANDARD, FORREST TIL VENSTRE, KO", 5, 7.75m, 7.75m),
        new(24, "8017", "7188241", "KOBLINGSFLANGE / MELLEMSTYKKE, SIDEMONTERET STANDARD, FORREST TIL HØJRE, LANG", 20, 33.10m, 33.10m),
        new(25, "8092", "6034917", "KOBLINGSFLANGE / MELLEMSTYKKE, DREJET UDFØRELSE TIL VENSTRE, STANDARD, Ø", 10, 10.44m, 10.44m),
    ];

    /// "1.014,00" and "−0,58": the app runs with invariant culture, so the
    /// Danish separators and a real minus are put in by hand.
    public static string Kr(decimal d) =>
        (d < 0 ? "−" : "") + Math.Abs(d).ToString("#,##0.00", CultureInfo.InvariantCulture).Replace(',', ' ').Replace('.', ',').Replace(' ', '.');

    public static string Signed(decimal d) => (d > 0 ? "+" : "") + Kr(d);

    public static string OrdersText(Line l) => l.Orders.Length == 0 ? "Ingen åben ordre" : string.Join(", ", l.Orders.Select(o => $"Ordre {o.No}: {o.Qty} {Unit}"));
}
