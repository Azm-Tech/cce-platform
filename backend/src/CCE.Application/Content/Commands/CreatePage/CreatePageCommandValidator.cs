using FluentValidation;

namespace CCE.Application.Content.Commands.CreatePage;

public sealed class CreatePageCommandValidator : AbstractValidator<CreatePageCommand>
{
    public CreatePageCommandValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TitleAr).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TitleEn).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ContentAr).NotEmpty();
        RuleFor(x => x.ContentEn).NotEmpty();
    }
}
