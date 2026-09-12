using System.Net.Http.Json;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using XlCell = DocumentFormat.OpenXml.Spreadsheet.Cell;
using XlSheet = DocumentFormat.OpenXml.Spreadsheet.Sheet;

namespace OnboardingChecklist.Model;

/// The accounting system's price sheets: one real .xlsx per customer, read
/// with Microsoft's Open XML SDK. This app reads them, it never authors them —
/// a real build points the loader at the system's own files instead of the
/// ones shipped in wwwroot/sheets, and nothing else here changes.
public static partial class Accounting
{
    /// A workbook: every worksheet in it, the size of the file, and where
    /// Office for the web can reach it if it lives somewhere Microsoft may
    /// read. Edited says the app holds newer cells than the file does.
    public record Book(Tab[] Tabs, int Bytes, bool Edited = false, string? Url = null);

    /// A line in the system's own list of what it has made.
    private record Listed(string File, string? Url = null);

    private static readonly Dictionary<string, Book> Books = [];

    /// The mail's figures come off the first worksheet: a line is a row with a
    /// quantity and a price in it, which leaves out the header and the sum.
    public static QuoteLine[] SheetFor(string company) =>
        Books.TryGetValue(company, out var book) && book.Tabs.Length > 0 ? Lines(book.Tabs[0]) : [];

    public static bool HasSheet(string company) => SheetFor(company).Length > 0;

    public static Tab[] TabsFor(string company) =>
        Books.TryGetValue(company, out var book) ? book.Tabs : [];

    public static string FileFor(string company) => $"priser-{Slug(company)}.xlsx";

    public static string SizeFor(string company) =>
        Books.TryGetValue(company, out var book) ? $"{(book.Bytes + 512) / 1024} KB" : "—";

    public static bool IsEdited(string company) => Books.TryGetValue(company, out var book) && book.Edited;

    /// Office for the web, ready to be framed. A link that is already an embed
    /// — the one SharePoint hands out — goes in as it is; anything else is a
    /// file at a public address, which the viewer fetches for itself. Without
    /// a link there is nothing to frame, and the app draws the sheet instead.
    public static string? ViewerFor(string company)
    {
        if (!Books.TryGetValue(company, out var book) || string.IsNullOrWhiteSpace(book.Url)) return null;

        var url = book.Url.Trim();
        return url.Contains("embed", StringComparison.OrdinalIgnoreCase)
            ? url
            : $"https://view.officeapps.live.com/op/embed.aspx?src={Uri.EscapeDataString(url)}";
    }

    /// A cell was typed into. Anything starting with "=" is kept as a formula,
    /// as it would be in Excel; everything else stands as it is written. The
    /// file on disk is untouched until somebody saves it.
    public static void SetCell(string company, int tab, int row, int column, string typed)
    {
        if (!Books.TryGetValue(company, out var book) || tab < 0 || tab >= book.Tabs.Length) return;

        var was = book.Tabs[tab].At(row, column);
        var text = typed.Trim();

        book.Tabs[tab].Put(row, column, text.StartsWith('=')
            ? new Cell("", text[1..], was.Bold)
            : new Cell(text, null, was.Bold));

        Books[company] = book with { Edited = true };
    }

    /// The workbook changed shape rather than content — rows moved — so the
    /// file is behind again.
    public static void Touch(string company)
    {
        if (Books.TryGetValue(company, out var book)) Books[company] = book with { Edited = true };
    }

    /// Read once, before the first screen, so every page below can stay
    /// synchronous. The system lists the workbooks it has made, and a customer
    /// missing from that list simply has no sheet — asking for each file and
    /// letting it 404 would work too, and would log an error per customer who
    /// has not been priced yet.
    public static async Task LoadAll(HttpClient http, IEnumerable<string> companies)
    {
        var made = await http.GetFromJsonAsync<Listed[]>("sheets/index.json") ?? [];

        var reads = companies.Distinct()
            .Select(company => (company, listed: made.FirstOrDefault(l => l.File == FileFor(company))))
            .Where(pair => pair.listed is not null)
            .Select(async pair =>
            {
                var file = await http.GetByteArrayAsync($"sheets/{pair.listed!.File}");
                using var stream = new MemoryStream(file);
                return (pair.company, book: Read(stream, file.Length) with { Url = pair.listed.Url });
            });

        foreach (var (company, book) in await Task.WhenAll(reads))
        {
            Books[company] = book;
        }
    }

