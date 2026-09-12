namespace OnboardingChecklist.Model;

/// The people a price mail can go to. Each brings their company, and the company
/// is what decides which sheet they receive.
public record Person(string Name, string Email, string Company)
{
    public Recipient Compose() =>
        new(Name, Email, Company, Accounting.SheetFor(Company), RecipientStatus.Sent);

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
    // Two at Kestrel on purpose: same company, same sheet, two mails.
    private static readonly Person[] People =
    [
        new("Bartholomew Featherstonehaugh-Villanueva",
            "bartholomew.featherstonehaugh-villanueva@internationalsystems-engineering.co.uk",
            "Featherstonehaugh-Villanueva International Systems"),
        new("Anna Sørensen", "anna@velarobotics.com", "Vela Robotics"),
        new("Bo Halden", "bo@halden.dk", "Halden & Co."),
        new("Klaus Richter", "einkauf@ferrous-mfg.de", "Ferrous Manufacturing Group"),
        new("Cecilie Nord", "finance@kestrel.io", "Kestrel Analytics"),
        new("Jonas Vik", "jonas@kestrel.io", "Kestrel Analytics"),
        new("Mia Brandt", "ops@brightharbour.co", "Bright Harbour Logistics"),
        new("Ida Solberg", "hei@solbergmedia.no", "Solberg Media"),
    ];

    private static Person Find(string email) => People.First(p => p.Email == email);

    /// The lists as the marketing side keeps them. Bo Halden is on two of them
    /// on purpose: the lists overlap, and a send-out belongs to one of them.
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
    ];

    /// Everybody, whichever list they are on — what the sent mails compose from.
    public static readonly Person[] All = People;
}
