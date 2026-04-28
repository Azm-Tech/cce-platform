using FluentValidation;

namespace CCE.Application.Identity.Commands.AssignUserRoles;

public sealed class AssignUserRolesCommandValidator : AbstractValidator<AssignUserRolesCommand>
{
    private static readonly HashSet<string> KnownRoleNames = new(StringComparer.Ordinal)
    {
        "SuperAdmin",
        "ContentManager",
        "StateRepresentative",
        "CommunityExpert",
        "RegisteredUser",
    };

    public AssignUserRolesCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Roles).NotNull();
        RuleForEach(x => x.Roles).NotEmpty().Must(BeKnownRole)
            .WithMessage(static (cmd, role) => $"Unknown role '{role}'. Known: {string.Join(", ", KnownRoleNames)}.");
        RuleFor(x => x.Roles)
            .Must(NotContainDuplicates)
            .WithMessage("Roles list contains duplicates.");
    }

    private static bool BeKnownRole(string role) => role != null && KnownRoleNames.Contains(role);

    private static bool NotContainDuplicates(IReadOnlyList<string> roles)
    {
        if (roles is null) return true;
        return roles.Count == roles.Distinct(StringComparer.Ordinal).Count();
    }
}
