namespace OnboardingChecklist.Model;

public enum QuoteStatus { Draft, Sent, Viewed, Accepted, Declined }

/// One priced mail that has been sent to a client.
public record Quote(string Ref, string Client, QuoteStatus Status, decimal Amount, string Sent)
{
    public string Label => Status.ToString();

    // Deliberately awkward data: a very long client name, a zero-amount draft,
    // and a declined row, so the table has to survive more than the happy case.
    public static readonly Quote[] All =
    [
        new("Q-2418", "Featherstonehaugh-Villanueva International Systems", QuoteStatus.Viewed,   48_200m, "2026-09-02"),
        new("Q-2417", "Vela Robotics",                                     QuoteStatus.Accepted, 12_600m, "2026-09-01"),
        new("Q-2416", "Halden & Co.",                                      QuoteStatus.Sent,        940m, "2026-08-28"),
        new("Q-2415", "Ferrous Manufacturing Group",                       QuoteStatus.Viewed,    1_000m, "2026-08-24"),
        new("Q-2414", "Solberg Media",                                     QuoteStatus.Draft,         0m, "—"),
        new("Q-2413", "Kestrel Analytics",                                 QuoteStatus.Accepted,  7_315m, "2026-08-19"),
        new("Q-2412", "Ondrej Systems",                                    QuoteStatus.Declined,    289m, "2026-08-14"),
        new("Q-2411", "Bright Harbour Logistics",                          QuoteStatus.Sent,      2_560m, "2026-08-11"),
    ];
}
