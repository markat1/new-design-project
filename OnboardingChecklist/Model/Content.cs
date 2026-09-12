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
        new("template", "Skabelon", true,
            "Hvad skal der stå?",
            "Skabelonen følger modtagerens eget land: dansk til danskere, svensk til svenskere, engelsk til alle andre."),
        new("send", "Afsendelse", true,
            "Hvordan skal den ud?",
            "Udsendelsen kan lægges i CRM og gemmes lokalt. Prislisterne følger med uanset hvad."),
    ];

    public static TaskDef Task(string key) => Tasks.First(t => t.Key == key);

    public static int IndexOf(string key) => Array.FindIndex(Tasks, t => t.Key == key);
}
