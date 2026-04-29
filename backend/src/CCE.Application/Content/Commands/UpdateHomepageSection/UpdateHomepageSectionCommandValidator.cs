using FluentValidation;

namespace CCE.Application.Content.Commands.UpdateHomepageSection;

public sealed class UpdateHomepageSectionCommandValidator : AbstractValidator<UpdateHomepageSectionCommand>
{
    public UpdateHomepageSectionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
