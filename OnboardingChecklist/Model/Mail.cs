namespace OnboardingChecklist.Model;

public enum MailStatus { Draft, Sent }

public enum RecipientStatus { Sent, Viewed, Accepted, Declined }

public static class RecipientStatusText
{
    public static string Label(this RecipientStatus s) => s switch
    {
        RecipientStatus.Viewed => "Set",
        RecipientStatus.Accepted => "Accepteret",
        RecipientStatus.Declined => "Afvist",
        _ => "Sendt",
    };
}

/// One line in a customer's price sheet.
public record QuoteLine(string Description, int Qty, decimal Unit)
{
    public decimal Total => Qty * Unit;
}

/// Somebody on the send list, with the sheet their company gets. The lines are
/// copied in at send time: what was sent stays what was sent, even after the
/// accounting system moves on.
public record Recipient(string Name, string Email, string Company, QuoteLine[] Lines, RecipientStatus Status)
{
    public decimal Total => Lines.Sum(l => l.Total);

    public string Attachment => $"priser-{Slug(Company)}.xlsx";

    public string AttachmentSize => Lines.Length == 0 ? "—" : $"{12 + Lines.Length * 2} KB";

    public string Initials =>
        string.Concat(Name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                          .Take(2).Select(w => char.ToUpperInvariant(w[0])));

    private static string Slug(string s) =>
        new string(s.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray())
            .Trim('-').Replace("--", "-");
}

/// One send-out: a single mail to a marketing group, where every customer gets
/// their own price sheet attached.
public record Mail(string Ref, string Subject, string Note, MailStatus Status, string Sent, Recipient[] Recipients)
{
    public string Label => Status == MailStatus.Draft ? "Kladde" : "Sendt";

    public decimal Total => Recipients.Sum(r => r.Total);

    public int Companies => Recipients.Select(r => r.Company).Distinct().Count();

    public int CountOf(RecipientStatus s) => Recipients.Count(r => r.Status == s);

    /// "3 set · 1 accepteret", or nothing worth saying yet.
    public string Responses
    {
        get
        {
            var parts = new List<string>();
            if (CountOf(RecipientStatus.Viewed) > 0) parts.Add($"{CountOf(RecipientStatus.Viewed)} set");
            if (CountOf(RecipientStatus.Accepted) > 0) parts.Add($"{CountOf(RecipientStatus.Accepted)} accepteret");
            if (CountOf(RecipientStatus.Declined) > 0) parts.Add($"{CountOf(RecipientStatus.Declined)} afvist");
            return parts.Count > 0 ? string.Join(" · ", parts) : "Ingen svar endnu";
        }
    }

    private static Recipient R(string name, string mail, string company, RecipientStatus s) =>
        new(name, mail, company, Accounting.SheetFor(company), s);

    // Worst content on purpose: a very long company, two people at the same
    // company sharing one sheet, and a draft with nobody on it yet.
    public static readonly Mail[] Seed =
    [
        new("M-2418", "Priser på rammeaftale 2027",
            "Hej — her er priserne på rammeaftalen, som aftalt på mødet. Hver af jer har sit eget ark vedhæftet.",
            MailStatus.Sent, "2026-09-02",
            [
                R("Bartholomew Featherstonehaugh-Villanueva", "bartholomew.featherstonehaugh-villanueva@internationalsystems-engineering.co.uk", "Featherstonehaugh-Villanueva International Systems", RecipientStatus.Viewed),
                R("Anna Sørensen", "anna@velarobotics.com", "Vela Robotics", RecipientStatus.Accepted),
                R("Bo Halden", "bo@halden.dk", "Halden & Co.", RecipientStatus.Sent),
                R("Klaus Richter", "einkauf@ferrous-mfg.de", "Ferrous Manufacturing Group", RecipientStatus.Viewed),
            ]),

        new("M-2417", "Fornyelse 2027",
            "Samme vilkår som sidste år, onboarding er med denne gang.",
            MailStatus.Sent, "2026-08-19",
            [
                R("Cecilie Nord", "finance@kestrel.io", "Kestrel Analytics", RecipientStatus.Accepted),
                R("Jonas Vik", "jonas@kestrel.io", "Kestrel Analytics", RecipientStatus.Viewed),
            ]),

        new("M-2416", "Fragtpriser, revideret",
            "Fragtpriserne er justeret efter den nye rute. Håndteringen er uændret.",
            MailStatus.Sent, "2026-08-11",
            [
                R("Mia Brandt", "ops@brightharbour.co", "Bright Harbour Logistics", RecipientStatus.Declined),
            ]),

        new("M-2415", "(intet emne endnu)", "", MailStatus.Draft, "—", []),
    ];
}
