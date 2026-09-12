namespace OnboardingChecklist.Model;

/// The mail being written. Registered as a singleton so choices survive
/// re-renders and a trip to the other page and back.
public class MailDraft
{
    public string Subject { get; set; } = "";
    public string Note { get; set; } = "";

    /// The marketing list this send-out goes to. One send, one list.
    public Group List { get; private set; } = MarketingGroup.Lists[0];

    private readonly Dictionary<string, HashSet<string>> picks = [];

    /// Everybody on the list is on the send-out until somebody is taken off:
    /// sending to the whole group is the normal case, and eight ticks to say
    /// so is not a choice, it is a chore. Kept per list, so looking at another
    /// one and coming back costs nothing.
    public HashSet<string> Picked =>
        picks.TryGetValue(List.Name, out var set) ? set : picks[List.Name] = [.. List.People.Select(p => p.Email)];

    public void Choose(Group list)
    {
        List = list;
        Errors.Remove("recipients");
        NotifyChanged();
    }

    public bool TouchedNote { get; set; }

    public Dictionary<string, string> Errors { get; } = [];

    // ---- shared navigation ----
    // The steps live in the app sidebar and the fields live in the panel, so
    // neither is the other's parent. They talk through this event instead.
    public event Action? Changed;
    public void NotifyChanged() => Changed?.Invoke();

    /// Set when a step should be drawn as blocking; cleared after one render.
    public string? Nudge { get; set; }

    /// Asks the panel to move focus into its first field after the next render.
    public bool FocusNext { get; set; }

    public string StepKey { get; set; } = "recipients";

    public Recipient[] Recipients =>
        [.. List.People.Where(p => Picked.Contains(p.Email)).Select(p => p.Compose())];

    public decimal Total => Recipients.Sum(r => r.Total);

    public int Companies => Recipients.Select(r => r.Company).Distinct().Count();

    public bool IsDone(string key) => key switch
    {
        "recipients" => Picked.Count > 0,
        "subject" => Subject.Trim().Length > 0,
        "note" => TouchedNote,
        _ => false,
    };

    public int DoneCount => Content.Tasks.Count(t => IsDone(t.Key));

    public TaskDef[] RequiredLeft => Content.Tasks.Where(t => t.Required && !IsDone(t.Key)).ToArray();

    public string? Summary(string key) => key switch
    {
        "recipients" => Picked.Count == 0 ? null
            : Companies == 1 ? $"{Picked.Count} hos 1 kunde"
            : $"{Picked.Count} hos {Companies} kunder",
        "subject" => Subject.Trim().Length > 0 ? Subject.Trim() : null,
        "note" => !TouchedNote ? null
            : Note.Trim().Length == 0 ? "Ingen besked"
            : Note.Trim(),
        _ => null,
    };

    public bool Validate(string key)
    {
        Errors.Clear();

        if (key == "recipients" && Picked.Count == 0)
        {
            Errors["recipients"] = "Vælg mindst én modtager.";
            return false;
        }

        if (key == "subject" && Subject.Trim().Length == 0)
        {
            Errors["subject"] = "Emnelinjen er det første modtagerne ser.";
            return false;
        }

        return true;
    }

    public void Toggle(string email)
    {
        if (!Picked.Remove(email)) Picked.Add(email);
        Errors.Remove("recipients");
    }

    public void MarkVisited(string key)
    {
        if (key == "note") TouchedNote = true;
    }

    public void Open(string key)
    {
        StepKey = key;
        Errors.Clear();
        FocusNext = true;
        NotifyChanged();
    }

    /// Walks to the first required step still missing an answer, or reports that
    /// the mail is ready to send.
    public bool ReadyToSend()
    {
        var left = RequiredLeft;
        if (left.Length == 0) return true;

        StepKey = left[0].Key;
        Nudge = left[0].Key;
        Errors.Clear();
        Validate(left[0].Key);
        FocusNext = true;
        NotifyChanged();
        return false;
    }

    public Mail ToMail(string reference, string sentOn) =>
        new(reference, Subject.Trim(), Note.Trim(), MailStatus.Sent, sentOn, Recipients);

    /// The draft as it would look sent, for the preview beside the form.
    public Mail Preview(string reference) =>
        new(reference, Subject, Note, MailStatus.Draft,
            DateTime.Now.ToString("yyyy-MM-dd"), Recipients);

    public void Reset()
    {
        Subject = "";
        Note = "";
        picks.Clear();
        List = MarketingGroup.Lists[0];
        TouchedNote = false;
        Errors.Clear();
        StepKey = "recipients";
        Nudge = null;
    }
}