    /// Every worksheet in the workbook, in the order the tabs sit in.
    private static Book Read(Stream file, int bytes)
    {
        using var doc = SpreadsheetDocument.Open(file, false);
        var workbook = doc.WorkbookPart ?? throw new InvalidDataException("No workbook part.");
        var strings = workbook.SharedStringTablePart?.SharedStringTable;
        var bold = BoldStyles(workbook);

        var tabs = (workbook.Workbook?.Sheets?.Elements<XlSheet>() ?? [])
            .Select(entry => ReadTab(workbook, entry, strings, bold))
            .ToArray();

        return new Book(tabs, bytes);
    }

    private static Tab ReadTab(WorkbookPart workbook, XlSheet entry, SharedStringTable? strings, HashSet<uint> bold)
    {
        var part = (WorksheetPart)workbook.GetPartById(entry.Id!);
        var tab = new Tab(entry.Name?.Value ?? "Ark", []) { Widths = ColumnWidths(part) };

        foreach (var row in part.Worksheet?.Descendants<Row>() ?? [])
        {
            var at = (int)(row.RowIndex?.Value ?? (uint)(tab.Rows.Count + 1)) - 1;

            foreach (var cell in row.Elements<XlCell>())
            {
                tab.Put(at, Column(cell), new Cell(
                    CellText(cell, strings),
                    cell.CellFormula?.Text,
                    bold.Contains(cell.StyleIndex?.Value ?? 0)));
            }
        }

        return tab;
    }

    /// The widths the sheet carries. Excel writes them as spans — "columns 3
    /// to 4 are 12 wide" — so they are spread out one column each.
    private static double[] ColumnWidths(WorksheetPart part)
    {
        var spans = part.Worksheet?.GetFirstChild<Columns>()?.Elements<Column>().ToArray() ?? [];
        if (spans.Length == 0) return [];

        var widths = new double[spans.Max(span => (int)(span.Max?.Value ?? 0))];

        foreach (var span in spans)
        {
            for (var at = (int)(span.Min?.Value ?? 1); at <= (int)(span.Max?.Value ?? 0); at++)
            {
                if (at - 1 < widths.Length) widths[at - 1] = span.Width?.Value ?? 0;
            }
        }

        return widths;
    }

    /// Excel counts a column in characters of its default font; the screen
    /// counts pixels. Seven to a character plus the cell's own padding is the
    /// conversion everyone uses, and 64px is Excel's own default width.
    public static int WidthPx(Tab tab, int column)
    {
        var width = column < tab.Widths.Length ? tab.Widths[column] : 0;
        return width <= 0 ? 64 : (int)Math.Round(width * 7 + 5);
    }

    /// Which style slots carry a bold font. Read so a heading stays a heading
    /// when the sheet is written back out.
    private static HashSet<uint> BoldStyles(WorkbookPart workbook)
    {
        var styles = workbook.WorkbookStylesPart?.Stylesheet;
        var fonts = styles?.Fonts?.Elements<Font>().ToArray() ?? [];
        var formats = styles?.CellFormats?.Elements<CellFormat>().ToArray() ?? [];

        return [.. Enumerable.Range(0, formats.Length)
            .Where(i => formats[i].FontId?.Value is { } font && font < fonts.Length && fonts[font].Bold is not null)
            .Select(i => (uint)i)];
    }

    private static QuoteLine[] Lines(Tab tab) =>
        [.. Enumerable.Range(0, tab.Rows.Count)
            .Select(row => (row, qty: Formulas.Value(tab, row, 1), unit: Formulas.Value(tab, row, 2)))
            .Where(line => line.qty is not null && line.unit is not null)
            .Select(line => new QuoteLine(tab.At(line.row, 0).Value.Trim(), (int)line.qty!.Value, line.unit!.Value))];

    /// The workbook written back out, every sheet of it, by the same SDK that
    /// read it. Formulas go in as formulas so Excel recalculates on open.
    public static byte[] Write(string company)
    {
        var book = Books[company];

        using var memory = new MemoryStream();
        using (var doc = SpreadsheetDocument.Create(memory, SpreadsheetDocumentType.Workbook))
        {
            var workbook = doc.AddWorkbookPart();
            workbook.Workbook = new Workbook();
            workbook.AddNewPart<WorkbookStylesPart>().Stylesheet = Styles();
            var sheets = workbook.Workbook.AppendChild(new Sheets());

            uint id = 1;
            foreach (var tab in book.Tabs)
            {
                var part = workbook.AddNewPart<WorksheetPart>();
                var rows = new SheetData();
                part.Worksheet = new Worksheet(Widths(), rows);

                for (var r = 0; r < tab.Rows.Count; r++)
                {
                    var row = new Row { RowIndex = (uint)(r + 1) };

                    for (var c = 0; c < tab.Rows[r].Count; c++)
                    {
                        var cell = tab.At(r, c);
                        if (cell.Blank) continue;

                        row.Append(Written($"{ColumnName(c)}{r + 1}", cell));
                    }

                    rows.Append(row);
                }

                sheets.AppendChild(new XlSheet { Id = workbook.GetIdOfPart(part), SheetId = id, Name = tab.Name });
                id++;
            }

            workbook.Workbook.Save();
        }

        var file = memory.ToArray();
        Books[company] = book with { Bytes = file.Length, Edited = false };
        return file;
    }

