using FluentValidation;

namespace CCE.Application.Content.Commands.CreateNews;

public sealed class CreateNewsCommandValidator : AbstractValidator<CreateNewsCommand>
{
    public CreateNewsCommandValidator()
    {
        RuleFor(x => x.TitleAr).NotEmpty().MaximumLength(255);
        RuleFor(x => x.TitleEn).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentAr).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.ContentEn).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.TopicId).NotEmpty();
    }
}
