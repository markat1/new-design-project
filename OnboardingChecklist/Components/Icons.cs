using Microsoft.AspNetCore.Components;

namespace OnboardingChecklist.Components;

public static class Icons
{
    public static readonly MarkupString Warn = new(
        """<svg width="13" height="13" viewBox="0 0 16 16" aria-hidden="true"><path d="M8 1.6 15 14H1L8 1.6Z" fill="none" stroke="currentColor" stroke-width="1.4" stroke-linejoin="round"/><path d="M8 6v3.4" stroke="currentColor" stroke-width="1.4" stroke-linecap="round"/><circle cx="8" cy="11.6" r="0.85" fill="currentColor"/></svg>""");

    public static readonly MarkupString X = new(
        """<svg width="13" height="13" viewBox="0 0 16 16" aria-hidden="true"><path d="M4 4l8 8M12 4l-8 8" stroke="currentColor" stroke-width="1.6" stroke-linecap="round"/></svg>""");

    public static readonly MarkupString CheckBig = new(
        """<svg width="22" height="22" viewBox="0 0 24 24" aria-hidden="true"><path d="M5 12.5l4.5 4.5L19 7.5" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"/></svg>""");

    public static readonly MarkupString CheckSm = new(
        """<svg width="12" height="12" viewBox="0 0 16 16" aria-hidden="true"><path d="M3.5 8.4l3 3 6-6"/></svg>""");

    public static readonly MarkupString ArrowL = new(
        """<svg width="14" height="14" viewBox="0 0 16 16" aria-hidden="true"><path d="M10 3 5 8l5 5" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round"/></svg>""");

    public static readonly MarkupString Chevron = new(
        """<svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m6 9 6 6 6-6"/></svg>""");

    public static readonly MarkupString CheckRow = new(
        """<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M20 6 9 17l-5-5"/></svg>""");

    public static readonly MarkupString Inbox = new(
        """<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M22 12h-6l-2 3h-4l-2-3H2"/><path d="M5.45 5.11 2 12v6a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-6l-3.45-6.89A2 2 0 0 0 16.76 4H7.24a2 2 0 0 0-1.79 1.11z"/></svg>""");

    public static readonly MarkupString Plus = new(
        """<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.9" stroke-linecap="round" aria-hidden="true"><path d="M12 5v14M5 12h14"/></svg>""");

    public static readonly MarkupString TickSm = new(
        """<svg width="12" height="12" viewBox="0 0 16 16" aria-hidden="true"><path d="M3 8.4l3.2 3.2L13 5" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/></svg>""");
}
