using FluentValidation;

namespace CCE.Application.Content.Commands.UpdatePage;

public sealed class UpdatePageCommandValidator : AbstractValidator<UpdatePageCommand>
{
    public UpdatePageCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.TitleAr).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TitleEn).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ContentAr).NotEmpty();
        RuleFor(x => x.ContentEn).NotEmpty();
        RuleFor(x => x.RowVersion).NotNull().Must(rv => rv.Length == 8)
            .WithMessage("RowVersion must be exactly 8 bytes.");
    }
}
