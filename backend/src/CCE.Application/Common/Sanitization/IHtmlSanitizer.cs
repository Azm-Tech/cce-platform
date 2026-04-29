namespace CCE.Application.Common.Sanitization;

/// <summary>
/// Strips disallowed HTML from user-submitted content. Allowlist:
/// p, br, strong, em, a (https only), ul, ol, li, blockquote, code, pre.
/// </summary>
public interface IHtmlSanitizer
{
    /// <summary>Returns sanitized HTML. Null/empty input returns empty string.</summary>
    string Sanitize(string input);
}
