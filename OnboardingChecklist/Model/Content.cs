namespace OnboardingChecklist.Model;

public record TaskDef(string Key, string Label, bool Required, string Title, string Sub, string Q, string Help);

public record PrefDef(string Key, string Title, string Desc);

public static class Content
{
    public const string Workspace = "Featherstonehaugh-Villanueva International Systems";

    public static readonly TaskDef[] Tasks =
    [
        new("name", "Your name", true,
            "What's your name?",
            "This is how you appear to teammates in mentions, comments, and the member list.",
            "First, what's your name?",
            "This is how you appear to teammates in mentions and comments."),
        new("role", "Your role", true,
            "What do you do?",
            "We'll use this to pick your default views and your first project template.",
            "What do you do here?",
            "It sets your default views and your first project template. You can change it later."),
        new("invites", "Invite your team", false,
            "Invite your team",
            "Setup goes faster with someone to compare notes with. You can always do this later.",
            "Who else should be here?",
            "Setup goes faster with someone to compare notes with — but this can wait."),
        new("prefs", "Notifications", false,
            "How should we reach you?",
            "All of this is editable later under Settings → Notifications.",
            "How should we reach you?",
            "All of it is editable later under Settings → Notifications."),
    ];

    // one deliberately long label, so every variant has to survive a two-line option
    public static readonly string[] Roles =
    [
        "Founder or executive",
        "Engineering manager / technical lead (individual contributor hybrid)",
        "Software engineer",
        "Designer",
        "Product manager",
        "Operations, finance, or something else",
    ];

    public static readonly PrefDef[] Prefs =
    [
        new("digest", "Weekly digest",
            $"A Monday morning summary of everything that changed in {Workspace} while you were away."),
        new("mentions", "Mentions and replies",
            "Email me the moment somebody @-mentions me in a thread, or replies to a comment I left."),
        new("updates", "Product updates",
            "Occasional notes about new features. Never more than once a month, and never shared with anyone."),
    ];

    public static TaskDef Task(string key) => Tasks.First(t => t.Key == key);

    public static int IndexOf(string key) => Array.FindIndex(Tasks, t => t.Key == key);
}
