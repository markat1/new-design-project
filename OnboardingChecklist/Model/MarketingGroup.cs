namespace OnboardingChecklist.Model;

/// The people a price mail can go to. Each brings their company, and the company
/// is what decides which sheet they receive.
public record Person(string Name, string Email, string Company)
{
    public Recipient Compose() =>
        new(Name, Email, Company, Accounting.SheetFor(Company), RecipientStatus.Sent);

    public bool HasSheet => Accounting.HasSheet(Company);
}

public static class MarketingGroup
{
    // Two at Kestrel on purpose: same company, same sheet, two mails.
    public static readonly Person[] All =
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
}
