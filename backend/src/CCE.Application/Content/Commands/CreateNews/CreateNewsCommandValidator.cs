using FluentValidation;

namespace CCE.Application.Content.Commands.CreateNews;

public sealed class CreateNewsCommandValidator : AbstractValidator<CreateNewsCommand>
{
    public CreateNewsCommandValidator()
    {
        RuleFor(x => x.TitleAr).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TitleEn).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ContentAr).NotEmpty();
        RuleFor(x => x.ContentEn).NotEmpty();
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(200);
    }
}
