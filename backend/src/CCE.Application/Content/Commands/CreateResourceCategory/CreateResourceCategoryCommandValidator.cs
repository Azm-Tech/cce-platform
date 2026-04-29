using FluentValidation;

namespace CCE.Application.Content.Commands.CreateResourceCategory;

public sealed class CreateResourceCategoryCommandValidator : AbstractValidator<CreateResourceCategoryCommand>
{
    public CreateResourceCategoryCommandValidator()
    {
        RuleFor(x => x.NameAr).NotEmpty();
        RuleFor(x => x.NameEn).NotEmpty();
        RuleFor(x => x.Slug).NotEmpty();
        RuleFor(x => x.OrderIndex).GreaterThanOrEqualTo(0);
    }
}
