using AppSanitizer = CCE.Application.Common.Sanitization.IHtmlSanitizer;
using Ganss.Xss;

namespace CCE.Infrastructure.Sanitization;

public sealed class HtmlSanitizerWrapper : AppSanitizer
{
    private static readonly HtmlSanitizer Inner = BuildInner();

    public string Sanitize(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }
        return Inner.Sanitize(input);
    }

    private static HtmlSanitizer BuildInner()
    {
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedTags.Clear();
        foreach (var tag in new[] { "p", "br", "strong", "em", "a", "ul", "ol", "li", "blockquote", "code", "pre" })
        {
            sanitizer.AllowedTags.Add(tag);
        }
        sanitizer.AllowedAttributes.Clear();
        sanitizer.AllowedAttributes.Add("href");
        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.Add("https");
        sanitizer.AllowedCssProperties.Clear();
        return sanitizer;
    }
}
