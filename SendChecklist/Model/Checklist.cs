using System.Text.RegularExpressions;

namespace SendChecklist.Model;

public record Person(string Name, string Email, string Company, string Country);

public record MarketingList(string Name, string Sub, Person[] People);

/// One row of the uploaded file. Row is the spreadsheet's own number, so
/// "række 6" is a place you can find in Excel.
public record FileRow(int Row, string Name, string Email, string Company, string Country);

public enum Tone { Ok, Muted, Bad }

public record RowState(FileRow Row, Tone Tone, string Status);

public record StepDef(string Key, string Label, string Title, string Sub);

/// The send-out being put together. One instance for the whole app; a page
/// switch resets it, so both designs start from the same place.
public partial class Checklist
{
    public int MountCounter { get; set; }

    public static readonly StepDef[] Steps =
    [
        new("upload", "Upload", "Upload modtagerlisten", "En Excel- eller PDF-fil med én person pr. række. Hver række bliver til en modtager."),
        new("list", "Marketingliste", "Skal en marketingliste med?", "Valgfrit. Står en person både i filen og på listen, får de kun én mail."),
        new("message", "Besked", "Hvad skal der stå?", "Teksten følger med hver prisliste. {navn} og {firma} bliver udfyldt pr. modtager."),
        new("done", "Klar", "Klar til at sende", "Se det hele igennem. Intet sendes, før du trykker Send."),
    ];

    public int Step { get; set; }
    public string StepKey => Steps[Step].Key;
    public int Furthest { get; set; }
    public string? List { get; set; }
    public string? FileName { get; set; }
    public string? Reading { get; set; }
    public HashSet<int> Off { get; } = [];
    public string Subject { get; set; } = "Jeres priser for 2027";
    public string Body { get; set; } = "Hej {navn}\n\nHer er jeres priser for 2027. Arket er vedhæftet og gælder {firma} alene.\n\nVenlig hilsen";
    public string? Error { get; set; }
    public bool Sent { get; set; }

    // The upload-only page: the file's rows go to the storage system instead.
    public bool Storing { get; set; }
    /// True on the "Kun frigiv" tab: the rows are only released, no draft is made.
    public bool ReleaseOnly { get; set; }
    public int? Stored { get; set; }

    public event Action? Changed;
    public void Notify() => Changed?.Invoke();

    public void Reset()
    {
        Step = Furthest = 0;
        List = null;
        FileName = Reading = Error = null;
        Off.Clear();
        Subject = "Jeres priser for 2027";
        Body = "Hej {navn}\n\nHer er jeres priser for 2027. Arket er vedhæftet og gælder {firma} alene.\n\nVenlig hilsen";
        Sent = false;
        Storing = false; Stored = null; ReleaseOnly = false; DraftId = null;
    }

    // ---- data ----

    private static readonly Person Anna = new("Anna Sørensen", "anna@velarobotics.com", "Vela Robotics", "DK");
    private static readonly Person Bo = new("Bo Halden", "bo@halden.dk", "Halden & Co.", "DK");
    private static readonly Person Cecilie = new("Cecilie Nord", "finance@kestrel.io", "Kestrel Analytics", "SE");
    private static readonly Person Jonas = new("Jonas Vik", "jonas@kestrel.io", "Kestrel Analytics", "SE");
    private static readonly Person Ida = new("Ida Solberg", "hei@solbergmedia.no", "Solberg Media", "NO");
    private static readonly Person Mia = new("Mia Brandt", "ops@brightharbour.co", "Bright Harbour Logistics", "GB");
    private static readonly Person Klaus = new("Klaus Richter", "einkauf@ferrous-mfg.de", "Ferrous Manufacturing Group", "DE");
    private static readonly Person Bart = new("Bartholomew Featherstonehaugh-Villanueva",
        "bartholomew.featherstonehaugh-villanueva@internationalsystems-engineering.co.uk",
        "Featherstonehaugh-Villanueva International Systems", "GB");

    public static readonly MarketingList[] Lists =
    [
        new("Rammeaftale 2027", "8 personer · 7 kunder", [Bart, Anna, Bo, Klaus, Cecilie, Jonas, Mia, Ida]),
        new("Norden", "5 personer · 4 kunder", [Anna, Cecilie, Jonas, Bo, Ida]),
        new("Fragtkunder", "3 personer · 3 kunder", [Mia, Klaus, Bo]),
    ];

    private static readonly HashSet<string> Priced =
    [
        "Vela Robotics", "Kestrel Analytics", "Halden & Co.", "Ferrous Manufacturing Group",
        "Bright Harbour Logistics", "Featherstonehaugh-Villanueva International Systems",
    ];

    /// Every row that breaks something: already on the list, no mail, half a
    /// mail, no country, no price list, and a name too long for its column.
    public static readonly FileRow[] FileRows =
    [
        new(2, "Anna Sørensen", "anna@velarobotics.com", "Vela Robotics", "DK"),
        new(3, "Henrik Lund", "henrik.lund@nordvind-energi.dk", "Nordvind Energi A/S", "DK"),
        new(4, "Sofia Lindqvist", "sofia.lindqvist@kestrel.io", "Kestrel Analytics", "SE"),
        new(5, "Maximiliane Oberhauser-Schwarzenbach", "m.oberhauser-schwarzenbach@ferrous-mfg.de", "Ferrous Manufacturing Group", "DE"),
        new(6, "Marie Holm", "", "Vela Robotics", "DK"),
        new(7, "Torben Ask", "torben@halden", "Halden & Co.", "DK"),
        new(8, "Pieter de Vries", "pieter@brightharbour.co", "Bright Harbour Logistics", ""),
        new(9, "Lars Øberg", "lars@oberg-byg.no", "Øberg Byg AS", "NO"),
    ];

