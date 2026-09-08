namespace OnboardingChecklist.Model;

public enum QuoteStatus { Draft, Sent, Viewed, Accepted, Declined }

/// One line in the attached price sheet.
public record QuoteLine(string Description, int Qty, decimal Unit)
{
    public decimal Total => Qty * Unit;
}

/// One priced mail. What was actually sent is the spreadsheet, so its lines live
/// here and the detail view can show them without opening anything.
public record Quote(
    string Ref,
    string Client,
    string To,
    string Subject,
    QuoteStatus Status,
    string Sent,
    QuoteLine[] Lines)
{
    public string Label => Status.ToString();

    /// Derived, never stored — a total that can drift from its lines is a bug waiting.
    public decimal Amount => Lines.Sum(l => l.Total);

    public string Attachment => $"priser-{Ref}.xlsx";

    public string AttachmentSize => Lines.Length == 0 ? "—" : $"{12 + Lines.Length * 2} KB";

    // Worst content on purpose: a very long client and recipient, a draft with
    // nothing in it, and a declined row. The store starts from these.
    public static readonly Quote[] Seed =
    [
        new("Q-2418", "Featherstonehaugh-Villanueva International Systems",
            "bartholomew.featherstonehaugh-villanueva@internationalsystems-engineering.co.uk",
            "Priser på rammeaftale 2027", QuoteStatus.Viewed, "2026-09-02",
            [new("Implementering, fase 1", 1, 28_000m), new("Licens pr. bruger", 40, 380m),
             new("Support, årligt", 1, 5_000m)]),

        new("Q-2417", "Vela Robotics", "procurement@velarobotics.com",
            "Tilbud: servicepakke Q4", QuoteStatus.Accepted, "2026-09-01",
            [new("Servicebesøg", 12, 850m), new("Reservedelslager", 1, 2_400m)]),

        new("Q-2416", "Halden & Co.", "kontakt@halden.dk",
            "Opdaterede priser", QuoteStatus.Sent, "2026-08-28",
            [new("Konsulenttime", 8, 1_175m)]),

        new("Q-2415", "Ferrous Manufacturing Group", "einkauf@ferrous-mfg.de",
            "Angebot: Wartungsvertrag", QuoteStatus.Viewed, "2026-08-24",
            [new("Wartung, monatlich", 12, 1_000m)]),

        new("Q-2414", "Solberg Media", "hei@solbergmedia.no",
            "(intet emne endnu)", QuoteStatus.Draft, "—", []),

        new("Q-2413", "Kestrel Analytics", "finance@kestrel.io",
            "Renewal pricing 2027", QuoteStatus.Accepted, "2026-08-19",
            [new("Platform, årligt", 1, 61_000m), new("Onboarding", 1, 12_000m)]),

        new("Q-2412", "Ondrej Systems", "dev@acme.io",
            "Lille tilkøb", QuoteStatus.Declined, "2026-08-14",
            [new("Ekstra sæde", 2, 1_445m)]),

        new("Q-2411", "Bright Harbour Logistics", "ops@brightharbour.co",
            "Fragtpriser, revideret", QuoteStatus.Sent, "2026-08-11",
            [new("Palleplads pr. måned", 40, 320m), new("Håndtering", 1, 3_200m)]),
    ];
}
