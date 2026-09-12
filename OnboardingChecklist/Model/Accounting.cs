using System.Globalization;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
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
    /// mail quotes, and the size of the file that gets attached.
    public record Sheet(string Name, QuoteLine[] Lines, int Bytes);

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

    /// Read once, before the first screen, so every page below can stay
    /// synchronous. The system lists the workbooks it has made, and a customer
    /// missing from that list simply has no sheet — asking for each file and
    /// letting it 404 would work too, and would log an error per customer who
    /// has not been priced yet.
    public static async Task LoadAll(HttpClient http, IEnumerable<string> companies)
    {
        var made = await http.GetFromJsonAsync<string[]>("sheets/index.json") ?? [];

        var reads = companies.Distinct()
            .Where(company => made.Contains(FileFor(company)))
            .Select(async company =>
            {
                var file = await http.GetByteArrayAsync($"sheets/{FileFor(company)}");
                using var stream = new MemoryStream(file);
                return (company, book: Read(stream, file.Length));
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
            var cells = row.Elements<Cell>().ToDictionary(Column, c => Text(c, strings));

            if (!cells.TryGetValue("B", out var qty) || !int.TryParse(qty, NumberStyles.Any, CultureInfo.InvariantCulture, out var count)) continue;
            if (!cells.TryGetValue("C", out var price) || !decimal.TryParse(price, NumberStyles.Any, CultureInfo.InvariantCulture, out var unit)) continue;

            lines.Add(new QuoteLine(cells.GetValueOrDefault("A", "").Trim(), count, unit));
        }

        return new Sheet(first.Name?.Value ?? "Ark1", [.. lines], bytes);
    }

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
    private static string Text(Cell cell, SharedStringTable? strings)
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
