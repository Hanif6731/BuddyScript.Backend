using System.Text.RegularExpressions;

namespace BuddyScript.Backend.Utils;

// Strips HTML/script tags to prevent stored XSS while preserving plain text characters
// (apostrophes, quotes, etc.) that React escapes at render time.
public static class InputSanitizer
{
    private static readonly Regex _htmlTags = new(@"<[^>]*>", RegexOptions.Compiled);
    private static readonly Regex _dangerousProtocols = new(@"javascript\s*:", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static string Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var result = _htmlTags.Replace(input.Trim(), string.Empty);
        result = _dangerousProtocols.Replace(result, string.Empty);
        return result;
    }

    public static string? SanitizeNullable(string? input) =>
        input is null ? null : Sanitize(input);
}
