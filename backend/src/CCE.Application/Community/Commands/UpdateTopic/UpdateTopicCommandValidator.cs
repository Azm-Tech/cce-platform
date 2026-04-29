using FluentValidation;

namespace CCE.Application.Community.Commands.UpdateTopic;

public sealed class UpdateTopicCommandValidator : AbstractValidator<UpdateTopicCommand>
{
    public UpdateTopicCommandValidator()
    {
        RuleFor(x => x.NameAr).NotEmpty();
        RuleFor(x => x.NameEn).NotEmpty();
        RuleFor(x => x.DescriptionAr).NotEmpty();
        RuleFor(x => x.DescriptionEn).NotEmpty();
        RuleFor(x => x.OrderIndex).GreaterThanOrEqualTo(0);
    }
}