    private static XlCell Written(string reference, Cell cell)
    {
        var money = Formulas.Number(cell.Value) is not null || cell.Formula is not null;
        var style = (cell.Bold, money) switch
        {
            (true, true) => HeadMoney,
            (true, false) => Head,
            (false, true) => Money,
            _ => Plain,
        };

        if (cell.Formula is not null) return Formula(reference, cell.Formula, style);

        return Formulas.Number(cell.Value) is { } number
            ? Num(reference, number, style)
            : Str(reference, cell.Value, style);
    }

    /// 0 is A, 26 is AA. These sheets are four columns wide, but the rule is
    /// cheap and the grid is not ours to limit.
    public static string ColumnName(int column)
    {
        var name = "";
        for (var rest = column; rest >= 0; rest = rest / 26 - 1)
        {
            name = (char)('A' + rest % 26) + name;
        }

        return name;
    }

    // Style slots, in the order Styles() lists them.
    private const uint Plain = 0, Head = 1, Money = 2, HeadMoney = 3;

    private static Columns Widths() =>
        new(new Column { Min = 1, Max = 1, Width = 34, CustomWidth = true },
            new Column { Min = 2, Max = 2, Width = 8, CustomWidth = true },
            new Column { Min = 3, Max = 4, Width = 12, CustomWidth = true });

    /// The parts go in the order the format wants them, and the two fills and
    /// the Normal style are the ones Excel expects to find in every workbook —
    /// leave them out and it offers to repair the file on the way in.
    private static Stylesheet Styles() => new(
        new NumberingFormats(new NumberingFormat { NumberFormatId = 164, FormatCode = "#,##0" }),
        new Fonts(new Font(), new Font(new Bold())),
        new Fills(
            new Fill(new PatternFill { PatternType = PatternValues.None }),
            new Fill(new PatternFill { PatternType = PatternValues.Gray125 })),
        new Borders(new Border()),
        new CellStyleFormats(new CellFormat()),
        new CellFormats(
            new CellFormat(),
            new CellFormat { FontId = 1, ApplyFont = true },
            new CellFormat { NumberFormatId = 164, ApplyNumberFormat = true },
            new CellFormat { FontId = 1, ApplyFont = true, NumberFormatId = 164, ApplyNumberFormat = true }),
        new CellStyles(new CellStyle { Name = "Normal", FormatId = 0, BuiltinId = 0 }));

    private static XlCell Str(string reference, string value, uint style = Plain) => new()
    {
        CellReference = reference,
        StyleIndex = style,
        DataType = CellValues.InlineString,
        InlineString = new InlineString(new Text(value)),
    };

    private static XlCell Num(string reference, decimal value, uint style = Plain) => new()
    {
        CellReference = reference,
        StyleIndex = style,
        DataType = CellValues.Number,
        CellValue = new CellValue(value),
    };

    private static XlCell Formula(string reference, string formula, uint style = Plain) => new()
    {
        CellReference = reference,
        StyleIndex = style,
        CellFormula = new CellFormula(formula),
    };

    /// "B7" names column B, counting from zero. The letters lead, so the
    /// digits end them.
    private static int Column(XlCell cell)
    {
        var reference = cell.CellReference?.Value ?? "";
        var letters = reference.TakeWhile(char.IsLetter).Count();

        return reference[..letters].ToUpperInvariant()
            .Aggregate(0, (value, letter) => value * 26 + (letter - 'A' + 1)) - 1;
    }

    /// Text lives in a shared table, numbers in the cell. A formula cell holds
    /// the last value Excel worked out — the SDK reads files, it does not
    /// recalculate them, so Formulas works the value out instead.
    private static string CellText(XlCell cell, SharedStringTable? strings)
    {
        var value = cell.CellValue?.InnerText ?? cell.InlineString?.Text?.Text ?? "";

        if (cell.DataType?.Value == CellValues.SharedString && strings is not null
            && int.TryParse(value, out var index) && index < strings.Count())
        {
            return strings.ElementAt(index).InnerText;
        }

        return value;
    }

    /// "Halden & Co." has three non-letters in a row, so the runs have to
    /// collapse — a single Replace("--", "-") leaves halden--co behind.
    private static string Slug(string s) =>
        SlugRuns().Replace(
            new string(s.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray()),
            "-").Trim('-');

    [GeneratedRegex("-{2,}")]
    private static partial Regex SlugRuns();
}
