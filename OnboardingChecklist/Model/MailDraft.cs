namespace OnboardingChecklist.Model;

/// The mail being written. Registered as a singleton so choices survive
/// re-renders and a trip to the other page and back.
public class MailDraft
{
    // ---- the letters ----
    // One letter per language in play, started from the company's own template
    // and editable from there. Kept by language, not by recipient: everybody in
    // Sweden gets the same letter, with their own name in it.
    private readonly Dictionary<string, Letter> letters = [];

    /// The languages this send-out will actually go out in, in template order.
    /// Nobody chooses them — the recipients' countries do.
    public string[] Langs => [.. Templates.All.Select(t => t.Code).Where(c => Recipients.Any(r => r.Lang == c))];

    public Letter LetterFor(string lang)
    {
        if (letters.TryGetValue(lang, out var written)) return written;

        var template = Templates.Get(lang);
        return letters[lang] = new Letter(lang, template.Subject, template.Body);
    }

    public void Write(string lang, string? subject = null, string? body = null)
    {
        var letter = LetterFor(lang);
        letters[lang] = letter with { Subject = subject ?? letter.Subject, Body = body ?? letter.Body };
        Errors.Remove("template");
    }

    /// True once the letter differs from the template it started as, which is
    /// the only thing worth saying about it in a summary.
    public bool Edited(string lang) =>
        letters.TryGetValue(lang, out var written) && written != new Letter(lang, Templates.Get(lang).Subject, Templates.Get(lang).Body);

    /// What the list of send-outs shows: the first language in play, because a
    /// send-out has one headline and three letters.
    public string Subject => Langs.Length == 0 ? "" : LetterFor(Langs[0]).Subject;

    public string Note => Langs.Length == 0 ? "" : LetterFor(Langs[0]).Body;

    // ---- how it goes out ----
    public bool ViaCrm { get; set; } = true;
    public bool SaveLocally { get; set; }
    public bool CopyToMe { get; set; }

    public bool AnyDestination => ViaCrm || SaveLocally;

    /// A send-out goes to one list, or to two. Never three: past two, nobody can
    /// hold in their head who is about to get a mail. None is allowed while you
    /// are still choosing — a control that refuses to let go of its own default
    /// is a control you have to fight — and the send itself is what insists on
    /// at least one.
    public const int MaxLists = 2;

    private readonly List<Group> lists = [MarketingGroup.Lists[0]];

    public IReadOnlyList<Group> Lists => lists;

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
    /// for the same one twice still counts as asking. Which pane opens is the
    /// step's business, not the ask's: their sheet while you pick recipients,
    /// their letter while you write it.
    public (string Email, int Nonce)? Show { get; private set; }

    private int asks;

    public void ShowPerson(string email)
    {
        Show = (email, ++asks);
        NotifyChanged();
    }

    /// Click a list that is already on and it comes off — including the last
    /// one. Swapping one list for another is otherwise add-then-remove, with a
    /// moment in the middle where the send-out goes to both.
    public void Toggle(Group list)
    {
        if (Chosen(list)) lists.RemoveAll(l => l.Name == list.Name);
        else if (RoomForMore) lists.Add(list);
        else return;

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
        // The templates arrive written, so this step starts done. That is the
        // truth: there is a letter for everybody. It comes undone only if you
        // empty one.
        "template" => Langs.Length > 0 && Langs.All(l => LetterFor(l).Subject.Trim().Length > 0),
        "send" => Picked.Count > 0 && AnyDestination,
        _ => false,
    };

    public int DoneCount => Content.Tasks.Count(t => IsDone(t.Key));

    public TaskDef[] RequiredLeft => Content.Tasks.Where(t => t.Required && !IsDone(t.Key)).ToArray();

    public string? Summary(string key) => key switch
    {
        "recipients" => Picked.Count == 0 ? null
            : Companies == 1 ? $"{Picked.Count} hos 1 kunde"
            : $"{Picked.Count} hos {Companies} kunder",
        "template" => Langs.Length == 0 ? null
            : Langs.Length == 1 ? $"{Templates.Name(Langs[0])}"
            : string.Join(" · ", Langs.Select(Templates.Name)),
        "send" => Picked.Count == 0 ? null : string.Join(" · ", Destinations),
        _ => null,
    };

    /// Where the send-out ends up, in the order it happens.
    public IEnumerable<string> Destinations
    {
        get
        {
            if (ViaCrm) yield return "CRM";
            if (SaveLocally) yield return "Gemt lokalt";
            if (CopyToMe) yield return "Kopi til mig";
            if (!ViaCrm && !SaveLocally) yield return "Ingen steder endnu";
        }
    }

    public bool Validate(string key)
    {
        Errors.Clear();

        if (key == "recipients" && Picked.Count == 0)
        {
            Errors["recipients"] = lists.Count == 0
                ? "Vælg den liste, udsendelsen skal gå til."
                : "Vælg mindst én modtager.";
            return false;
        }

        if (key == "template" && Langs.FirstOrDefault(l => LetterFor(l).Subject.Trim().Length == 0) is { } empty)
        {
            Errors["template"] = $"Den {Templates.Name(empty).ToLowerInvariant()} skabelon mangler en emnelinje.";
            return false;
        }

        if (key == "send" && !AnyDestination)
        {
            Errors["send"] = "Vælg mindst ét sted, udsendelsen skal ende.";
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

    public void MarkVisited(string key) { }

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
        new(reference, Subject.Trim(), Note.Trim(), MailStatus.Sent, sentOn, Recipients,
            [.. lists.Select(l => l.Name)], [.. Langs.Select(LetterFor)]);

    /// The draft as it would look sent, for the preview beside the form.
    public Mail Preview(string reference) =>
        new(reference, Subject, Note, MailStatus.Draft,
            DateTime.Now.ToString("yyyy-MM-dd"), Recipients,
            [.. lists.Select(l => l.Name)], [.. Langs.Select(LetterFor)]);

    public void Reset()
    {
        letters.Clear();
        ViaCrm = true;
        SaveLocally = false;
        CopyToMe = false;
        picks.Clear();
        lists.Clear();
        lists.Add(MarketingGroup.Lists[0]);
        Show = null;
        Errors.Clear();
        StepKey = "recipients";
        Nudge = null;
    }
}
