using System.Globalization;

namespace OnboardingChecklist.Model;

/// One cell as the workbook holds it: what stands in it, and the formula it
/// was written as, when it was one. The SDK reads files rather than
/// recalculating them, so a formula cell arrives without a value — this app
/// works the value out itself, in Formulas.
public record Cell(string Value = "", string? Formula = null, bool Bold = false)
{
    public bool Blank => Formula is null && Value.Trim().Length == 0;
}

/// A worksheet: the name on its tab, and its rows of cells.
public record Tab(string Name, List<List<Cell>> Rows)
{
    public int Width => Rows.Count == 0 ? 0 : Rows.Max(row => row.Count);

    public Cell At(int row, int column) =>
        row >= 0 && row < Rows.Count && column >= 0 && column < Rows[row].Count ? Rows[row][column] : new Cell();

    public void Put(int row, int column, Cell cell)
    {
        while (Rows.Count <= row) Rows.Add([]);
        while (Rows[row].Count <= column) Rows[row].Add(new Cell());
        Rows[row][column] = cell;
    }
}

/// The two formulas these sheets are written with — a line's total and the sum
/// under them. Anything else is shown as it was typed, the way Excel shows a
/// formula it cannot make sense of, rather than silently showing nothing.
public static class Formulas
{
    public static string Display(Tab tab, int row, int column)
    {
        var cell = tab.At(row, column);
        if (cell.Formula is null) return Number(cell.Value) is { } plain ? Format(plain) : cell.Value;

        var value = Value(tab, row, column, 0);
        return value is null ? "=" + cell.Formula : Format(value.Value);
    }

    /// The number a cell stands for, formula or not, or null when it holds
    /// something that is not one.
    public static decimal? Value(Tab tab, int row, int column, int depth = 0)
    {
        if (depth > 8) return null;

        var cell = tab.At(row, column);
        if (cell.Formula is null) return Number(cell.Value);

        var formula = cell.Formula.Replace(" ", "");

        // =SUM(D2:D7)
        if (formula.StartsWith("SUM(", StringComparison.OrdinalIgnoreCase) && formula.EndsWith(')'))
        {
            var range = formula[4..^1].Split(':');
            if (range.Length != 2 || Reference(range[0]) is not { } from || Reference(range[1]) is not { } to) return null;

            decimal sum = 0;
            for (var r = from.Row; r <= to.Row; r++)
            {
                for (var c = from.Column; c <= to.Column; c++)
                {
                    sum += Value(tab, r, c, depth + 1) ?? 0;
                }
            }

            return sum;
        }

        // =B2*C2
        var parts = formula.Split('*');
        if (parts.Length == 2 && Reference(parts[0]) is { } left && Reference(parts[1]) is { } right)
        {
            var a = Value(tab, left.Row, left.Column, depth + 1);
            var b = Value(tab, right.Row, right.Column, depth + 1);
            return a is null || b is null ? null : a * b;
        }

        return null;
    }

    /// "B7" as a place in the grid, counting from zero.
    private static (int Row, int Column)? Reference(string reference)
    {
        var letters = reference.TakeWhile(char.IsLetter).Count();
        if (letters == 0 || letters == reference.Length) return null;
        if (!int.TryParse(reference[letters..], out var row) || row < 1) return null;

        var column = reference[..letters].ToUpperInvariant()
            .Aggregate(0, (value, letter) => value * 26 + (letter - 'A' + 1));

        return (row - 1, column - 1);
    }

    public static decimal? Number(string text) =>
        decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var value) ? value : null;

    /// Whole kroner with thousands, which is the format these sheets carry.
    private static string Format(decimal value) =>
        value == decimal.Truncate(value) ? value.ToString("N0", CultureInfo.CurrentCulture) : value.ToString("N2", CultureInfo.CurrentCulture);
}
