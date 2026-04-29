using FluentValidation;

namespace CCE.Application.Content.Commands.UpdateNews;

public sealed class UpdateNewsCommandValidator : AbstractValidator<UpdateNewsCommand>
{
    public UpdateNewsCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.TitleAr).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TitleEn).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ContentAr).NotEmpty();
        RuleFor(x => x.ContentEn).NotEmpty();
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(200)
            .Matches("^[a-z0-9]+(-[a-z0-9]+)*$").WithMessage("Slug must be kebab-case.");
        RuleFor(x => x.RowVersion).NotNull().Must(rv => rv.Length == 8)
            .WithMessage("RowVersion must be exactly 8 bytes.");
    }
}
