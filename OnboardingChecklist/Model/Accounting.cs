using System.Globalization;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using XlSheet = DocumentFormat.OpenXml.Spreadsheet.Sheet;

namespace OnboardingChecklist.Model;

/// The accounting system's price sheets: one real .xlsx per customer, read
/// with Microsoft's Open XML SDK. This app reads them, it never authors them —
/// a real build points the loader at the system's own files instead of the
/// ones shipped in wwwroot/sheets, and nothing else here changes.
public static partial class Accounting
{
    /// A workbook as this app needs it: the worksheet's name, the lines the
    /// mail quotes, and the size of the file on disk. Edited says the two have
    /// parted ways — the app holds newer lines than the file does.
    public record Sheet(string Name, QuoteLine[] Lines, int Bytes, bool Edited = false, string? Url = null);

    /// A line in the system's own list of what it has made: the workbook, and
    /// where Office for the web can reach it, when it is somewhere Microsoft
    /// is allowed to read — a SharePoint embed link, or a public address.
    private record Listed(string File, string? Url = null);

    private static readonly Dictionary<string, Sheet> Books = [];

    public static QuoteLine[] SheetFor(string company) =>
        Books.TryGetValue(company, out var book) ? book.Lines : [];

    public static bool HasSheet(string company) => SheetFor(company).Length > 0;

    /// The worksheet's own name, off the tab inside the workbook.
    public static string SheetName(string company) =>
        Books.TryGetValue(company, out var book) ? book.Name : "Ark1";

    public static string FileFor(string company) => $"priser-{Slug(company)}.xlsx";

    public static string SizeFor(string company) =>
        Books.TryGetValue(company, out var book) ? $"{(book.Bytes + 512) / 1024} KB" : "—";

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

    public static bool IsEdited(string company) => Books.TryGetValue(company, out var book) && book.Edited;

    /// A cell was changed in the app. The workbook on disk is untouched until
    /// somebody saves it, which is what Edited is there to say.
    public static void Replace(string company, QuoteLine[] lines)
    {
        if (Books.TryGetValue(company, out var book))
        {
            Books[company] = book with { Lines = lines, Edited = true };
        }
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

    /// The first worksheet, top to bottom. A line is a row with a quantity and
    /// a price in it, which leaves out the header and the sum underneath.
    private static Sheet Read(Stream file, int bytes)
    {
        using var doc = SpreadsheetDocument.Open(file, false);
        var workbook = doc.WorkbookPart ?? throw new InvalidDataException("No workbook part.");
        var first = workbook.Workbook?.Sheets?.Elements<XlSheet>().FirstOrDefault()
                    ?? throw new InvalidDataException("No worksheet.");
        var sheet = (WorksheetPart)workbook.GetPartById(first.Id!);
        var strings = workbook.SharedStringTablePart?.SharedStringTable;

        var lines = new List<QuoteLine>();
        foreach (var row in sheet.Worksheet?.Descendants<Row>() ?? [])
        {
            var cells = row.Elements<Cell>().ToDictionary(Column, c => CellText(c, strings));

            if (!cells.TryGetValue("B", out var qty) || !int.TryParse(qty, NumberStyles.Any, CultureInfo.InvariantCulture, out var count)) continue;
            if (!cells.TryGetValue("C", out var price) || !decimal.TryParse(price, NumberStyles.Any, CultureInfo.InvariantCulture, out var unit)) continue;

            lines.Add(new QuoteLine(cells.GetValueOrDefault("A", "").Trim(), count, unit));
        }

        return new Sheet(first.Name?.Value ?? "Ark1", [.. lines], bytes);
    }

    /// The sheet written back out as a workbook: the same SDK, the other way
    /// round. The line totals and the sum go in as the formulas Excel expects
    /// (=B2*C2, =SUM), not as numbers, so the file recalculates when it is
    /// opened and keeps working when somebody edits it there.
    public static byte[] Write(string company)
    {
        var book = Books[company];

        using var memory = new MemoryStream();
        using (var doc = SpreadsheetDocument.Create(memory, SpreadsheetDocumentType.Workbook))
        {
            var workbook = doc.AddWorkbookPart();
            workbook.Workbook = new Workbook();
            workbook.AddNewPart<WorkbookStylesPart>().Stylesheet = Styles();

            var sheetPart = workbook.AddNewPart<WorksheetPart>();
            var rows = new SheetData();
            sheetPart.Worksheet = new Worksheet(Widths(), rows);

            rows.Append(new Row(
                Str("A1", "Beskrivelse", Head), Str("B1", "Antal", Head),
                Str("C1", "Stykpris", Head), Str("D1", "I alt", Head)) { RowIndex = 1 });

            uint index = 2;
            foreach (var line in book.Lines)
            {
                rows.Append(new Row(
                    Str($"A{index}", line.Description), Num($"B{index}", line.Qty),
                    Num($"C{index}", line.Unit, Money), Formula($"D{index}", $"B{index}*C{index}", Money))
                { RowIndex = index });
                index++;
            }

            var last = index - 1;
            rows.Append(new Row(
                Str($"A{index}", "I alt", Head), new Cell { CellReference = $"B{index}" }, new Cell { CellReference = $"C{index}" },
                Formula($"D{index}", $"SUM(D2:D{last})", HeadMoney)) { RowIndex = index });

            workbook.Workbook.AppendChild(new Sheets()).AppendChild(new XlSheet
            {
                Id = workbook.GetIdOfPart(sheetPart),
                SheetId = 1,
                Name = book.Name,
            });

            workbook.Workbook.Save();
        }

        var file = memory.ToArray();
        Books[company] = book with { Bytes = file.Length, Edited = false };
        return file;
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

    private static Cell Str(string reference, string value, uint style = Plain) => new()
    {
        CellReference = reference,
        StyleIndex = style,
        DataType = CellValues.InlineString,
        InlineString = new InlineString(new Text(value)),
    };

    private static Cell Num(string reference, decimal value, uint style = Plain) => new()
    {
        CellReference = reference,
        StyleIndex = style,
        DataType = CellValues.Number,
        CellValue = new CellValue(value),
    };

    private static Cell Formula(string reference, string formula, uint style = Plain) => new()
    {
        CellReference = reference,
        StyleIndex = style,
        CellFormula = new CellFormula(formula),
    };

    /// "B7" names column B. The letters lead, so the digits end them.
    private static string Column(Cell cell)
    {
        var reference = cell.CellReference?.Value ?? "";
        var end = reference.TakeWhile(char.IsLetter).Count();
        return reference[..end];
    }

    /// Text lives in a shared table, numbers in the cell. A formula cell holds
    /// the last value Excel worked out — the SDK reads files, it does not
    /// recalculate them, so the totals here are summed in C# instead.
    private static string CellText(Cell cell, SharedStringTable? strings)
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
