using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Clarius.OpenLaw;

/// <summary>
/// Normalizes text potentially containing escaped BBCode-like formatting 
/// such as [[bold]] or [[p]]..[[/p]].
/// </summary>
public static partial class StringMarkup
{
    /// <summary>
    /// Normalizes paragraphs and removes markup.
    /// </summary>
    [return: NotNullIfNotNull("value")]
    public static string? Cleanup(string? value)
    {
        if (value == null)
            return null;

        // First add proper paragraph breaks
        var paragraphs = value.Replace("\r\n", "\n").Replace("[[p]]", "\n").Replace("[[/p]]", "\n");
        // Next collapse multiple paragraph separators (unnecessary newlines) into a single one
        var multiline = MultilineExpr().Replace(paragraphs, "\n\n");
        // Finally, remove any remaining markup
        var clean = RemoveMarkup().Replace(multiline, string.Empty);

        return clean.Trim();
    }

    [GeneratedRegex(@"(\r?\n){3,}")]
    private static partial Regex MultilineExpr();

    [GeneratedRegex(@"\[\[(/?\w+[^\]]*)\]\]")]
    private static partial Regex RemoveMarkup();
}
