namespace OnboardingChecklist.Model;

public record TaskDef(string Key, string Label, bool Required, string Title, string Sub);

public static class Content
{
    public const string Workspace = "Featherstonehaugh-Villanueva International Systems";

    public static readonly TaskDef[] Tasks =
    [
        new("client", "Kunde", true,
            "Hvem er tilbuddet til?",
            "Navnet står i mailen og i listen over sendte tilbud."),
        new("mail", "Modtager", true,
            "Hvor skal det sendes hen?",
            "Adressen og emnelinjen, præcis som modtageren ser dem."),
        new("lines", "Prislinjer", true,
            "Hvad koster det?",
            "Linjerne her bliver til regnearket der vedhæftes mailen."),
        new("note", "Besked", false,
            "Vil du skrive noget med?",
            "En kort besked i selve mailen. Regnearket taler for sig selv, så den kan springes over."),
    ];

    public static TaskDef Task(string key) => Tasks.First(t => t.Key == key);

    public static int IndexOf(string key) => Array.FindIndex(Tasks, t => t.Key == key);
}
