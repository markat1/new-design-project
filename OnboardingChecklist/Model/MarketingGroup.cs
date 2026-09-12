namespace OnboardingChecklist.Model;

/// The people a price mail can go to. Each brings their company, and the company
/// is what decides which sheet they receive.
/// The country is the customer's own, as the CRM has it, and it is the only
/// thing that decides which of the three letters they get.
public record Person(string Name, string Email, string Company, string Country)
{
    public Recipient Compose() =>
        new(Name, Email, Company, Accounting.SheetFor(Company), RecipientStatus.Sent, Country);

    public string Lang => Templates.For(Country);

    public bool HasSheet => Accounting.HasSheet(Company);
}

/// A marketing list: the people a send-out goes to. The same person can be on
/// more than one, which is why a send names the list it went to.
public record Group(string Name, Person[] People)
{
    public int Customers => People.Select(p => p.Company).Distinct().Count();

    /// The same rule the sent list uses: a sheet counts once per person who
    /// gets it, because each of them gets their own copy attached.
    public decimal Total => People.Sum(p => Accounting.SheetFor(p.Company).Sum(line => line.Total));
}

public static class MarketingGroup
{
    // Two at Kestrel on purpose: same company, same sheet, two mails. And all
    // three languages on purpose too — a send that is Danish all the way
    // through would never show what the template step is for.
    private static readonly Person[] People =
    [
        new("Bartholomew Featherstonehaugh-Villanueva",
            "bartholomew.featherstonehaugh-villanueva@internationalsystems-engineering.co.uk",
            "Featherstonehaugh-Villanueva International Systems", "GB"),
        new("Anna Sørensen", "anna@velarobotics.com", "Vela Robotics", "DK"),
        new("Bo Halden", "bo@halden.dk", "Halden & Co.", "DK"),
        new("Klaus Richter", "einkauf@ferrous-mfg.de", "Ferrous Manufacturing Group", "DE"),
        new("Cecilie Nord", "finance@kestrel.io", "Kestrel Analytics", "SE"),
        new("Jonas Vik", "jonas@kestrel.io", "Kestrel Analytics", "SE"),
        new("Mia Brandt", "ops@brightharbour.co", "Bright Harbour Logistics", "GB"),
        new("Ida Solberg", "hei@solbergmedia.no", "Solberg Media", "NO"),
    ];

    private static Person Find(string email) => People.First(p => p.Email == email);

    /// The lists as the marketing side keeps them. Bo Halden is on two of the
    /// named ones on purpose: lists overlap, and a send-out belongs to one.
    ///
    /// The rest are made up, because three lists prove nothing about a picker
    /// that has to work at a hundred. Same seed every run, so the app looks
    /// the same twice.
    public static readonly Group[] Lists =
    [
        new("Rammeaftale 2027", People),
        new("Fragtkunder",
        [
            Find("ops@brightharbour.co"),
            Find("einkauf@ferrous-mfg.de"),
            Find("bo@halden.dk"),
        ]),
        new("Norden",
        [
            Find("anna@velarobotics.com"),
            Find("finance@kestrel.io"),
            Find("jonas@kestrel.io"),
            Find("bo@halden.dk"),
            Find("hei@solbergmedia.no"),
        ]),
        .. Invented(),
    ];

    /// The names live in here rather than in fields: a static field declared
    /// below Lists is still null while Lists is being built, and the whole
    /// type falls over on first touch.
    private static IEnumerable<Group> Invented()
    {
        string[] themes = ["Rammeaftale", "Fragt", "Service", "Onboarding", "Storkunder", "Nyhedsbrev", "Pilot", "Vedligehold", "Licenser", "Projekt"];
        string[] places = ["Norden", "Danmark syd", "Danmark nord", "Sjælland", "Jylland", "Fyn", "Tyskland", "UK", "Benelux", "Norge"];

        var seed = new Random(2027);

        foreach (var theme in themes)
        {
            foreach (var place in places)
            {
                var members = People.OrderBy(_ => seed.Next()).Take(seed.Next(2, 7)).ToArray();
                yield return new Group($"{theme} {place}", members);
            }
        }
    }

    /// Everybody, whichever list they are on — what the sent mails compose from.
    public static readonly Person[] All = People;
}
