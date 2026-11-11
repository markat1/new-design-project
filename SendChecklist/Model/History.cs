namespace SendChecklist.Model;

public enum Kind { Released, Draft, Sent }

/// One file that went somewhere: released to the storage system, made into
/// a draft, or sent as price mails.
public record Entry(int Id, string File, DateTime When, Kind Kind, int Rows, int Skipped, string? List);

public partial class Checklist
{
    /// What has been done with uploaded files. Lives outside Reset, so it
    /// survives starting over and moving between pages.
    public List<Entry> History { get; } =
    [
        new(6, "prisliste-modtagere-uge-45.xlsx", new(2026, 11, 10, 15, 42, 0), Kind.Released, 42, 3, null),
        new(5, "messe-oktober-2026.xlsx", new(2026, 11, 10, 9, 15, 0), Kind.Draft, 6, 2, "Norden"),
        new(4, "deltagerliste-messe.pdf", new(2026, 11, 7, 13, 5, 0), Kind.Released, 18, 0, null),
        new(3, "Q4-kampagne_nye-kunder_endelig_v3 (2) - kopi til marketing.xlsx", new(2026, 11, 4, 11, 20, 0), Kind.Sent, 124, 7, "Rammeaftale 2027"),
        new(2, "fragtkunder-norge.xlsx", new(2026, 10, 28, 10, 2, 0), Kind.Sent, 9, 0, "Fragtkunder"),
        new(1, "lager-import-oktober.xlsx", new(2026, 10, 21, 8, 47, 0), Kind.Released, 230, 12, null),
    ];

    /// The draft being worked on in Trinliste, so sending it updates its line.
    public int? DraftId { get; set; }

    private int Skipped => Rows().Count - FileOk;

    private int Log(Kind kind, int rows)
    {
        var id = History.Count == 0 ? 1 : History.Max(e => e.Id) + 1;
        History.Insert(0, new Entry(id, FileName ?? "—", DateTime.Now, kind, rows, Skipped, List));
        return id;
    }

    private void LogSent()
    {
        var i = History.FindIndex(e => e.Id == DraftId);
        var count = Recipients().Count;
        if (i >= 0) { History.RemoveAt(i); }
        Log(Kind.Sent, count);
        DraftId = null;
    }

    /// Opens a line from the overview in Trinliste: a draft carries on, a
    /// released file becomes a new draft. The file itself is the sample rows.
    public void OpenFrom(Entry e)
    {
        Reset();
        FileName = e.File;
        List = e.Kind == Kind.Draft ? e.List : null;
        Step = Furthest = IndexOf("list");
        DraftId = e.Kind == Kind.Draft ? e.Id : Log(Kind.Draft, FileOk);
        Notify();
    }

    private static readonly string[] Months = ["jan.", "feb.", "mar.", "apr.", "maj", "jun.", "jul.", "aug.", "sep.", "okt.", "nov.", "dec."];

    /// "I dag 14:32", "I går 09:15", "4. nov. 11:20" — the app runs with
    /// invariant culture, so the Danish words are written out here.
    public static string When(DateTime d)
    {
        var day = (DateTime.Today - d.Date).Days switch
        {
            0 => "I dag",
            1 => "I går",
            _ => $"{d.Day}. {Months[d.Month - 1]}" + (d.Year != DateTime.Today.Year ? $" {d.Year}" : ""),
        };
        return $"{day} {d:HH:mm}";
    }
}
