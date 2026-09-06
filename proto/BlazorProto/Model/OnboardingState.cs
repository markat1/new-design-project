using System.Text.RegularExpressions;

namespace BlazorProto.Model;

/// Registered as a singleton, so answers survive a variant switch — you type
/// once and compare all three against the same data.
public partial class OnboardingState
{
    // ---- worst content by default: a long real name, a long email ----
    public string Name { get; set; } = "Bartholomew Featherstonehaugh-Villanueva";
    public string Preferred { get; set; } = "Bart";
    public string? Role { get; set; }

    public List<string> Invites { get; } =
    [
        "bartholomew.featherstonehaugh-villanueva@internationalsystems-engineering.co.uk",
        "dev@acme.io",
    ];

    public Dictionary<string, bool> Prefs { get; } = new()
    {
        ["digest"] = true,
        ["mentions"] = true,
        ["updates"] = false,
    };

    public bool TouchedInvites { get; set; } = true;
    public bool TouchedPrefs { get; set; }

    public Dictionary<string, string> Errors { get; } = [];

    // ---- picker + per-variant navigation ----
    public int ActiveVariant { get; set; }
    public int MountCounter { get; set; }

    public int StepperIndex { get; set; }
    public bool StepperDone { get; set; }

    public int ConvoIndex { get; set; }
    public bool ConvoDone { get; set; }

    public string ChecklistKey { get; set; } = "name";
    public bool ChecklistDone { get; set; }

    // ---- derived ----
    public string FirstName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Preferred)) return Preferred.Trim();
            var n = Name.Trim();
            return n.Length > 0 ? n.Split(' ')[0] : "there";
        }
    }

    public bool IsDone(string key) => key switch
    {
        "name" => Name.Trim().Length > 0,
        "role" => !string.IsNullOrEmpty(Role),
        "invites" => TouchedInvites,
        "prefs" => TouchedPrefs,
        _ => false,
    };

    public int DoneCount => Content.Tasks.Count(t => IsDone(t.Key));

    public TaskDef[] RequiredLeft => Content.Tasks.Where(t => t.Required && !IsDone(t.Key)).ToArray();

    public string? Summary(string key) => key switch
    {
        "name" => Name.Trim().Length > 0 ? Name.Trim() : null,
        "role" => Role,
        "invites" => !TouchedInvites ? null
            : Invites.Count == 0 ? "Skipped for now"
            : Invites.Count == 1 ? "1 invite"
            : $"{Invites.Count} invites",
        "prefs" => !TouchedPrefs ? null
            : Prefs.Values.Count(v => v) is var on && on == 0 ? "All email off"
            : $"{Prefs.Values.Count(v => v)} of 3 email types on",
        _ => null,
    };

    // ---- validation, shared verbatim by all three variants so the
    //      comparison isn't rigged by one being stricter than another ----
    public bool Validate(string key)
    {
        Errors.Clear();
        if (key == "name" && Name.Trim().Length == 0)
        {
            Errors["name"] = "We need something to call you.";
            return false;
        }
        if (key == "role" && string.IsNullOrEmpty(Role))
        {
            Errors["role"] = "Pick the closest one — nothing here is binding.";
            return false;
        }
        return true;
    }

    public bool AddInvite(string? raw)
    {
        var v = (raw ?? "").Trim().ToLowerInvariant();
        if (v.Length == 0) { Errors["mail"] = "Type an email address first."; return false; }
        if (!EmailRe().IsMatch(v)) { Errors["mail"] = $"\"{v}\" is not an email address."; return false; }
        if (Invites.Contains(v)) { Errors["mail"] = "That teammate is already on the list."; return false; }

        Invites.Add(v);
        TouchedInvites = true;
        Errors.Remove("mail");
        return true;
    }

    public void RemoveInvite(string mail)
    {
        Invites.Remove(mail);
        TouchedInvites = true;
    }

    public void MarkVisited(string key)
    {
        if (key == "invites") TouchedInvites = true;
        if (key == "prefs") TouchedPrefs = true;
    }

    public void ResetFlow()
    {
        Errors.Clear();
        StepperIndex = 0; StepperDone = false;
        ConvoIndex = 0; ConvoDone = false;
        ChecklistKey = "name"; ChecklistDone = false;
    }

    [GeneratedRegex(@"^[^\s@]+@[^\s@]+\.[^\s@]{2,}$")]
    private static partial Regex EmailRe();
}
