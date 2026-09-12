namespace OnboardingChecklist.Model;

/// One language's mail. Nobody picks it from a list: the customer's own country
/// decides it, and the recipient list already knows the country.
public record Template(string Code, string Name, string Subject, string Body);

public static class Templates
{
    public const string Da = "da";
    public const string Sv = "sv";
    public const string En = "en";

    /// A Dane reads Danish, a Swede Swedish, everybody else English. Not a
    /// judgement about who speaks what — it is which letter the company has
    /// written, and there are three of them.
    public static string For(string country) => country switch
    {
        "DK" => Da,
        "SE" => Sv,
        _ => En,
    };

    /// Written once, in the order they are shown. The order is by how many they
    /// usually reach, not alphabetical: Danish first is what this workspace is.
    public static readonly Template[] All =
    [
        new(Da, "Dansk",
            "Jeres priser for 2027",
            """
            Hej {navn}

            Her er jeres priser for 2027. Arket er vedhæftet og gælder {firma} alene.

            Sig til, hvis noget ser forkert ud.

            Venlig hilsen
            """),

        new(Sv, "Svensk",
            "Era priser för 2027",
            """
            Hej {navn}

            Här är era priser för 2027. Arket är bifogat och gäller endast {firma}.

            Hör av dig om något ser fel ut.

            Vänliga hälsningar
            """),

        new(En, "Engelsk",
            "Your prices for 2027",
            """
            Hi {navn}

            Here are your prices for 2027. The sheet is attached and applies to {firma} alone.

            Do tell us if anything looks wrong.

            Kind regards
            """),
    ];

    public static Template Get(string code) => All.FirstOrDefault(t => t.Code == code) ?? All[^1];

    public static string Name(string code) => Get(code).Name;

    /// The tokens are the tool's, not the reader's — they never leave the
    /// editor. One set across all three letters, so a template can be copied
    /// from one language to the next without rewriting the placeholders.
    public static string Fill(string text, string name, string company) =>
        text.Replace("{navn}", name).Replace("{firma}", company);
}

/// One language's mail as it was actually sent. Copied at send time, like the
/// sheet lines: what went out stays what went out, even if the template moves on.
public record Letter(string Lang, string Subject, string Body);
