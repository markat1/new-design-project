namespace OnboardingChecklist.Model;

public record TaskDef(string Key, string Label, bool Required, string Title, string Sub);

public static class Content
{
    public const string Workspace = "Featherstonehaugh-Villanueva International Systems";

    public static readonly TaskDef[] Tasks =
    [
        new("recipients", "Modtagere", true,
            "Hvem skal have den?",
            "Hver kunde får sin egen prisliste vedhæftet, hentet fra regnskabssystemet."),
        new("subject", "Emne", true,
            "Hvad er emnet?",
            "Emnelinjen er ens for alle modtagere."),
        new("note", "Besked", false,
            "Vil du skrive noget med?",
            "En kort besked i selve mailen. Arkene taler for sig selv, så den kan springes over."),
    ];

    public static TaskDef Task(string key) => Tasks.First(t => t.Key == key);

    public static int IndexOf(string key) => Array.FindIndex(Tasks, t => t.Key == key);
}
