using FluentValidation;

namespace CCE.Application.Identity.Permissions.Commands;

public sealed class UpsertRolePermissionsCommandValidator : AbstractValidator<UpsertRolePermissionsCommand>
{
    private static readonly System.Text.RegularExpressions.Regex PermissionPattern =
        new(@"^[a-z][a-z0-9]*(\.[a-z][a-z0-9]*)+$",
            System.Text.RegularExpressions.RegexOptions.Compiled);

    private static readonly System.Collections.Generic.HashSet<string> KnownPermissions =
        new(CCE.Domain.Permissions.All, System.StringComparer.Ordinal);

    public UpsertRolePermissionsCommandValidator()
    {
        RuleFor(x => x.RoleName).NotEmpty();

        RuleFor(x => x.Permissions).NotNull();

        RuleForEach(x => x.Permissions)
            .NotEmpty()
            .Matches(PermissionPattern)
            .WithMessage("Permission names must be lowercase dot-separated (e.g. 'news.publish').")
            .Must(BeKnownPermission)
            .WithMessage("'{PropertyValue}' is not a known permission. Check permissions.yaml.");
    }

    private static bool BeKnownPermission(string permission)
        => KnownPermissions.Contains(permission);
}
