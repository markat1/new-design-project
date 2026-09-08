namespace OnboardingChecklist.Model;

public enum AppPage { Sent, Quote }

/// Which top-level page the side menu is on. Separate from QuoteDraft so
/// the step flow's own state doesn't get tangled up with app navigation.
public class AppState
{
    public AppPage Page { get; set; } = AppPage.Sent;

    /// Set when a quote has just been sent, so the list opens on it instead of
    /// leaving the reader to find the row themselves.
    public string? SelectOnArrival { get; set; }
}
