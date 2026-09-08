namespace OnboardingChecklist.Model;

/// The one list both pages read. Sent quotes renders it; the quote flow appends
/// to it, which is the whole connection between the two.
public class QuoteStore
{
    private readonly List<Quote> quotes = [.. Quote.Seed];

    public IReadOnlyList<Quote> All => quotes;

    public event Action? Changed;

    /// Newest first, matching the list's default sort.
    public void Add(Quote q)
    {
        quotes.Insert(0, q);
        Changed?.Invoke();
    }

    /// Turns the draft into a sent quote and empties the draft for the next one.
    public Quote Send(QuoteDraft draft)
    {
        var quote = draft.ToQuote(NextRef(), DateTime.Now.ToString("yyyy-MM-dd"));
        Add(quote);
        draft.Reset();
        return quote;
    }

    /// Continues the seeded numbering rather than restarting at 1.
    public string NextRef()
    {
        var highest = quotes
            .Select(q => int.TryParse(q.Ref.AsSpan(2), out var n) ? n : 0)
            .DefaultIfEmpty(2400)
            .Max();
        return $"Q-{highest + 1}";
    }
}
