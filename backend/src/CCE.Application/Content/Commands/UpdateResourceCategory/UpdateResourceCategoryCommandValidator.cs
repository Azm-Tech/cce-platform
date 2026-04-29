using FluentValidation;

namespace CCE.Application.Content.Commands.UpdateResourceCategory;

public sealed class UpdateResourceCategoryCommandValidator : AbstractValidator<UpdateResourceCategoryCommand>
{
    public UpdateResourceCategoryCommandValidator()
    {
        RuleFor(x => x.NameAr).NotEmpty();
        RuleFor(x => x.NameEn).NotEmpty();
        RuleFor(x => x.OrderIndex).GreaterThanOrEqualTo(0);
    }
}
