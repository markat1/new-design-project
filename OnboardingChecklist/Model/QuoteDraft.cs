using System.Text.RegularExpressions;

namespace OnboardingChecklist.Model;

/// The quote being written. Registered as a singleton so answers survive
/// re-renders and a trip to the other page and back.
public partial class QuoteDraft
{
    public string Client { get; set; } = "";
    public string To { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Note { get; set; } = "";

    public List<QuoteLine> Lines { get; } = [];

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

    public string StepKey { get; set; } = "client";
    public bool Sent { get; set; }

    public decimal Total => Lines.Sum(l => l.Total);

    public bool IsDone(string key) => key switch
    {
        "client" => Client.Trim().Length > 0,
        "mail" => To.Trim().Length > 0 && Subject.Trim().Length > 0,
        "lines" => Lines.Any(l => l.Description.Trim().Length > 0 && l.Unit > 0),
        "note" => TouchedNote,
        _ => false,
    };

    public int DoneCount => Content.Tasks.Count(t => IsDone(t.Key));

    public TaskDef[] RequiredLeft => Content.Tasks.Where(t => t.Required && !IsDone(t.Key)).ToArray();

    public string? Summary(string key) => key switch
    {
        "client" => Client.Trim().Length > 0 ? Client.Trim() : null,
        "mail" => To.Trim().Length > 0 ? To.Trim() : null,
        "lines" => Lines.Count == 0 ? null
            : Lines.Count == 1 ? $"1 linje · {Total:N0}"
            : $"{Lines.Count} linjer · {Total:N0}",
        "note" => !TouchedNote ? null
            : Note.Trim().Length == 0 ? "Ingen besked"
            : Note.Trim(),
        _ => null,
    };

    // ---- validation ----
    public bool Validate(string key)
    {
        Errors.Clear();

        if (key == "client" && Client.Trim().Length == 0)
        {
            Errors["client"] = "Skriv hvem tilbuddet er til.";
            return false;
        }

        if (key == "mail")
        {
            if (!EmailRe().IsMatch(To.Trim().ToLowerInvariant()))
            {
                Errors["to"] = To.Trim().Length == 0
                    ? "Der skal en modtager på."
                    : $"\"{To.Trim()}\" er ikke en e-mailadresse.";
                return false;
            }
            if (Subject.Trim().Length == 0)
            {
                Errors["subject"] = "Emnelinjen er det første modtageren ser.";
                return false;
            }
        }

        if (key == "lines" && !IsDone("lines"))
        {
            Errors["lines"] = "Der skal mindst være én linje med en beskrivelse og en pris.";
            return false;
        }

        return true;
    }

    public void AddLine()
    {
        Lines.Add(new QuoteLine("", 1, 0));
        Errors.Remove("lines");
    }

    public void RemoveLine(int index)
    {
        if (index >= 0 && index < Lines.Count) Lines.RemoveAt(index);
    }

    public void SetLine(int index, QuoteLine line)
    {
        if (index >= 0 && index < Lines.Count) Lines[index] = line;
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
    /// the quote is ready to send.
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

    /// Everything the flow collected, as the thing the list stores.
    public Quote ToQuote(string reference, string sentOn) =>
        new(reference, Client.Trim(), To.Trim().ToLowerInvariant(), Subject.Trim(),
            QuoteStatus.Sent, sentOn,
            [.. Lines.Where(l => l.Description.Trim().Length > 0 && l.Unit > 0)]);

    public void Reset()
    {
        Client = "";
        To = "";
        Subject = "";
        Note = "";
        Lines.Clear();
        TouchedNote = false;
        Errors.Clear();
        StepKey = "client";
        Sent = false;
        Nudge = null;
    }

    [GeneratedRegex(@"^[^\s@]+@[^\s@]+\.[^\s@]{2,}$")]
    private static partial Regex EmailRe();
}
