namespace Clarius.OpenLaw;

static class TimeSpanExtensions
{
    public static string ToMinimalString(this TimeSpan timeSpan)
    {
        var parts = new List<string>();

        if (timeSpan.Hours > 0)
            parts.Add($"{timeSpan.Hours}h");

        if (timeSpan.Minutes > 0)
            parts.Add($"{timeSpan.Minutes}m");

        if (timeSpan.Seconds > 0 || parts.Count == 0) // Always include seconds if no other parts
            parts.Add($"{timeSpan.Seconds}s");

        return string.Join(" ", parts);
    }
}
