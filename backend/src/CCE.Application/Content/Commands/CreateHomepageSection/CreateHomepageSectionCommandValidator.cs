using FluentValidation;

namespace CCE.Application.Content.Commands.CreateHomepageSection;

public sealed class CreateHomepageSectionCommandValidator : AbstractValidator<CreateHomepageSectionCommand>
{
    public CreateHomepageSectionCommandValidator()
    {
        RuleFor(x => x.OrderIndex).GreaterThanOrEqualTo(0);
    }
}
