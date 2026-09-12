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
public record Recipient(string Name, string Email, string Company, QuoteLine[] Lines, RecipientStatus Status, string Country)
{
    public decimal Total => Lines.Sum(l => l.Total);

    /// Which of the three letters this person got, decided by their country.
    public string Lang => Templates.For(Country);

    public string Attachment => Accounting.FileFor(Company);

    /// The workbook's real size on disk, not an estimate of one.
    public string AttachmentSize => Accounting.SizeFor(Company);

    public string Initials =>
        string.Concat(Name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                          .Take(2).Select(w => char.ToUpperInvariant(w[0])));

}

/// One send-out: a single mail to one or two marketing lists, where every
/// customer gets their own price sheet attached. Two lists is the exception the
/// rule allows — somebody on both is still one mail.
public record Mail(string Ref, string Subject, string Note, MailStatus Status, string Sent, Recipient[] Recipients, string[] Lists, Letter[] Letters)
{
    public string ListNames => string.Join(" + ", Lists);

    /// The languages this send-out actually went out in, in template order.
    public string[] Langs => [.. Templates.All.Select(t => t.Code).Where(c => Recipients.Any(r => r.Lang == c))];

    /// The letter one person got, with their name and company put in. The
    /// headline texts stand in for a send-out written before the templates did.
    public Letter LetterFor(Recipient person)
    {
        var letter = Letters.FirstOrDefault(l => l.Lang == person.Lang) ?? new Letter(person.Lang, Subject, Note);

        return letter with
        {
            Subject = Templates.Fill(letter.Subject, person.Name, person.Company),
            Body = Templates.Fill(letter.Body, person.Name, person.Company),
        };
    }

    public string Label => Status == MailStatus.Draft ? "Kladde" : "Sendt";

    public decimal Total => Recipients.Sum(r => r.Total);

    public int Companies => Recipients.Select(r => r.Company).Distinct().Count();

    public int CountOf(RecipientStatus s) => Recipients.Count(r => r.Status == s);

    /// "3 set · 1 accepteret". A draft has not asked anybody, so it is not
    /// waiting on anything — saying "no replies yet" would imply it went out.
    public string Responses
    {
        get
        {
            if (Status == MailStatus.Draft) return "Ikke sendt";

            var parts = new List<string>();
            if (CountOf(RecipientStatus.Viewed) > 0) parts.Add($"{CountOf(RecipientStatus.Viewed)} set");
            if (CountOf(RecipientStatus.Accepted) > 0) parts.Add($"{CountOf(RecipientStatus.Accepted)} accepteret");
            if (CountOf(RecipientStatus.Declined) > 0) parts.Add($"{CountOf(RecipientStatus.Declined)} afvist");
            return parts.Count > 0 ? string.Join(" · ", parts) : "Ingen svar endnu";
        }
    }

    private static Recipient R(string name, string mail, string company, RecipientStatus s, string country) =>
        new(name, mail, company, Accounting.SheetFor(company), s, country);

    // Worst content on purpose: a very long company, two people at the same
    // company sharing one sheet, a send-out that went to two lists at once,
    // and a draft with nobody on it yet.
    public static readonly Mail[] Seed =
    [
        new("M-2418", "Priser på rammeaftale 2027",
            "Hej — her er priserne på rammeaftalen, som aftalt på mødet. Hver af jer har sit eget ark vedhæftet.",
            MailStatus.Sent, "2026-09-02",
            [
                R("Bartholomew Featherstonehaugh-Villanueva", "bartholomew.featherstonehaugh-villanueva@internationalsystems-engineering.co.uk", "Featherstonehaugh-Villanueva International Systems", RecipientStatus.Viewed, "GB"),
                R("Anna Sørensen", "anna@velarobotics.com", "Vela Robotics", RecipientStatus.Accepted, "DK"),
                R("Bo Halden", "bo@halden.dk", "Halden & Co.", RecipientStatus.Sent, "DK"),
                R("Klaus Richter", "einkauf@ferrous-mfg.de", "Ferrous Manufacturing Group", RecipientStatus.Viewed, "DE"),
            ], ["Rammeaftale 2027"],
            [
                new(Templates.Da, "Priser på rammeaftale 2027",
                    "Hej {navn}\n\nHer er priserne på rammeaftalen, som aftalt på mødet. {firma} har sit eget ark vedhæftet.\n\nVenlig hilsen"),
                new(Templates.En, "Prices for the 2027 framework agreement",
                    "Hi {navn}\n\nHere are the framework prices we agreed on. The sheet attached is {firma}'s own.\n\nKind regards"),
            ]),

        new("M-2417", "Fornyelse 2027",
            "Samme vilkår som sidste år, onboarding er med denne gang.",
            MailStatus.Sent, "2026-08-19",
            [
                R("Cecilie Nord", "finance@kestrel.io", "Kestrel Analytics", RecipientStatus.Accepted, "SE"),
                R("Jonas Vik", "jonas@kestrel.io", "Kestrel Analytics", RecipientStatus.Viewed, "SE"),
            ], ["Rammeaftale 2027", "Norden"],
            [
                new(Templates.Sv, "Förnyelse 2027",
                    "Hej {navn}\n\nSamma villkor som förra året, onboarding ingår den här gången. Arket gäller {firma}.\n\nVänliga hälsningar"),
            ]),

        new("M-2416", "Fragtpriser, revideret",
            "Fragtpriserne er justeret efter den nye rute. Håndteringen er uændret.",
            MailStatus.Sent, "2026-08-11",
            [
                R("Mia Brandt", "ops@brightharbour.co", "Bright Harbour Logistics", RecipientStatus.Declined, "GB"),
            ], ["Fragtkunder"],
            [
                new(Templates.En, "Freight prices, revised",
                    "Hi {navn}\n\nFreight is adjusted for the new route; handling is unchanged. The sheet attached is {firma}'s own.\n\nKind regards"),
            ]),

        new("M-2415", "(intet emne endnu)", "", MailStatus.Draft, "—", [], ["Norden"], []),
    ];
}
