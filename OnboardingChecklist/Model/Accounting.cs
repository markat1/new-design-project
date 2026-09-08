namespace OnboardingChecklist.Model;

/// Stands in for the accounting system. The price sheets are made there, per
/// customer — this app reads them, it never authors them. A real build swaps
/// this one class for a call; nothing else changes.
public static class Accounting
{
    private static readonly Dictionary<string, QuoteLine[]> Sheets = new()
    {
        ["Featherstonehaugh-Villanueva International Systems"] =
        [
            new("Implementering, fase 1", 1, 28_000m),
            new("Licens pr. bruger", 40, 380m),
            new("Support, årligt", 1, 5_000m),
        ],
        ["Vela Robotics"] =
        [
            new("Servicebesøg", 12, 850m),
            new("Reservedelslager", 1, 2_400m),
        ],
        ["Halden & Co."] =
        [
            new("Konsulenttime", 8, 1_175m),
        ],
        ["Ferrous Manufacturing Group"] =
        [
            new("Wartung, monatlich", 12, 1_000m),
        ],
        ["Kestrel Analytics"] =
        [
            new("Platform, årligt", 1, 61_000m),
            new("Onboarding", 1, 12_000m),
        ],
        ["Bright Harbour Logistics"] =
        [
            new("Palleplads pr. måned", 40, 320m),
            new("Håndtering", 1, 3_200m),
        ],
        ["Solberg Media"] = [],
    };

    public static QuoteLine[] SheetFor(string company) =>
        Sheets.TryGetValue(company, out var lines) ? lines : [];

    public static bool HasSheet(string company) => SheetFor(company).Length > 0;
}
