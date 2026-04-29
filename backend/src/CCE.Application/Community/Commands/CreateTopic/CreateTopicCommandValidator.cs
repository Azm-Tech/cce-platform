using FluentValidation;

namespace CCE.Application.Community.Commands.CreateTopic;

public sealed class CreateTopicCommandValidator : AbstractValidator<CreateTopicCommand>
{
    public CreateTopicCommandValidator()
    {
        RuleFor(x => x.NameAr).NotEmpty();
        RuleFor(x => x.NameEn).NotEmpty();
        RuleFor(x => x.DescriptionAr).NotEmpty();
        RuleFor(x => x.DescriptionEn).NotEmpty();
        RuleFor(x => x.Slug).NotEmpty();
        RuleFor(x => x.OrderIndex).GreaterThanOrEqualTo(0);
        RuleFor(x => x.IconUrl)
            .Must(url => url is null || url.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
            .WithMessage("IconUrl must use https://.");
    }
}
