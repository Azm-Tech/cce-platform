using FluentValidation;

namespace CCE.Application.Content.Commands.ReorderHomepageSections;

public sealed class ReorderHomepageSectionsCommandValidator : AbstractValidator<ReorderHomepageSectionsCommand>
{
    public ReorderHomepageSectionsCommandValidator()
    {
        RuleFor(x => x.Assignments).NotNull().NotEmpty();
        RuleForEach(x => x.Assignments).ChildRules(a =>
        {
            a.RuleFor(x => x.Id).NotEmpty();
            a.RuleFor(x => x.OrderIndex).GreaterThanOrEqualTo(0);
        });
        RuleFor(x => x.Assignments)
            .Must(NotContainDuplicateIds)
            .WithMessage("Assignments must not reference the same section more than once.");
    }

    private static bool NotContainDuplicateIds(System.Collections.Generic.IReadOnlyList<HomepageSectionOrderAssignment> assignments)
    {
        if (assignments is null) return true;
        return assignments.Select(a => a.Id).Distinct().Count() == assignments.Count;
    }
}
