using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace OnboardingChecklist.Model;

/// The letter's light formatting, as the toolbar writes it: **fed**, _kursiv_,
/// and lines starting "- " as a list. The text is escaped first, so nothing a
/// person typed — or a customer's name — can turn into markup.
public static partial class LetterText
{
    public static MarkupString Render(string text)
    {
        var html = new StringBuilder();
        var inList = false;

        foreach (var line in WebUtility.HtmlEncode(text).Split('\n'))
        {
            var item = Item().Match(line);

            if (item.Success && !inList) { html.Append("<ul>"); inList = true; }
            if (!item.Success && inList) { html.Append("</ul>"); inList = false; }

            var inline = Italic().Replace(Bold().Replace(item.Success ? item.Groups[1].Value : line, "<strong>$1</strong>"), "$1<em>$2</em>");

            // The pane keeps the text's own line breaks (pre-wrap), so a list
            // item carries none of its own or every item gains an empty line.
            html.Append(item.Success ? $"<li>{inline}</li>" : inline + "\n");
        }

        if (inList) html.Append("</ul>");

        return new MarkupString(html.ToString().TrimEnd('\n'));
    }

    /// The fields the letter can hold. Anything else in braces is a field
    /// spelt wrong, and would reach the customer as "{frima}".
    public static IEnumerable<string> UnknownFields(string text) =>
        Field().Matches(text).Select(m => m.Value).Where(f => f is not ("{navn}" or "{firma}")).Distinct();

    [GeneratedRegex(@"\*\*(.+?)\*\*")]
    private static partial Regex Bold();

    [GeneratedRegex(@"(^|\W)_(.+?)_(?=\W|$)")]
    private static partial Regex Italic();

    [GeneratedRegex(@"^\s*[-•]\s+(.*)$")]
    private static partial Regex Item();

    [GeneratedRegex(@"\{[^{}\s]*\}")]
    private static partial Regex Field();
}
