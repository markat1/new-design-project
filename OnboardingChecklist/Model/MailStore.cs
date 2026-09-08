namespace OnboardingChecklist.Model;

/// The one list both pages read. Mails renders it; the compose flow appends to
/// it, which is the whole connection between the two.
public class MailStore
{
    private readonly List<Mail> mails = [.. Mail.Seed];

    public IReadOnlyList<Mail> All => mails;

    public event Action? Changed;

    /// Newest first, matching the list's default sort.
    public void Add(Mail m)
    {
        mails.Insert(0, m);
        Changed?.Invoke();
    }

    /// Turns the draft into a sent mail and empties the draft for the next one.
    public Mail Send(MailDraft draft)
    {
        var mail = draft.ToMail(NextRef(), DateTime.Now.ToString("yyyy-MM-dd"));
        Add(mail);
        draft.Reset();
        return mail;
    }

    /// Continues the seeded numbering rather than restarting at 1.
    public string NextRef()
    {
        var highest = mails
            .Select(m => int.TryParse(m.Ref.AsSpan(2), out var n) ? n : 0)
            .DefaultIfEmpty(2400)
            .Max();
        return $"M-{highest + 1}";
    }
}
