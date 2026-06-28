using FluentValidation;

namespace CCE.Application.Content.Commands.UpdateResource;

public sealed class UpdateResourceCommandValidator : AbstractValidator<UpdateResourceCommand>
{
    public UpdateResourceCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.TitleAr).NotEmpty().MaximumLength(255);
        RuleFor(x => x.TitleEn).NotEmpty().MaximumLength(255);
        RuleFor(x => x.DescriptionAr).NotEmpty().MaximumLength(500);
        RuleFor(x => x.DescriptionEn).NotEmpty().MaximumLength(500);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.CountryIds).NotEmpty().ForEach(x => x.NotEmpty());
    }
}
