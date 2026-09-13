namespace OnboardingChecklist.Model;

/// The one list both pages read. Mails renders it; the compose flow appends to
/// it, which is the whole connection between the two.
public class MailStore
{
    private readonly List<Mail> mails = [.. Mail.Seed];
    private readonly MailDraft draft;

    /// There is only ever one draft, and a draft saved in the list is it: it is
    /// taken up at start, so the side menu shows it from the first render.
    public MailStore(MailDraft draft)
    {
        this.draft = draft;

        if (mails.FirstOrDefault(m => m.Status == MailStatus.Draft) is { } saved)
        {
            mails.RemoveAll(m => m.Status == MailStatus.Draft && m != saved);
            draft.Load(saved);
        }
    }

    /// The draft's row is drawn from the draft itself, so the list shows what
    /// was just written rather than what was there when it was saved.
    public IReadOnlyList<Mail> All =>
        [.. mails.Select(m => m.Status == MailStatus.Draft ? draft.AsDraft(m.Ref) : m)];

    /// Drawing the draft's row composes every recipient and their sheet, so
    /// whatever only needs the sent send-outs, or how many rows there are, reads
    /// these instead. The marketing-list cards ask once per list, a hundred
    /// times a render: through All, a click on a card froze the app.
    public IEnumerable<Mail> Sent => mails.Where(m => m.Status == MailStatus.Sent);

    public int Count => mails.Count;

    /// The draft's number, kept from the moment it was started until it is sent.
    public string? DraftRef => mails.FirstOrDefault(m => m.Status == MailStatus.Draft)?.Ref;

    /// A new send-out in place of the draft, if there was one: one draft, one row.
    public void StartDraft()
    {
        mails.RemoveAll(m => m.Status == MailStatus.Draft);
        mails.Insert(0, new Mail(NextRef(), "", "", MailStatus.Draft, "—", [], [], []));
        draft.StartOver();
        Changed?.Invoke();
    }

    public event Action? Changed;

    /// Newest first, matching the list's default sort.
    public void Add(Mail m)
    {
        mails.Insert(0, m);
        Changed?.Invoke();
    }

    /// Turns the draft into a sent mail under the number it already had, and
    /// empties the draft for the next one.
    public Mail Send(MailDraft draft)
    {
        var reference = DraftRef ?? NextRef();
        mails.RemoveAll(m => m.Status == MailStatus.Draft);
        var mail = draft.ToMail(reference, DateTime.Now.ToString("yyyy-MM-dd"));
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
