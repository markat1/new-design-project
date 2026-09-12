namespace OnboardingChecklist.Model;

/// Which of the ten colours a person's initials get. Outlook does the same:
/// the identity decides, so one person is one colour on every list, in every
/// session, every time — that is what makes the circle worth looking at.
public static class Avatar
{
    public const int Tones = 10;

    /// FNV-1a, and deliberately not string.GetHashCode(): .NET randomises
    /// string hashing per process, so the colours would deal themselves anew
    /// on every reload and mean nothing.
    public static int ToneOf(string key)
    {
        var hash = 2166136261u;

        foreach (var c in key.Trim().ToLowerInvariant())
        {
            hash ^= c;
            hash *= 16777619u;
        }

        return (int)(hash % Tones);
    }
}
