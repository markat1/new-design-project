namespace OnboardingChecklist.Model;

/// The mail being written. Registered as a singleton so choices survive
/// re-renders and a trip to the other page and back.
public class MailDraft
{
    public string Subject { get; set; } = "";
    public string Note { get; set; } = "";

    /// A send-out goes to one list, or to two. Never none — a send with no list
    /// has nobody to go to — and never three: past two, nobody can hold in their
    /// head who is about to get a mail.
    public const int MaxLists = 2;

    private readonly List<Group> lists = [MarketingGroup.Lists[0]];

    public IReadOnlyList<Group> Lists => lists;

    public Group List => lists[0];

    public bool Chosen(Group list) => lists.Any(l => l.Name == list.Name);

    public bool RoomForMore => lists.Count < MaxLists;

    /// Everybody the chosen lists hold, each person once. Somebody on both
    /// lists is one mail with one sheet, not two of each.
    public Person[] People => [.. lists.SelectMany(l => l.People).DistinctBy(p => p.Email)];

    /// The people who stand on more than one of the chosen lists. Worth naming
    /// in the interface: it is the one thing about two lists that surprises.
    public Person[] OnBoth =>
        lists.Count < 2 ? [] : [.. People.Where(p => lists.Count(l => l.People.Any(x => x.Email == p.Email)) > 1)];

    public int Customers => People.Select(p => p.Company).Distinct().Count();

    private readonly Dictionary<string, HashSet<string>> picks = [];

    /// Everybody on the list is on the send-out until somebody is taken off:
    /// sending to the whole group is the normal case, and eight ticks to say
    /// so is not a choice, it is a chore. Kept per list, so dropping a list and
    /// picking it up again costs nothing.
    private HashSet<string> PicksFor(Group list) =>
        picks.TryGetValue(list.Name, out var set) ? set : picks[list.Name] = [.. list.People.Select(p => p.Email)];

    public HashSet<string> Picked => [.. lists.SelectMany(PicksFor)];

    /// The person the preview has been asked to show, with a nonce so asking
    /// for the same one twice still counts as asking.
    public (string Email, int Nonce)? Show { get; private set; }

    private int asks;

    public void ShowSheet(string email)
    {
        Show = (email, ++asks);
        NotifyChanged();
    }

    /// Click a list that is already on and it comes off; click another and it
    /// joins — up to two. The last one cannot come off: that would leave the
    /// send-out with nobody on it, and the step has no way back from there.
    public void Toggle(Group list)
    {
        if (Chosen(list))
        {
            if (lists.Count > 1) lists.RemoveAll(l => l.Name == list.Name);
        }
        else if (RoomForMore)
        {
            lists.Add(list);
        }
        else
        {
            return;
        }

        Forget();
        Errors.Remove("recipients");
        NotifyChanged();
    }

    /// The one list, replacing whatever was there. Used where a single answer
    /// is meant — dropping a whole selection for one pick.
    public void Choose(Group list)
    {
        lists.Clear();
        lists.Add(list);
        Forget();
        Errors.Remove("recipients");
        NotifyChanged();
    }

    /// The preview is showing somebody who may no longer be on the send.
    private void Forget()
    {
        if (Show is { } shown && !Picked.Contains(shown.Email)) Show = null;
    }

    /// Name first, but people and companies too: the question is often "which
    /// list has Kestrel on it", and nobody knows that list's name.
    public static IEnumerable<Group> Search(string word)
    {
        var find = word.Trim();
        if (find.Length == 0) return MarketingGroup.Lists;

        return MarketingGroup.Lists.Where(list =>
            list.Name.Contains(find, StringComparison.OrdinalIgnoreCase)
            || list.People.Any(p => p.Name.Contains(find, StringComparison.OrdinalIgnoreCase)
                                 || p.Company.Contains(find, StringComparison.OrdinalIgnoreCase)));
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
        [.. People.Where(p => Picked.Contains(p.Email)).Select(p => p.Compose())];

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

    /// Somebody standing on both lists comes off both at once: one person, one
    /// mail, one tick.
    public void Toggle(string email)
    {
        var off = Picked.Contains(email);

        foreach (var list in lists.Where(l => l.People.Any(p => p.Email == email)))
        {
            if (off) PicksFor(list).Remove(email);
            else PicksFor(list).Add(email);
        }

        Errors.Remove("recipients");

        // Somebody taken off the send has no sheet in the preview to show.
        if (Show?.Email == email && !Picked.Contains(email)) Show = null;
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
        new(reference, Subject.Trim(), Note.Trim(), MailStatus.Sent, sentOn, Recipients, [.. lists.Select(l => l.Name)]);

    /// The draft as it would look sent, for the preview beside the form.
    public Mail Preview(string reference) =>
        new(reference, Subject, Note, MailStatus.Draft,
            DateTime.Now.ToString("yyyy-MM-dd"), Recipients, [.. lists.Select(l => l.Name)]);

    public void Reset()
    {
        Subject = "";
        Note = "";
        picks.Clear();
        lists.Clear();
        lists.Add(MarketingGroup.Lists[0]);
        Show = null;
        TouchedNote = false;
        Errors.Clear();
        StepKey = "recipients";
        Nudge = null;
    }
}
