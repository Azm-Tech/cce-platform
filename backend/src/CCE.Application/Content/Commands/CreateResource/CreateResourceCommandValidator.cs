using FluentValidation;

namespace CCE.Application.Content.Commands.CreateResource;

public sealed class CreateResourceCommandValidator : AbstractValidator<CreateResourceCommand>
{
    public CreateResourceCommandValidator()
    {
        RuleFor(x => x.TitleAr).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TitleEn).NotEmpty().MaximumLength(500);
        RuleFor(x => x.DescriptionAr).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.DescriptionEn).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.AssetFileId).NotEmpty();
    }
}
