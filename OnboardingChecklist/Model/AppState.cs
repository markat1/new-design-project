namespace OnboardingChecklist.Model;

public enum AppPage { Sent, Quote }

/// Which top-level page the side menu is on. Separate from OnboardingState so
/// the step flow's own state doesn't get tangled up with app navigation.
public class AppState
{
    public AppPage Page { get; set; } = AppPage.Sent;
}
