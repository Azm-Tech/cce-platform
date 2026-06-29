using System.Text.RegularExpressions;
using CCE.Application.Community.Moderation;
using Microsoft.Extensions.Options;

namespace CCE.Infrastructure.Moderation;

public sealed partial class RuleBasedPreFilter : IRuleBasedPreFilter
{
    private readonly ModerationOptions _opts;

    public RuleBasedPreFilter(IOptions<ModerationOptions> opts) => _opts = opts.Value;

    [GeneratedRegex(@"^(https?://\S+\s*)+$", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1000)]
    private static partial Regex UrlOnlyPattern();

    public bool ShouldFlag(string content, out string reason)
    {
        if (string.IsNullOrWhiteSpace(content) || content.Trim().Length < 5)
        {
            reason = "content-too-short";
            return true;
        }

        if (UrlOnlyPattern().IsMatch(content.Trim()))
        {
            reason = "url-only";
            return true;
        }

        foreach (var keyword in _opts.DenyList)
        {
            if (content.Contains(keyword, System.StringComparison.OrdinalIgnoreCase))
            {
                reason = $"denylist:{keyword}";
                return true;
            }
        }

        reason = string.Empty;
        return false;
    }
}