    public const string SampleXlsx = "messe-oktober-2026.xlsx";
    public const string SamplePdf = "deltagerliste-messe.pdf";

    [GeneratedRegex(@"^[^\s@]+@[^\s@]+\.[^\s@]{2,}$")]
    private static partial Regex MailRx();

    public static bool HasSheet(string company) => Priced.Contains(company);

    public bool Pdf => FileName?.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) == true;

    public Person[] ListPeople => Lists.FirstOrDefault(l => l.Name == List)?.People ?? [];

    /// Each row of the file with what happens to it.
    public List<RowState> Rows()
    {
        if (FileName is null) return [];
        var list = ListPeople;
        return FileRows.Select(r =>
        {
            if (r.Email.Length == 0) return new RowState(r, Tone.Bad, "Mangler mail");
            if (!MailRx().IsMatch(r.Email)) return new RowState(r, Tone.Bad, "Mailen er ikke hel");
            if (list.Any(p => p.Email.Equals(r.Email, StringComparison.OrdinalIgnoreCase)))
                return new RowState(r, Tone.Muted, "Står på listen");
            if (Off.Contains(r.Row)) return new RowState(r, Tone.Muted, "Fravalgt");
            return new RowState(r, Tone.Ok, ReleaseOnly ? "Klar" : !HasSheet(r.Company) ? "Klar · ingen prisliste" : r.Country.Length == 0 ? "Klar · engelsk" : "Klar");
        }).ToList();
    }

    public int FileOk => Rows().Count(r => r.Tone == Tone.Ok);
    public int FileBad => Rows().Count(r => r.Tone == Tone.Bad);

    /// The recipient list being built: the marketing list, then the file's new rows.
    public List<Person> Recipients()
    {
        var all = ListPeople.ToList();
        foreach (var r in Rows().Where(r => r.Tone == Tone.Ok))
            all.Add(new Person(r.Row.Name, r.Row.Email, r.Row.Company, r.Row.Country));
        return all;
    }

    public int Customers => Recipients().Select(p => p.Company).Distinct().Count();
    public int Sheets => Recipients().Where(p => HasSheet(p.Company)).Select(p => p.Company).Distinct().Count();

    // ---- moving ----

    public bool Done(int i) => i < Furthest || (i == 3 && Sent);

    /// What stops the step from being left forward, said where it applies.
    public string? Problem(int i) => Steps[i].Key switch
    {
        "list" when Recipients().Count == 0 => "Der er ingen modtagere endnu. Vælg en liste, eller gå tilbage og upload en fil.",
        "message" when Subject.Trim().Length == 0 => "Beskeden mangler en emnelinje.",
        _ => null,
    };

    public static int IndexOf(string key) => Array.FindIndex(Steps, s => s.Key == key);

    public bool Next()
    {
        Error = Problem(Step);
        if (Error is not null) { Notify(); return false; }
        if (Step < Steps.Length - 1) { Step++; Furthest = Math.Max(Furthest, Step); }
        Notify();
        return true;
    }

    public void Back() { if (Step > 0) { Step--; Error = null; Notify(); } }

    public void Go(int i)
    {
        if (i > Furthest) return;   // only steps already reached
        Step = i; Error = null; Notify();
    }

    public void Send()
    {
        Error = Problem(IndexOf("list")) ?? Problem(IndexOf("message"));
        if (Error is null) { Sent = true; Furthest = 3; LogSent(); }
        Notify();
    }

    public void PickList(string? name) { List = name; Error = null; Notify(); }

    public async Task Load(string name)
    {
        Reading = name; Error = null; Notify();
        await Task.Delay(600);
        Reading = null; FileName = name; Off.Clear(); Stored = null; Notify();
    }

    /// From the upload page to a draft: the file is already in, so the
    /// draft opens on the step after it.
    public void MakeDraft()
    {
        ReleaseOnly = false; Stored = null;
        Step = Furthest = IndexOf("list"); Error = null;
        DraftId = Log(Kind.Draft, FileOk);
        Notify();
    }

    public void RemoveFile() { FileName = null; Off.Clear(); Stored = null; Notify(); }

    /// PROTO: stands in for the call to the storage system.
    public async Task Store()
    {
        Storing = true; Notify();
        await Task.Delay(900);
        Storing = false; Stored = FileOk; Log(Kind.Released, FileOk); Notify();
    }

    public void ToggleRow(int row) { if (!Off.Remove(row)) Off.Add(row); Stored = null; Notify(); }

    public static string Lang(string c) => c switch { "DK" => "Dansk", "SE" => "Svensk", "" => "—", _ => "Engelsk" };

    public string Fill(Person p) =>
        Body.Replace("{navn}", p.Name).Replace("{firma}", p.Company);
}
