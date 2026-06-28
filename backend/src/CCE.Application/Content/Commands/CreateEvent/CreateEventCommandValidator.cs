using FluentValidation;

namespace CCE.Application.Content.Commands.CreateEvent;

public sealed class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator()
    {
        RuleFor(x => x.TitleAr).NotEmpty().MaximumLength(255);
        RuleFor(x => x.TitleEn).NotEmpty().MaximumLength(255);
        RuleFor(x => x.DescriptionAr).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.DescriptionEn).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.LocationAr).MaximumLength(255).When(x => x.LocationAr is not null);
        RuleFor(x => x.LocationEn).MaximumLength(255).When(x => x.LocationEn is not null);
        RuleFor(x => x.EndsOn).GreaterThan(x => x.StartsOn).WithMessage("EndsOn must be after StartsOn.");
        RuleFor(x => x.TopicId).NotEmpty();
    }
}
