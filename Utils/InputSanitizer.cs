using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace BuddyScript.Backend.Utils;

public static class InputSanitizer
{
    private static readonly HtmlEncoder _encoder =
        HtmlEncoder.Create(new TextEncoderSettings(UnicodeRanges.All));

    public static string Sanitize(string input) =>
        _encoder.Encode(input.Trim());

    public static string? SanitizeNullable(string? input) =>
        input is null ? null : _encoder.Encode(input.Trim());
}
