using CCE.Domain;
using FluentValidation;

namespace CCE.Application.Identity.Permissions.Commands;

public sealed class UpsertUserClaimsCommandValidator : AbstractValidator<UpsertUserClaimsCommand>
{
    private static readonly System.Text.RegularExpressions.Regex ClaimPattern =
        new(@"^[a-z][a-z0-9]*(\.[a-z][a-z0-9]*)+$",
            System.Text.RegularExpressions.RegexOptions.Compiled);

    private static readonly System.Collections.Generic.HashSet<string> KnownClaims =
        new(CCE.Domain.Permissions.All, System.StringComparer.Ordinal);

    public UpsertUserClaimsCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.Claims).NotNull();

        RuleForEach(x => x.Claims)
            .NotEmpty()
            .Matches(ClaimPattern)
            .WithMessage("Claim names must be lowercase dot-separated (e.g. 'news.publish').")
            .Must(BeKnownClaim)
            .WithMessage("'{PropertyValue}' is not a known permission claim.");
    }

    private static bool BeKnownClaim(string claim)
        => KnownClaims.Contains(claim);
}
